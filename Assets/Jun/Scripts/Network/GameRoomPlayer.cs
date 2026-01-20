using UnityEngine;
using Mirror;
using Jun;

namespace Jun
{
    public class GameRoomPlayer : NetworkRoomPlayer
    {

        [SyncVar(hook = nameof(ChangedCharater))]
        public int HeroIndex = -1;
        public int HeroPos = -1;

        //선택한 정보로 영웅정보 변경
        [Command]
        public void CMDChangeHero(int index)
        {
            HeroIndex = index;
            HeroPos = index;
        }
        // 변경된 영웅 바꿔서 보여주기
        public void ChangedCharater(int oldIndex, int newIndex)
        {

            var lobby = GameObject.Find("LobbyManager").GetComponent<LobbyManager>();
            if (oldIndex != -1) lobby.Go[oldIndex].SetActive(false);
            lobby.Go[newIndex].SetActive(true);
        }
    }
}
