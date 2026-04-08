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
        public string PlayerNickname = "";

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
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            CharaterNum.OnChange += OnCharaterListChanged;

            // 플레이어 수 업데이트
            LobbyManager.Instance.UpdatePlayerNum(true);
            // 준비 or 시작버튼 활성화
            LobbyManager.Instance.ActiveBTN(isServer);
            // 플레이어 슬롯 갱신
            LobbyManager.Instance?.RefreshPlayerSlots();

            // 이미 선택된 캐릭터도 표시
            foreach (var item in CharaterNum)
                UpdateLobbyUI(SyncList<Charater>.Operation.OP_ADD, item);
        }

        private void OnDestroy()
        {
            if (LobbyManager.Instance != null)
            {
                LobbyManager.Instance.UpdatePlayerNum(false);
                LobbyManager.Instance.RefreshPlayerSlots();
            }
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
