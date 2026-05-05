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

        /// <summary>
        /// 캐릭터 선택 씬 이름. Inspector에서 Build Settings의 씬 이름과 동일하게 입력하세요.
        /// </summary>
        public string CharacterSelectScene = "CharacterSelect";

        public override void OnStopHost()
        {
            // RemoveRoom을 base보다 먼저 호출해야 합니다.
            // base.OnStopHost()가 네트워크 스택을 닫기 전에 HTTP 요청을 보내야
            // 강제 종료나 씬 전환 중에도 PlayFab에 전달될 가능성이 높아집니다.
            if (!string.IsNullOrEmpty(RoomId))
            {
                PlayfabCommand.RemoveRoom(RoomId);
                Debug.Log($"[GameRoomManager] 방 제거 요청: {RoomId}");
                RoomId = "";
            }
            base.OnStopHost();
        }

        /// <summary>
        /// 게임 강제 종료 시 호출됩니다.
        /// 호스트 중이면 방을 제거한 뒤 base에 위임합니다.
        /// </summary>
        public override void OnApplicationQuit()
        {
            if (NetworkServer.active && !string.IsNullOrEmpty(RoomId))
            {
                PlayfabCommand.RemoveRoom(RoomId);
                Debug.Log($"[GameRoomManager] 앱 종료 - 방 제거 요청: {RoomId}");
                RoomId = "";
            }
            base.OnApplicationQuit();
        }

        /// <summary>
        /// 플레이어 한 명이 캐릭터 선택을 확정할 때 서버에서 호출됩니다.
        /// 모든 플레이어가 완료되면 GameplayScene으로 전환합니다.
        /// </summary>
        public void OnPlayerConfirmedSelection()
        {
            // 서버에서만 실행
            if (!NetworkServer.active) return;

            foreach (var slot in roomSlots)
            {
                var player = slot as GameRoomPlayer;
                if (player == null || !player.IsChoiced)
                    return; // 아직 안 끝낸 플레이어 있음
            }

            Debug.Log("[GameRoomManager] 모든 플레이어 캐릭터 선택 완료 → 게임 씬으로 전환");
            ServerChangeScene(GameplayScene);
        }

        // 모든 플레이어가 Ready → 게임 씬으로 전환 직전에 호출됨
        // 방이 로비 목록에서 사라져야 새 플레이어가 들어오지 않음
        public override void OnRoomServerPlayersReady()
        {
            if (!string.IsNullOrEmpty(RoomId))
            {
                PlayfabCommand.RemoveRoom(RoomId);
                Debug.Log($"[GameRoomManager] 게임 시작 - 방 목록에서 제거: {RoomId}");
                RoomId = ""; // 씬 전환 후 OnServerDisconnect 중복 호출 방지
            }

            base.OnRoomServerPlayersReady(); // 씬 전환 실행
        }

        /// <summary>
        /// 클라이언트가 정상/비정상 종료로 연결이 끊겼을 때 서버에서 호출됩니다.
        /// PlayFab playerCount를 롤백하여 유령 인원 문제를 방지합니다.
        /// </summary>
        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            // RoomId가 비어있으면 이미 RemoveRoom됐거나 방이 없는 상태 → 스킵
            if (!string.IsNullOrEmpty(RoomId))
            {
                PlayfabCommand.LeaveRoom(RoomId);
                Debug.Log($"[GameRoomManager] 클라이언트 연결 종료 → PlayFab playerCount 롤백: {RoomId}");
            }

            base.OnServerDisconnect(conn);
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