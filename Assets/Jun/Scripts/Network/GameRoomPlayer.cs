using UnityEngine;
using Mirror;
using Jun;

namespace Jun
{
    [System.Serializable]
    public class Charater
    {
        public int HeroIndex;
        public int HeroPos;
    }

    public class GameRoomPlayer : NetworkRoomPlayer
    {
        public readonly SyncList<Charater> CharaterNum = new SyncList<Charater>();

        // 닉네임 - 모든 클라이언트에 자동 동기화
        [SyncVar(hook = nameof(OnNicknameChanged))]
        public string PlayerNickname = "nonono";

        private bool isChoiced = false;

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
            LobbyManager.Instance?.RefreshPlayerSlots();
            PlayerRoomManager.Instance?.RefreshPlayerSlots();
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
            LobbyManager.Instance?.RefreshPlayerSlots();

            // 이미 선택된 캐릭터도 표시
            foreach (var item in CharaterNum)
                UpdateLobbyUI(SyncList<Charater>.Operation.OP_ADD, item);
        }

        private void OnDestroy()
        {
            PlayerRoomManager.Instance?.UpdatePlayerNum(false);
            PlayerRoomManager.Instance?.RefreshPlayerSlots();

            LobbyManager.Instance?.UpdatePlayerNum(false);
            LobbyManager.Instance?.RefreshPlayerSlots();
        }

        // ── 추방 ──

        /// <summary>
        /// 호스트가 특정 플레이어를 강제로 연결 해제(추방)합니다.
        /// [Command]이므로 서버에서 실행되며, 발신자가 호스트 슬롯인지 재검증합니다.
        /// </summary>
        [Command]
        public void CmdKickPlayer(uint targetNetId)
        {
            // 서버에서 실행됨.
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
                LobbyManager.Instance?.AddChatMessage(message);
        }

        // ── 캐릭터 선택 ──

        [Command]
        public void CMDChoiceHero(int index)
        {
            foreach (var i in CharaterNum)
            {
                if (index == i.HeroIndex)
                {
                    isChoiced = true;
                    CharaterNum.Remove(i);
                    return;
                }
            }

            // 다른 플레이어가 이미 선택했는지 확인
            foreach (var player in ((GameRoomManager)NetworkManager.singleton).roomSlots)
            {
                GameRoomPlayer roomPlayer = player as GameRoomPlayer;
                if (roomPlayer == null) continue;

                foreach (var charInfo in roomPlayer.CharaterNum)
                {
                    if (charInfo.HeroIndex == index)
                    {
                        Debug.Log("이미 다른 플레이어가 선택한 캐릭터입니다.");
                        return;
                    }
                }
            }

            CharaterNum.Add(new Charater { HeroIndex = index, HeroPos = index });
        }

        private void OnCharaterListChanged(SyncList<Charater>.Operation op, int itemIndex, Charater item)
        {
            UpdateLobbyUI(op, item);
        }

        private void UpdateLobbyUI(SyncList<Charater>.Operation op, Charater item)
        {
            var lobby = LobbyManager.Instance;
            if (lobby == null || item == null) return;

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
