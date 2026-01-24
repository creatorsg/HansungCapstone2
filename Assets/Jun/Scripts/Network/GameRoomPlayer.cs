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

        private bool isChoiced = false;
        public override void OnStartClient()
        {
            base.OnStartClient();
            CharaterNum.OnChange += OnCharaterListChanged;

            // 접속 당시 이미 선택된 캐릭터들 표시
            foreach (var item in CharaterNum)
            {
                UpdateLobbyUI(SyncList<Charater>.Operation.OP_ADD, item);
            }
        }


        //선택한 정보로 영웅정보 추가
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
            // 다른 사람이 이미 선택했는지 확인
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


        // Mirror 버전(3개 매개변수)에 맞춘 콜백
        private void OnCharaterListChanged(SyncList<Charater>.Operation op, int itemIndex, Charater item)
        {
            UpdateLobbyUI(op, item);
        }

        private void UpdateLobbyUI(SyncList<Charater>.Operation op, Charater item)
        {
            var lobby = GameObject.Find("LobbyManager")?.GetComponent<LobbyManager>();
            if (lobby == null || item == null) return;

            switch (op)
            {
                case SyncList<Charater>.Operation.OP_ADD:
                    lobby.Go[item.HeroIndex].SetActive(true);
                    // 여기에 "누가 선택했는지" 표시하는 로직이 있으면 더 좋습니다.
                    // 예: lobby.NameText[item.HeroIndex].text = this.playerName;
                    break;

                case SyncList<Charater>.Operation.OP_REMOVEAT:
                    lobby.Go[item.HeroIndex].SetActive(false);
                    break;
            }
        }

    }
}
