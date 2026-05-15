using UnityEngine;
using UnityEngine.UI;
using Mirror;

namespace Jun
{
    public class MainMenu : NetworkBehaviour
    {
        public void OnCreateRoom()
        {
            var manager = GameRoomManager.singleton;
            manager.StartHost();
        }

        //방 입장하기
        public void OnEnterRoom()
        {
            var manager = GameRoomManager.singleton;
            manager.StartClient();
        }
    }
}
