using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace Lsy
{
    public class ReadySystem : NetworkBehaviour
    {
        public static ReadySystem Instance;

        private readonly HashSet<int> _readyConnectionIds = new HashSet<int>();
        private readonly SyncDictionary<uint, bool> _readyByPlayerNetId = new SyncDictionary<uint, bool>();

        [SyncVar(hook = nameof(OnAllReadySyncChanged))]
        private bool _allReadySync;

        public bool AllReady => _allReadySync;

        public static event System.Action<bool> OnAllReadyChanged;
        public static event System.Action<uint, bool> OnPlayerReadyChanged;

        private void Awake()
        {
            if (Instance == null) Instance = this;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            _readyByPlayerNetId.OnChange += OnReadyMapChanged;
        }

        public override void OnStopClient()
        {
            base.OnStopClient();
            _readyByPlayerNetId.OnChange -= OnReadyMapChanged;
        }

        public bool IsPlayerReady(uint playerNetId)
        {
            // 서버에서 받아온다: playerNetId의 준비 상태(readyMap)
            return _readyByPlayerNetId.TryGetValue(playerNetId, out bool isReady) && isReady;
        }

        private void OnReadyMapChanged(SyncDictionary<uint, bool>.Operation op, uint key, bool value)
        {
            // 서버에서 받아온다: 준비 상태 맵 변경(playerNetId -> isReady)
            if (op == SyncDictionary<uint, bool>.Operation.OP_REMOVE)
            {
                OnPlayerReadyChanged?.Invoke(key, false);
                return;
            }

            OnPlayerReadyChanged?.Invoke(key, value);
        }

        [Server]
        public void ServerSetReady(int connectionId, uint playerNetId, bool isReady)
        {
            if (connectionId != 0)
            {
                if (isReady)
                    _readyConnectionIds.Add(connectionId);
                else
                    _readyConnectionIds.Remove(connectionId);
            }

            _readyByPlayerNetId[playerNetId] = isReady;
            RpcOnPlayerReadyChanged(playerNetId, isReady);
            CheckAllReady();
        }

        [Command(requiresAuthority = false)]
        public void CmdToggleReady(uint playerNetId, NetworkConnectionToClient sender = null)
        {
            // 서버로 보낸다: 준비 토글 요청(요청자 connection/netId 기준)
            if (sender == null)
            {
                Debug.LogWarning("[ReadySystem][Server] sender connection is null.");
                return;
            }

            if (sender.identity == null)
            {
                Debug.LogWarning("[ReadySystem][Server] sender identity is null.");
                return;
            }

            int connectionId = sender.connectionId;
            uint authoritativeNetId = sender.identity.netId;

            if (_readyConnectionIds.Contains(connectionId))
            {
                _readyConnectionIds.Remove(connectionId);
                _readyByPlayerNetId[authoritativeNetId] = false;
                Debug.Log($"<color=orange>[ReadySystem][Server] conn:{connectionId}, netId:{authoritativeNetId} 준비 취소. 준비클라:{_readyConnectionIds.Count}</color>");
                RpcOnPlayerReadyChanged(authoritativeNetId, false);
            }
            else
            {
                _readyConnectionIds.Add(connectionId);
                _readyByPlayerNetId[authoritativeNetId] = true;
                Debug.Log($"<color=green>[ReadySystem][Server] conn:{connectionId}, netId:{authoritativeNetId} 준비 완료. 준비클라:{_readyConnectionIds.Count}</color>");
                RpcOnPlayerReadyChanged(authoritativeNetId, true);
            }

            CheckAllReady();
        }

        [Server]
        private void CheckAllReady()
        {
            int clientCount = 0;
            foreach (var conn in NetworkServer.connections.Values)
            {
                if (conn == null) continue;
                if (conn.connectionId == 0) continue;
                clientCount++;
            }

            bool allReady = clientCount > 0 && _readyConnectionIds.Count == clientCount;

            Debug.Log($"<color=cyan>[ReadySystem][Server] 클라이언트:{clientCount}, 준비클라:{_readyConnectionIds.Count}, allReady:{allReady}</color>");

            _allReadySync = allReady;
            OnAllReadyChanged?.Invoke(allReady);
            RpcOnAllReadyChanged(allReady);
        }

        private void OnAllReadySyncChanged(bool oldValue, bool newValue)
        {
            // 서버에서 받아온다: 전체 준비 완료 여부(allReady)
            OnAllReadyChanged?.Invoke(newValue);
        }

        [ClientRpc]
        private void RpcOnPlayerReadyChanged(uint playerNetId, bool isReady)
        {
            // 서버에서 받아온다: 특정 플레이어 준비 상태(playerNetId, isReady)
            Debug.Log($"<color=yellow>[ReadySystem][Client] netId:{playerNetId} isReady:{isReady}</color>");
            OnPlayerReadyChanged?.Invoke(playerNetId, isReady);
        }

        [ClientRpc]
        private void RpcOnAllReadyChanged(bool allReady)
        {
            // 서버에서 받아온다: 전체 준비 완료 여부(allReady)
            OnAllReadyChanged?.Invoke(allReady);
        }

        [Server]
        public void OnPlayerDisconnected(int connectionId, uint playerNetId)
        {
            _readyConnectionIds.Remove(connectionId);
            _readyByPlayerNetId[playerNetId] = false;
            RpcOnPlayerReadyChanged(playerNetId, false);
            CheckAllReady();
        }
    }
}
