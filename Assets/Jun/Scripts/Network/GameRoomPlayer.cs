using UnityEngine;
using Mirror;
using Jun;

namespace Jun
{
    [System.Serializable]
    public class Charater
    {
        /// <summary>CharacterDatabase.index (UI/레거시 호환용)</summary>
        public int    HeroIndex;

        /// <summary>CharacterCard의 CharacterCode - 프리팹 룩업 기준 식별자</summary>
        public string HeroCode;

        // HeroPos 제거: 서버에서 스폰 시 HeroNum 기반으로 FinalHeroPos를 전역 순번으로 할당
    }

    /// <summary>
    /// 캐릭터 선택 전역 큐의 한 항목.
    /// heroCode: 선택된 캐릭터 코드 / ownerNetId: 선택한 GameRoomPlayer의 netId.
    /// </summary>
    [System.Serializable]
    public struct SelectionEntry
    {
        public string heroCode;
        public uint   ownerNetId;
    }

    public class GameRoomPlayer : NetworkRoomPlayer
    {
        public readonly SyncList<Charater> CharaterNum = new SyncList<Charater>();

        // 닉네임 - 모든 클라이언트에 자동 동기화
        [SyncVar(hook = nameof(OnNicknameChanged))]
        public string PlayerNickname = "nonono";

        // 이 플레이어가 조종할 캐릭터 수 (합계는 항상 4)
        [SyncVar(hook = nameof(OnCharCountChanged))]
        public int CharCount = 1;

        // 방 공개 여부 — 서버에서 GameRoomManager.IsPrivate를 읽어 동기화
        [SyncVar(hook = nameof(OnRoomTypeChanged))]
        public bool IsRoomPrivate = false;

        // 연결 순서 기준 고정 인덱스 – 핑 시스템에서 PlayerData와 공유
        [SyncVar] public int PingIndex = -1;

        private bool isChoiced = false;
        public  bool IsChoiced => isChoiced;

        // ── 로컬 플레이어만: 서버에 닉네임 등록 ──
        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();
            string nick = inseon.Playfab.User.PlayfabUserManage.Player?.Nickname ?? "Unknown";
            CmdSetNickname(nick);
        }

        [Command]
        void CmdSetNickname(string nickname)
        {
            PlayerNickname = nickname;
        }

        void OnNicknameChanged(string _, string __)
        {
            PlayerRoomManager.Instance?.RefreshPlayerSlots();
        }

        void OnCharCountChanged(int _, int __)
        {
            PlayerRoomManager.Instance?.RefreshPlayerSlots();
        }

        void OnRoomTypeChanged(bool _, bool __)
        {
            PlayerRoomManager.Instance?.RefreshRoomInfo();
        }

        // NetworkRoomPlayer의 readyToBegin SyncVar hook 재정의
        // Ready 상태가 바뀔 때마다 Host의 Start 버튼 활성화 여부를 갱신합니다.
        public override void ReadyStateChanged(bool oldReadyState, bool newReadyState)
        {
            base.ReadyStateChanged(oldReadyState, newReadyState);
            PlayerRoomManager.Instance?.RefreshStartButton();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            CharaterNum.OnChange += OnCharaterListChanged;

            // Inseon Room 씬: PlayerRoomManager가 모든 UI 처리
            PlayerRoomManager.Instance?.UpdatePlayerNum(true);
            PlayerRoomManager.Instance?.ActiveBTN(isServer);
            PlayerRoomManager.Instance?.RefreshAll();

            // Jun GameRoom 씬 fallback (Jun.LobbyManager가 씬에 있는 경우만 실행됨)
            LobbyManager.Instance?.UpdatePlayerNum(true);
            LobbyManager.Instance?.ActiveBTN(isServer);

            // 이미 선택된 캐릭터도 표시
            foreach (var item in CharaterNum)
                UpdateLobbyUI(SyncList<Charater>.Operation.OP_ADD, item);
        }

        private void OnDestroy()
        {
            // Mirror의 NetworkRoomPlayer.OnDestroy()는 virtual이 아니라 override 불가.
            // UI 갱신 전에 roomSlots에서 먼저 제거해 RefreshPlayerSlots()가 정확한 수를 읽도록 합니다.
            if (NetworkServer.active && NetworkManager.singleton is NetworkRoomManager roomManager)
            {
                roomManager.roomSlots.Remove(this);

                // 퇴장한 플레이어의 CharCount를 가장 적은 잔여 플레이어(주로 호스트)에게 반환
                GameRoomPlayer receiver = null;
                foreach (var slot in roomManager.roomSlots)
                {
                    var p = slot as GameRoomPlayer;
                    if (p == null) continue;
                    if (receiver == null || p.CharCount < receiver.CharCount)
                        receiver = p;
                }
                if (receiver != null) receiver.CharCount += CharCount;
            }

            PlayerRoomManager.Instance?.UpdatePlayerNum(false);
            PlayerRoomManager.Instance?.RefreshPlayerSlots();

            LobbyManager.Instance?.UpdatePlayerNum(false);
        }

