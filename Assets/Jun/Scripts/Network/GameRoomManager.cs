using UnityEngine;
using Mirror;
using System.Collections.Generic;

namespace Jun {
    public class GameRoomManager : NetworkRoomManager
    {
        // 현재 방의 고유 ID (PlayFab에 등록된 roomId)
        // Host는 StartHost() 전에, Client는 StartClient() 전에 직접 세팅합니다.
        // (PlayFab에서 이미 데이터를 가져오므로 SyncVar 불필요)
        public string RoomId   = "";
        public string RoomName = "";

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

        // 모든 플레이어가 Ready → 게임 씬으로 전환 직전에 호출됨
        // 방이 로비 목록에서 사라져야 새 플레이어가 들어오지 않음
        public override void OnRoomServerPlayersReady()
        {
            if (!string.IsNullOrEmpty(RoomId))
            {
                PlayfabCommand.RemoveRoom(RoomId);
                Debug.Log($"[GameRoomManager] 게임 시작 - 방 목록에서 제거: {RoomId}");
            }

            base.OnRoomServerPlayersReady(); // 씬 전환 실행
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