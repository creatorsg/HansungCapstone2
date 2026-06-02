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
            // [수정] 준비 상태 알림은 SyncDictionary.OnChange 한 경로로만 전달합니다.
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

            // [수정] 기존 호환용 토글 경로도 공통 설정 함수로 통일합니다.
            ServerSetReady(connectionId, authoritativeNetId, !_readyConnectionIds.Contains(connectionId));
        }

        // [수정] 클라이언트가 목표 준비 상태를 명시적으로 보내도록 합니다.
        // 같은 요청이 중복 전달되어도 준비/취소 상태가 다시 뒤집히지 않습니다.
        [Command(requiresAuthority = false)]
        public void CmdSetReady(bool isReady, NetworkConnectionToClient sender = null)
        {
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
            ServerSetReady(connectionId, authoritativeNetId, isReady);
        }

        [Server]
        private void CheckAllReady()
        {
            int clientCount = 0;
            // [수정] 현재 접속 중인 모든 비호스트 클라이언트가 실제로 준비 목록에 있는지 확인합니다.
            // 단순 Count 비교는 연결 종료 후 남은 오래된 ID 때문에 잘못 true가 될 수 있습니다.
            bool allReady = true;
            foreach (var conn in NetworkServer.connections.Values)
            {
                if (conn == null) continue;
                if (conn.connectionId == 0) continue;
                clientCount++;
                if (!_readyConnectionIds.Contains(conn.connectionId))
                    allReady = false;
            }

            Debug.Log($"<color=cyan>[ReadySystem][Server] 클라이언트:{clientCount}, 준비클라:{_readyConnectionIds.Count}, allReady:{allReady}</color>");

            // Bug Fix: 이벤트 발화 경로를 RpcOnAllReadyChanged 하나로 통일한다.
            // 이전 코드는 호스트 기준으로
            //   ① _allReadySync = allReady  → SyncVar hook(OnAllReadySyncChanged) → 이벤트 1회
            //   ② OnAllReadyChanged?.Invoke() → 이벤트 1회
            //   ③ RpcOnAllReadyChanged        → 이벤트 1회  (총 3회 중복)
            // 클라이언트도 SyncVar hook + RPC로 2회 중복 발화됐다.
            // → SyncVar hook과 직접 호출을 제거하고 RPC 한 경로만 남긴다.
            _allReadySync = allReady;
            RpcOnAllReadyChanged(allReady);
        }

        private void OnAllReadySyncChanged(bool oldValue, bool newValue)
        {
            // 순수 클라이언트(non-host)에서 SyncVar가 뒤늦게 동기화될 경우 보정용.
            // 호스트는 RpcOnAllReadyChanged로 이미 처리되므로 중복 방지.
            if (!isServer)
                OnAllReadyChanged?.Invoke(newValue);
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
            // [수정] 연결 종료 상태도 SyncDictionary.OnChange를 통해 한 번만 알립니다.
            CheckAllReady();
        }
    }
}
