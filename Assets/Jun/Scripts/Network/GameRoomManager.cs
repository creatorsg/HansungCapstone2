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
            var roomPlayerScript = roomPlayer.GetComponent<GameRoomPlayer>();
            if (roomPlayerScript == null)
            {
                Debug.LogError("[GameRoomManager] roomPlayer에 GameRoomPlayer 컴포넌트가 없습니다!");
                return null;
            }

            var roomPlayerCharaterNum = roomPlayerScript.CharaterNum;
            Debug.Log($"[GameRoomManager] OnRoomServerCreateGamePlayer 호출: conn={conn}, CharaterNum.Count={roomPlayerCharaterNum.Count}, spawnPrefabs.Count={spawnPrefabs.Count}");

            if (roomPlayerCharaterNum.Count == 0)
            {
                Debug.LogWarning($"[GameRoomManager] CharaterNum이 비어 있습니다. playerPrefab 폴백 사용.");
                return null; // Mirror가 playerPrefab으로 폴백
            }

            GameObject mainPlayer = null;
            for (int i = 0; i < roomPlayerCharaterNum.Count; i++)
            {
                HeroNum++;
                int index = roomPlayerCharaterNum[i].HeroIndex;
                int pos = roomPlayerCharaterNum[i].HeroPos;

                // ── 핵심 방어 코드 ──
                if (index < 0 || index >= spawnPrefabs.Count)
                {
                    Debug.LogError($"[GameRoomManager] spawnPrefabs 인덱스 범위 초과! HeroIndex={index}, spawnPrefabs.Count={spawnPrefabs.Count}");
                    // mainPlayer가 null이면 Mirror가 playerPrefab으로 폴백
                    return mainPlayer;
                }
                if (spawnPrefabs[index] == null)
                {
                    Debug.LogError($"[GameRoomManager] spawnPrefabs[{index}]가 null입니다! Inspector에서 프리팹을 확인하세요.");
                    return mainPlayer;
                }

                GameObject gamePlayer = Instantiate(spawnPrefabs[index]);

                var gamePlayerScript = gamePlayer.GetComponent<GamePlayerController>();
                if (gamePlayerScript == null)
                {
                    Debug.LogError($"[GameRoomManager] spawnPrefabs[{index}]에 GamePlayerController가 없습니다!");
                    Destroy(gamePlayer);
                    return mainPlayer;
                }

                gamePlayerScript.FinalHeroIndex = index;
                gamePlayerScript.FinalHeroPos = pos;
                gamePlayerScript.Info = spawnPrefabs[index].GetComponent<GamePlayerController>().Info;

                if (i == 0)
                    mainPlayer = gamePlayer;
                else
                    NetworkServer.Spawn(gamePlayer, conn);
            }

            Debug.Log($"[GameRoomManager] mainPlayer 생성 완료: {mainPlayer?.name ?? "null"}");
            return mainPlayer;
        }

    }
}