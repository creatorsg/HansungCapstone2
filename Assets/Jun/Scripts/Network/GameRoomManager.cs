using UnityEngine;
using Mirror;
using System.Collections.Generic;

namespace Jun {
    public class GameRoomManager : NetworkRoomManager
    {
        // 현재 방의 고유 ID (PlayFab에 등록된 roomId)
        public string RoomId;

        // 게임에 참여하는 캐릭터의 수
        public int HeroNum = 0;

        public override void OnStopHost()
        {
            base.OnStopHost();
            if (!string.IsNullOrEmpty(RoomId))
            {
                PlayfabCommand.RemoveRoom(RoomId);
                Debug.Log($"[GameRoomManager] 방 제거 요청: {RoomId}");
            }
        }
        // �κ񿡼� ���������� �Ѿ �� �������� ����Ǵ� �Լ�
        public override GameObject OnRoomServerCreateGamePlayer(NetworkConnectionToClient conn, GameObject roomPlayer)
        {
            // �κ� �÷��̾�� �����ߴ� ������ ������
            var roomPlayerScript = roomPlayer.GetComponent<GameRoomPlayer>();
            var roomPlayerCharaterNum = roomPlayerScript.CharaterNum;

            GameObject mainPlayer = null;
            for (int i = 0; i < roomPlayerCharaterNum.Count; i++)
            {
                HeroNum++;
                int index = roomPlayerCharaterNum[i].HeroIndex;
                int pos = roomPlayerCharaterNum[i].HeroPos;

                // �����ӿ� �÷��̾� �������� ����
                GameObject gamePlayer = Instantiate(spawnPrefabs[index]);

                // ������ ���� �÷��̾� ��ũ��Ʈ�� �����͸� ����
                var gamePlayerScript = gamePlayer.GetComponent<GamePlayerController>();
                gamePlayerScript.FinalHeroIndex = index;
                gamePlayerScript.FinalHeroPos = pos;
                gamePlayerScript.Info = spawnPrefabs[index].GetComponent<GamePlayerController>().Info;

                // ���� ĳ���� ��ȯ ó��
                if (i == 0)
                {
                    // ù ��° ĳ���ʹ� �Լ��� ���ϰ����� ���� (Mirror�� �ڵ� ��ȯ)
                    mainPlayer = gamePlayer;
                }
                else
                {
                    // �� ��° ĳ���ͺ��ʹ� �������� NetworkServer.Spawn ȣ��
                    // conn�� �����ؾ� �ش� Ŭ���̾�Ʈ�� �� ĳ������ ����(isLocalPlayer)�� �����ϴ�.
                    NetworkServer.Spawn(gamePlayer, conn);
                }
            }

            // ù ��°�� ������ ĳ���͸� ��ȯ�Ͽ� ������ ���� �������� ����
            return mainPlayer;
        }


    }
}