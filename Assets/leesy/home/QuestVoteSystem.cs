using System;
using System.Collections;
using System.Collections.Generic;
using Jun;
using Mirror;
using UnityEngine;

namespace Lsy
{
    public class QuestVoteSystem : NetworkBehaviour
    {
        public static QuestVoteSystem Instance;

        [SyncVar(hook = nameof(OnVoteStateSync))] private bool _voteInProgress;
        [SyncVar(hook = nameof(OnVoteStateSync))] private bool _voteResolved;
        [SyncVar(hook = nameof(OnVoteStateSync))] private bool _votePassed;
        [SyncVar(hook = nameof(OnVoteTimeSync))] private double _voteEndTime;
        [SyncVar(hook = nameof(OnVoteSceneSync))] private string _selectedSceneName = string.Empty;
        [SyncVar(hook = nameof(OnVoteDistrictSync))] private string _selectedDistrictType = string.Empty;
        [SyncVar(hook = nameof(OnVoteStageNameSync))] private string _selectedStageName = string.Empty;
        [SyncVar(hook = nameof(OnVoteCountSync))] private int _acceptCount;
        [SyncVar(hook = nameof(OnVoteCountSync))] private int _rejectCount;

        private readonly SyncDictionary<uint, sbyte> _votes = new SyncDictionary<uint, sbyte>();
        private readonly HashSet<int> _eligibleConnectionIds = new HashSet<int>();

        public static event Action OnVoteStateChanged;

        public bool VoteInProgress => _voteInProgress;
        public bool VoteResolved => _voteResolved;
        public bool VotePassed => _votePassed;
        public double VoteEndTime => _voteEndTime;
        public string SelectedSceneName => _selectedSceneName;
        public string SelectedDistrictType => _selectedDistrictType;
        public string SelectedStageName => _selectedStageName;
        public int AcceptCount => _acceptCount;
        public int RejectCount => _rejectCount;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                return;
            }

            if (Instance != this)
            {
                Debug.LogWarning("[QuestVoteSystem] Duplicate instance detected. Disabling this component.");
                enabled = false;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            OnVoteStateChanged?.Invoke();
        }

        [Command(requiresAuthority = false)]
        public void CmdHostStartVote(NetworkConnectionToClient sender = null)
        {
            if (!isServer) return;
            if (!IsHostConnection(sender)) return;
            if (_voteInProgress) return;
            if (!TryResolveCurrentBattleScene(out string sceneName, out string districtType, out string stageName)) return;

            _selectedSceneName = sceneName;
            _selectedDistrictType = districtType;
            _selectedStageName = stageName;
            _voteResolved = false;
            _votePassed = false;
            _voteInProgress = true;
            _acceptCount = 0;
            _rejectCount = 0;
            _votes.Clear();

            BuildEligibleConnections();
            if (_eligibleConnectionIds.Count == 0)
            {
                ResolveVote(true);
                return;
            }

            _voteEndTime = NetworkTime.time + 60d;

            RpcNotifyVoteStateChanged();
            StartCoroutine(CoVoteTimeout());
        }

        [Command(requiresAuthority = false)]
        public void CmdSubmitVote(bool accept, NetworkConnectionToClient sender = null)
        {
            if (!isServer) return;
            if (!_voteInProgress) return;
            if (sender == null || sender.identity == null) return;
            if (sender.connectionId == 0) return;
            if (!_eligibleConnectionIds.Contains(sender.connectionId)) return;

            uint netId = sender.identity.netId;
            if (_votes.ContainsKey(netId)) return;

            _votes[netId] = accept ? (sbyte)1 : (sbyte)-1;
            RecountVotes();

            if (_votes.Count >= _eligibleConnectionIds.Count)
            {
                ResolveVote(_acceptCount > _rejectCount);
                return;
            }

            RpcNotifyVoteStateChanged();
        }

