using UnityEngine;
using Mirror;
using NUnit.Framework;
using System.Collections.Generic;

namespace Jun
{
    public class LobbyManager : NetworkBehaviour
    {
        public List<GameObject> Go;

        //캐릭터 선택 버튼
        public void ChoiceChar(int index)
        {
            var player = NetworkClient.localPlayer.GetComponent<GameRoomPlayer>();
            player.CMDChoiceHero(index);
        }

    }
}