        // ── CharCount 서버 초기화 / 재배분 ──

        public override void OnStartServer()
        {
            base.OnStartServer();

            var manager = NetworkManager.singleton as GameRoomManager;
            if (manager == null) return;

            // 방 공개 여부를 SyncVar에 기록 → 모든 클라이언트에 자동 동기화
            IsRoomPrivate = manager.IsPrivate;

            // 기존 플레이어 수 파악 (자신 제외)
            int otherCount = 0;
            GameRoomPlayer richest = null;
            foreach (var slot in manager.roomSlots)
            {
                var p = slot as GameRoomPlayer;
                if (p == null || p == this) continue;
                otherCount++;
                if (richest == null || p.CharCount > richest.CharCount)
                    richest = p;
            }

            if (otherCount == 0)
            {
                // 최초 입장(호스트) → 4개 전부 획득
                CharCount = 4;
            }
            else
            {
                // 후속 플레이어 → CharCount = 1, 가장 많이 가진 플레이어에서 1 가져옴
                CharCount = 1;
                if (richest != null && richest.CharCount > 1)
                    richest.CharCount--;
                // richest가 이미 1이면 합계가 일시적으로 초과하지만
                // 이 경우 방이 가득 찬 상태가 아니므로 호스트가 수동 조정합니다.
            }
        }

        // ── CharCount 조정 Command ──

        /// <summary>
        /// 호스트가 특정 플레이어의 CharCount를 delta만큼 조정합니다.
        /// 합계 4를 유지하기 위해 다른 플레이어에서 자동으로 보상합니다.
        /// </summary>
        [Command]
        public void CmdAdjustCharCount(uint targetNetId, int delta)
        {
            // 호스트만 허용
            if (connectionToClient != NetworkServer.localConnection)
            {
                Debug.LogWarning("[GameRoomPlayer] CmdAdjustCharCount: 호스트가 아닌 호출 차단");
                return;
            }

            var manager = NetworkManager.singleton as GameRoomManager;
            if (manager == null) return;

            if (!NetworkServer.spawned.TryGetValue(targetNetId, out NetworkIdentity targetIdentity)) return;
            var targetPlayer = targetIdentity.GetComponent<GameRoomPlayer>();
            if (targetPlayer == null) return;

            int newCount = targetPlayer.CharCount + delta;
            if (newCount < 1) return; // 플레이어당 최소 1개

            // 보상할 플레이어 선택 (대상 제외)
            GameRoomPlayer compensate = null;
            if (delta > 0)
            {
                // 올릴 때 → CharCount가 가장 많은 타인에게서 1 가져옴
                foreach (var slot in manager.roomSlots)
                {
                    var p = slot as GameRoomPlayer;
                    if (p == null || p == targetPlayer || p.CharCount <= 1) continue;
                    if (compensate == null || p.CharCount > compensate.CharCount)
                        compensate = p;
                }
                if (compensate == null) return; // 가져올 플레이어 없음
                compensate.CharCount--;
            }
            else
            {
                // 내릴 때 → CharCount가 가장 적은 타인에게 1 넘김
                foreach (var slot in manager.roomSlots)
                {
                    var p = slot as GameRoomPlayer;
                    if (p == null || p == targetPlayer) continue;
                    if (compensate == null || p.CharCount < compensate.CharCount)
                        compensate = p;
                }
                if (compensate == null) return; // 혼자라 내릴 수 없음
                compensate.CharCount++;
            }

            targetPlayer.CharCount = newCount;
        }

        // ── 추방 ──