        private IEnumerator CoVoteTimeout()
        {
            while (_voteInProgress && NetworkTime.time < _voteEndTime)
                yield return null;

            if (!_voteInProgress) yield break;

            foreach (var conn in NetworkServer.connections.Values)
            {
                if (conn == null || conn.identity == null) continue;
                if (!_eligibleConnectionIds.Contains(conn.connectionId)) continue;

                uint netId = conn.identity.netId;
                if (_votes.ContainsKey(netId)) continue;
                _votes[netId] = 1;
            }

            RecountVotes();

            ResolveVote(_acceptCount > _rejectCount);
        }

        private void RecountVotes()
        {
            int accept = 0;
            int reject = 0;
            foreach (var kv in _votes)
            {
                if (kv.Value > 0) accept++;
                else if (kv.Value < 0) reject++;
            }

            _acceptCount = accept;
            _rejectCount = reject;
        }

        private void BuildEligibleConnections()
        {
            _eligibleConnectionIds.Clear();
            foreach (var conn in NetworkServer.connections.Values)
            {
                if (conn == null) continue;
                if (conn.connectionId == 0) continue;
                _eligibleConnectionIds.Add(conn.connectionId);
            }
        }

        [Server]
        private void ResolveVote(bool passed)
        {
            _votePassed = passed;
            _voteResolved = true;
            _voteInProgress = false;
            _voteEndTime = 0d;

            RpcNotifyVoteResolved(_votePassed);
            RpcNotifyVoteStateChanged();
        }

        private bool IsHostConnection(NetworkConnectionToClient sender)
        {
            return sender != null && sender.connectionId == 0;
        }

        private static bool TryResolveCurrentBattleScene(out string sceneName, out string districtType, out string stageName)
        {
            sceneName = string.Empty;
            districtType = string.Empty;
            stageName = string.Empty;

            // [수정] 투표는 "방금 눌린 퀘스트 버튼" 기준이 되도록 브리지 선택값을 우선 사용
            QuestData selectedQuest = null;
            if (QuestVoteSelectionBridge.Instance != null && QuestVoteSelectionBridge.Instance.CurrentQuest != null)
                selectedQuest = QuestVoteSelectionBridge.Instance.CurrentQuest;
            else
                selectedQuest = SelectedQuest.Current;

            if (selectedQuest == null)
            {
                Debug.LogWarning("[QuestVoteSystem] SelectedQuest.Current is null. Host must select quest first.");
                return false;
            }

            var rm = NetworkManager.singleton as GameRoomManager;
            if (rm == null || string.IsNullOrWhiteSpace(rm.GameplayScene))
            {
                Debug.LogWarning("[QuestVoteSystem] GameplayScene is empty. Host must select quest first.");
                return false;
            }

            sceneName = rm.GameplayScene;
            districtType = selectedQuest.districtType.ToString();

            // [수정] stageName이 비어 있으면 districtType 문자열로 fallback
            string resolvedStageName = selectedQuest.stageName;
            if (string.IsNullOrWhiteSpace(resolvedStageName))
                resolvedStageName = districtType;
            stageName = resolvedStageName;
            return true;
        }

        private void OnVoteStateSync(bool _, bool __) { OnVoteStateChanged?.Invoke(); }
        private void OnVoteTimeSync(double _, double __) { OnVoteStateChanged?.Invoke(); }
        private void OnVoteSceneSync(string _, string __) { OnVoteStateChanged?.Invoke(); }
        private void OnVoteDistrictSync(string _, string __) { OnVoteStateChanged?.Invoke(); }
        private void OnVoteStageNameSync(string _, string __) { OnVoteStateChanged?.Invoke(); }
        private void OnVoteCountSync(int _, int __) { OnVoteStateChanged?.Invoke(); }

        [ClientRpc]
        private void RpcNotifyVoteStateChanged()
        {
            OnVoteStateChanged?.Invoke();
        }

        [ClientRpc]
        private void RpcNotifyVoteResolved(bool passed)
        {
            _votePassed = passed;
            OnVoteStateChanged?.Invoke();
        }
    }
}
