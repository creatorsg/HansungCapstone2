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

        [Header("Vote Settings")]
        [SerializeField] private int voteDurationSeconds = 30;

        [Header("Quest Data (District -> StageName)")]
        [SerializeField] private QuestData[] allQuests;

        [SyncVar(hook = nameof(OnQuestSelectedChanged))] private bool _isQuestSelected;
        [SyncVar(hook = nameof(OnVoteRunningChanged))] private bool _isVoteRunning;
        [SyncVar(hook = nameof(OnVoteFinishedChanged))] private bool _isVoteFinished;
        [SyncVar(hook = nameof(OnLockedDistrictChanged))] private string _lockedDistrictName = "";
        [SyncVar(hook = nameof(OnLockedSceneChanged))] private string _lockedSceneName = "";
        [SyncVar(hook = nameof(OnLockedStageNameChanged))] private string _lockedStageName = "";
        [SyncVar(hook = nameof(OnRemainingSecondsChanged))] private int _remainingSeconds;
        [SyncVar(hook = nameof(OnAcceptCountChanged))] private int _acceptCount;
        [SyncVar(hook = nameof(OnRejectCountChanged))] private int _rejectCount;
        [SyncVar(hook = nameof(OnVoteRoundChanged))] private int _voteRoundId;
        [SyncVar(hook = nameof(OnVoteResultMessageChanged))] private string _voteResultMessage = "";
        [SyncVar(hook = nameof(OnVoteApprovedChanged))] private bool _isVoteApproved;

        private readonly Dictionary<int, bool> _votesByConnectionId = new Dictionary<int, bool>();

        public struct VoteSnapshot
        {
            public bool IsQuestSelected;
            public bool IsVoteRunning;
            public bool IsVoteFinished;
            public bool IsVoteApproved;
            public string LockedDistrictName;
            public string LockedSceneName;
            public string LockedStageName;
            public int RemainingSeconds;
            public int AcceptCount;
            public int RejectCount;
            public int VoteRoundId;
            public string VoteResultMessage;
        }

        public static event System.Action<bool, bool> OnVotePhaseChanged;
        public static event System.Action<VoteSnapshot> OnVoteSnapshotChanged;

        public bool IsQuestSelected => _isQuestSelected;
        public bool IsVoteRunning => _isVoteRunning;
        public bool IsVoteFinished => _isVoteFinished;
        public bool IsVoteApproved => _isVoteApproved;
        public bool CanUseReadyOrStartButton => _isVoteFinished && _isVoteApproved;
        public VoteSnapshot CurrentSnapshot => BuildSnapshot();

        private Coroutine _voteRoutine;

        private void Awake()
        {
            if (Instance == null) Instance = this;
        }

        private void OnEnable()
        {
            DistrictHover.OnDistrictClicked += OnDistrictClicked;
        }

        private void OnDisable()
        {
            DistrictHover.OnDistrictClicked -= OnDistrictClicked;
        }

        private void Start()
        {
            RaiseVoteSnapshotChanged();
            OnVotePhaseChanged?.Invoke(_isVoteRunning, _isVoteFinished);
        }

        private void OnDistrictClicked(DistrictType districtType)
        {
            if (!NetworkServer.active) return;
            if (_isVoteRunning) return;

            var rm = NetworkManager.singleton as GameRoomManager;
            if (rm == null) return;

            string currentScene = rm.GameplayScene;
            if (string.IsNullOrEmpty(currentScene)) return;

            _lockedDistrictName = districtType.ToString();
            _lockedSceneName = currentScene;
            _lockedStageName = ResolveStageName(districtType);
            _isQuestSelected = true;

            _isVoteFinished = false;
            _isVoteApproved = false;
            _voteResultMessage = "";

            RaiseVoteSnapshotChanged();
        }

        [Command(requiresAuthority = false)]
        public void CmdRequestStartVoteFromUI(NetworkConnectionToClient sender = null)
        {
            if (!NetworkServer.active) return;
            if (sender != NetworkServer.localConnection) return;
            if (!_isQuestSelected) return;
            if (_isVoteRunning || _isVoteFinished) return;

            var rm = NetworkManager.singleton as GameRoomManager;
            if (rm == null) return;

            string currentScene = rm.GameplayScene;
            if (string.IsNullOrEmpty(currentScene)) return;

            if (!System.Enum.TryParse(_lockedDistrictName, out DistrictType selectedDistrict))
                return;

            // 호스트 혼자: 투표 없이 즉시 전투 씬 이동
            if (IsSoloHost())
            {
                _lockedDistrictName = selectedDistrict.ToString();
                _lockedSceneName = currentScene;
                _lockedStageName = ResolveStageName(selectedDistrict);
                _isQuestSelected = true;
                _isVoteRunning = false;
                _isVoteFinished = true;
                _isVoteApproved = true;
                _voteRoundId++;
                _voteResultMessage = "솔로 플레이 - 바로 시작합니다.";
                _votesByConnectionId.Clear();
                RaiseAll();

                // PlayerAccount를 찾아 데이터를 동기화합니다.
                foreach (var conn in NetworkServer.connections.Values)
                {
                    var account = conn.identity.GetComponent<PlayerAccount>();
                    if (account != null) account.SyncAllHideoutDataToBattleData();
                }

                rm.ServerChangeScene(currentScene);
                return;
            }

            ServerStartVote(selectedDistrict, currentScene);
        }

        [Server]
        private bool IsSoloHost()
        {
            foreach (var conn in NetworkServer.connections.Values)
            {
                if (conn == null) continue;
                if (conn.connectionId == 0) continue;
                return false;
            }
            return true;
        }

        [Command(requiresAuthority = false)]
        public void CmdSubmitVote(bool accept, NetworkConnectionToClient sender = null)
        {
            if (!NetworkServer.active) return;
            if (!_isVoteRunning) return;
            if (sender == null) return;
            if (sender == NetworkServer.localConnection) return;
            if (_votesByConnectionId.ContainsKey(sender.connectionId)) return;

            _votesByConnectionId[sender.connectionId] = accept;
            RecountVotesOnServer();
        }

        [Server]
        private void ServerStartVote(DistrictType districtType, string sceneName)
        {
            _lockedDistrictName = districtType.ToString();
            _lockedSceneName = sceneName;
            _lockedStageName = ResolveStageName(districtType);
            _isQuestSelected = true;

            _isVoteRunning = true;
            _isVoteFinished = false;
            _isVoteApproved = false;
            _remainingSeconds = voteDurationSeconds;
            _voteRoundId++;
            _voteResultMessage = "";

            _votesByConnectionId.Clear();
            RecountVotesOnServer();

            if (_voteRoutine != null)
                StopCoroutine(_voteRoutine);
            _voteRoutine = StartCoroutine(ServerVoteCountdown());
        }

        [Server]
        private IEnumerator ServerVoteCountdown()
        {
            while (_remainingSeconds > 0)
            {
                RecountVotesOnServer();
                yield return new WaitForSeconds(1f);
                _remainingSeconds--;
            }

            RecountVotesOnServer();

            int totalVotes = _acceptCount + _rejectCount;
            int majority = (totalVotes / 2) + 1;
            bool approved = totalVotes > 0 && _acceptCount >= majority;

            _isVoteRunning = false;

            if (approved)
            {
                _isVoteFinished = true;
                _isVoteApproved = true;
                _voteResultMessage = "과반수 찬성으로 퀘스트가 확정되었습니다.";
                yield break;
            }

            _isVoteFinished = false;
            _isVoteApproved = false;
            _isQuestSelected = false;
            _voteResultMessage = "동점 또는 거절 우세로 확정 실패. 퀘스트를 다시 선택하세요.";

            _lockedDistrictName = "";
            _lockedSceneName = "";
            _lockedStageName = "";
            _acceptCount = 0;
            _rejectCount = 0;
            _remainingSeconds = 0;
            _votesByConnectionId.Clear();

            var rm = NetworkManager.singleton as GameRoomManager;
            if (rm != null)
                rm.GameplayScene = "";
        }

        [Server]
        private void RecountVotesOnServer()
        {
            int accept = 0;
            int reject = 0;

            foreach (var pair in _votesByConnectionId)
            {
                if (pair.Value) accept++;
                else reject++;
            }

            _acceptCount = accept;
            _rejectCount = reject;
        }

        [Server]
        private string ResolveStageName(DistrictType districtType)
        {
            if (allQuests != null)
            {
                for (int i = 0; i < allQuests.Length; i++)
                {
                    var q = allQuests[i];
                    if (q == null) continue;
                    if (q.districtType != districtType) continue;
                    if (!string.IsNullOrWhiteSpace(q.stageName)) return q.stageName;
                }
            }
            return "미정";
        }

        private void OnQuestSelectedChanged(bool _, bool __) { RaiseVoteSnapshotChanged(); }
        private void OnVoteRunningChanged(bool _, bool __) { RaiseAll(); }
        private void OnVoteFinishedChanged(bool _, bool __) { RaiseAll(); }
        private void OnLockedDistrictChanged(string _, string __) { RaiseVoteSnapshotChanged(); }
        private void OnLockedSceneChanged(string _, string __) { RaiseVoteSnapshotChanged(); }
        private void OnLockedStageNameChanged(string _, string __) { RaiseVoteSnapshotChanged(); }
        private void OnRemainingSecondsChanged(int _, int __) { RaiseVoteSnapshotChanged(); }
        private void OnAcceptCountChanged(int _, int __) { RaiseVoteSnapshotChanged(); }
        private void OnRejectCountChanged(int _, int __) { RaiseVoteSnapshotChanged(); }
        private void OnVoteRoundChanged(int _, int __) { RaiseVoteSnapshotChanged(); }
        private void OnVoteResultMessageChanged(string _, string __) { RaiseVoteSnapshotChanged(); }
        private void OnVoteApprovedChanged(bool _, bool __) { RaiseVoteSnapshotChanged(); }

        private void RaiseAll()
        {
            RaiseVoteSnapshotChanged();
            OnVotePhaseChanged?.Invoke(_isVoteRunning, _isVoteFinished);
        }

        private VoteSnapshot BuildSnapshot()
        {
            return new VoteSnapshot
            {
                IsQuestSelected = _isQuestSelected,
                IsVoteRunning = _isVoteRunning,
                IsVoteFinished = _isVoteFinished,
                IsVoteApproved = _isVoteApproved,
                LockedDistrictName = _lockedDistrictName,
                LockedSceneName = _lockedSceneName,
                LockedStageName = _lockedStageName,
                RemainingSeconds = _remainingSeconds,
                AcceptCount = _acceptCount,
                RejectCount = _rejectCount,
                VoteRoundId = _voteRoundId,
                VoteResultMessage = _voteResultMessage
            };
        }

        private void RaiseVoteSnapshotChanged()
        {
            OnVoteSnapshotChanged?.Invoke(BuildSnapshot());
        }
    }
}