        /// <summary>
        /// 호스트가 특정 플레이어를 강제로 연결 해제(추방)합니다.
        /// [Command]이므로 서버에서 실행되며, 발신자가 호스트 슬롯인지 재검증합니다.
        /// </summary>
        [Command]
        public void CmdKickPlayer(uint targetNetId)
        {
            // 호스트 판별: Mirror 호스트 모드에서 호스트의 connectionToClient == NetworkServer.localConnection
            if (connectionToClient != NetworkServer.localConnection)
            {
                Debug.LogWarning($"[GameRoomPlayer] CmdKickPlayer: 호스트가 아닌 플레이어의 추방 시도 차단 (netId={netId})");
                return;
            }

            if (!NetworkServer.spawned.TryGetValue(targetNetId, out NetworkIdentity target))
            {
                Debug.LogWarning($"[GameRoomPlayer] CmdKickPlayer: 대상 netId={targetNetId} 를 찾을 수 없습니다.");
                return;
            }

            Debug.Log($"[GameRoomPlayer] 플레이어 추방: netId={targetNetId}");
            target.connectionToClient?.Disconnect();
        }

        // ── 채팅 ──

        /// <summary>
        /// 로컬에서 채팅 메시지 전송. 서버를 통해 모든 클라이언트에 전파됩니다.
        /// </summary>
        [Command]
        public void CmdSendChat(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            RpcReceiveChat($"[{PlayerNickname}]: {message}");
        }

        [ClientRpc]
        void RpcReceiveChat(string message)
        {
            // Instance가 null이면 씬에서 직접 찾기 시도
            var window = ChattingWindow.Instance
                         ?? Object.FindAnyObjectByType<ChattingWindow>();

            if (window != null)
                window.DisplayMessage(message);
            else
                Debug.Log($"[Chat] {message}"); // ChattingWindow가 없는 씬에서는 로그로 fallback
        }

        // ── 캐릭터 선택 ──

        /// <summary>
        /// 전역 큐에 캐릭터 선택/해제 토글을 요청합니다.
        /// 실제 큐 관리는 GameRoomManager.TrySelectCharacter(서버)에서 처리하고
        /// 변경 결과는 RpcSyncGlobalQueue로 전체 클라이언트에 브로드캐스트됩니다.
        /// </summary>
        [Command]
        public void CMDChoiceHero(string code)
        {
            var manager = NetworkManager.singleton as GameRoomManager;
            if (manager == null) return;
            manager.TrySelectCharacter(netId, code, CharCount);
        }

        /// <summary>
        /// 캐릭터 선택 확정. 모든 플레이어가 완료되면 서버가 Home 씬으로 전환합니다.
        /// </summary>
        [Command]
        public void CmdConfirmSelection()
        {
            isChoiced = true;
            Debug.Log($"[GameRoomPlayer] {PlayerNickname} 캐릭터 선택 확정");

            var manager = NetworkManager.singleton as GameRoomManager;
            manager?.OnPlayerConfirmedSelection();
        }

        /// <summary>
        /// 서버가 전역 큐 상태를 모든 클라이언트에 동기화할 때 호출합니다.
        /// CharacterSelectManager.OnGlobalQueueSynced 로 전달됩니다.
        /// </summary>
        [ClientRpc]
        public void RpcSyncGlobalQueue(string[] heroCodes, uint[] ownerNetIds)
        {
            int len = Mathf.Min(heroCodes.Length, ownerNetIds.Length);
            var queue = new SelectionEntry[len];
            for (int i = 0; i < len; i++)
                queue[i] = new SelectionEntry { heroCode = heroCodes[i], ownerNetId = ownerNetIds[i] };

            CharacterSelectManager.Instance?.OnGlobalQueueSynced(queue);
        }

        /// <summary>
        /// 클라이언트가 씬 진입 후 현재 큐 상태를 요청합니다.
        /// </summary>
        [Command]
        public void CmdRequestQueueSync()
        {
            var manager = NetworkManager.singleton as GameRoomManager;
            manager?.BroadcastQueue();
        }

        private void OnCharaterListChanged(SyncList<Charater>.Operation op, int itemIndex, Charater item)
        {
            UpdateLobbyUI(op, item);
        }

        private void UpdateLobbyUI(SyncList<Charater>.Operation op, Charater item)
        {
            var lobby = LobbyManager.Instance;
            // HeroIndex가 유효한 경우에만 UI 반영 (레거시 int 인덱스 기반)
            if (lobby == null || item == null || item.HeroIndex < 0) return;
            if (item.HeroIndex >= lobby.Go.Count) return;

            switch (op)
            {
                case SyncList<Charater>.Operation.OP_ADD:
                    lobby.Go[item.HeroIndex].SetActive(true);
                    break;

                case SyncList<Charater>.Operation.OP_REMOVEAT:
                    lobby.Go[item.HeroIndex].SetActive(false);
                    break;
            }
        }
    }
}
