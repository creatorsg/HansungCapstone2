using UnityEngine;
using Mirror;
using NUnit.Framework;
using System.Collections.Generic;

namespace Jun {
    public class GameRoomManager : NetworkRoomManager
    {
        // 로비에서 본게임으로 넘어갈 때 서버에서 실행되는 함수
        public override GameObject OnRoomServerCreateGamePlayer(NetworkConnectionToClient conn, GameObject roomPlayer)
        {
            // 로비 플레이어에서 선택했던 정보를 꺼내기
            var roomPlayerScript = roomPlayer.GetComponent<GameRoomPlayer>();
            var roomPlayerCharaterNum = roomPlayerScript.CharaterNum;

            GameObject mainPlayer = null;
            for (int i = 0; i < roomPlayerCharaterNum.Count; i++)
            {
                int index = roomPlayerCharaterNum[i].HeroIndex;
                int pos = roomPlayerCharaterNum[i].HeroPos;

                // 본게임용 플레이어 프리팹을 생성
                GameObject gamePlayer = Instantiate(spawnPrefabs[index]);

                // 생성된 게임 플레이어 스크립트에 데이터를 주입
                var gamePlayerScript = gamePlayer.GetComponent<GamePlayerController>();
                gamePlayerScript.FinalHeroIndex = index;
                gamePlayerScript.FinalHeroPos = pos;
                gamePlayerScript.Info = spawnPrefabs[index].GetComponent<GamePlayerController>().Info;

                // 다중 캐릭터 소환 처리
                if (i == 0)
                {
                    // 첫 번째 캐릭터는 함수의 리턴값으로 지정 (Mirror가 자동 소환)
                    mainPlayer = gamePlayer;
                }
                else
                {
                    // 두 번째 캐릭터부터는 수동으로 NetworkServer.Spawn 호출
                    // conn을 전달해야 해당 클라이언트가 이 캐릭터의 권한(isLocalPlayer)을 가집니다.
                    NetworkServer.Spawn(gamePlayer, conn);
                }
            }

            // 첫 번째로 생성된 캐릭터를 반환하여 연결의 메인 유닛으로 설정
            return mainPlayer;
        }


    }
}