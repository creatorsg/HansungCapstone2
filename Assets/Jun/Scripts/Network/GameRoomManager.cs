using UnityEngine;
using Mirror;
using NUnit.Framework;
using System.Collections.Generic;

namespace Jun {
    public class GameRoomManager : NetworkRoomManager
    {
        // 로비에서 본게임으로 넘어갈 때 서버에서 실행되는 함수입니다.
        public override GameObject OnRoomServerCreateGamePlayer(NetworkConnectionToClient conn, GameObject roomPlayer)
        {
            // 로비 플레이어(GameRoomPlayer)에서 선택했던 정보를 꺼내기
            var roomPlayerScript = roomPlayer.GetComponent<GameRoomPlayer>();
            int index = roomPlayerScript.HeroIndex;
            int pos = roomPlayerScript.HeroPos;

            // 본게임용 플레이어 프리팹을 생성
            GameObject gamePlayer = Instantiate(spawnPrefabs[index]);

            // 생성된 게임 플레이어 스크립트에 데이터를 주입
            var gamePlayerScript = gamePlayer.GetComponent<GamePlayerController>();
            gamePlayerScript.FinalHeroIndex = index;
            gamePlayerScript.FinalHeroPos = pos;
            gamePlayerScript.Info = spawnPrefabs[index].GetComponent<GamePlayerController>().Info;

            return gamePlayer;

        }
    }
}