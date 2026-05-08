using UnityEngine;
using Mirror;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace Jun {
    public class GameRoomManager : NetworkRoomManager
    {
        // 현재 방의 고유 ID (PlayFab에 등록된 roomId)
        public string RoomId   = "";
        public string RoomName = "";

        // 게임에 참여하는 캐릭터의 수
        public int HeroNum = 0;

        // 연결 순서 기준 PingIndex 카운터 (서버 전용)
        private int _pingIndexCounter = 0;

        /// <summary>
        /// 캐릭터 선택 씬 이름. Inspector에서 Build Settings의 씬 이름과 동일하게 입력하세요.
        /// </summary>
        public string CharacterSelectScene = "CharacterSelect";

        // ── PlayFab 연동 ──────────────────────────────────────────────

        public override void OnStopHost()
        {
            if (!string.IsNullOrEmpty(RoomId))
            {
                PlayfabCommand.RemoveRoom(RoomId);
                Debug.Log($"[GameRoomManager] 방 제거 요청: {RoomId}");
                RoomId = "";
            }
            base.OnStopHost();
        }

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

        public override void OnRoomServerPlayersReady()
        {
            // base.OnRoomServerPlayersReady()를 호출하지 않는다.
            // base의 내부는 ServerChangeScene(GameplayScene)으로 즉시 씬을 전환하는데,
            // 이 프로젝트는 방→캐릭터선택→게임 경로를 사용하므로 자동 전환을 막아야 한다.
            // 씬 전환 권한은 오직 호스트의 명시적 버튼 클릭에만 있다.
            if (!string.IsNullOrEmpty(RoomId))
            {
                PlayfabCommand.RemoveRoom(RoomId);
                Debug.Log($"[GameRoomManager] 전원 준비 완료 - 방 목록에서 제거: {RoomId}");
                RoomId = "";
            }
        }

        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            if (!string.IsNullOrEmpty(RoomId))
            {
                PlayfabCommand.LeaveRoom(RoomId);
                Debug.Log($"[GameRoomManager] 클라이언트 연결 종료 → PlayFab playerCount 롤백: {RoomId}");
            }
            base.OnServerDisconnect(conn);
        }

        // ── 씬 전환 시 카운터 초기화 & PlayerData 정리 ───────────────

        /// <summary>
        /// 서버에서 씬 전환이 완료될 때 호출됩니다.
        /// GameplayScene 진입 시 HeroNum / PingIndexCounter를 초기화하고,
        /// RoomScene 복귀 시 이전 게임의 PlayerData를 전부 정리합니다.
        /// </summary>
        public override void OnRoomServerSceneChanged(string newSceneName)
        {
            base.OnRoomServerSceneChanged(newSceneName);

            if (newSceneName == GameplayScene)
            {
                // Jun 경로(방→게임 직행)에서 OnRoomServerCreateGamePlayer가 호출되기 전에
                // 카운터를 초기화한다. Inseon 경로(캐릭터선택→게임)는 OnPlayerConfirmedSelection
                // 내부에서 별도로 초기화하므로 이 시점에 먼저 초기화해도 덮어쓰기가 발생하지 않는다.
                _pingIndexCounter = 0;
                HeroNum = 0;
                Debug.Log("[GameRoomManager] GameplayScene 진입 - HeroNum / PingIndexCounter 초기화");
            }
            else if (newSceneName == RoomScene)
            {
                // 게임이 끝나고 방으로 돌아왔을 때 이전 게임의 PlayerData를 정리한다.
                CleanUpPlayerData();
            }
        }

        /// <summary>
        /// DontDestroyOnLoad로 남아있는 PlayerData를 서버에서 전부 파괴합니다.
        /// NetworkServer.Destroy는 서버와 모든 클라이언트에서 동시에 오브젝트를 제거합니다.
        /// </summary>
        private void CleanUpPlayerData()
        {
            if (!NetworkServer.active) return;

            var remaining = FindObjectsByType<PlayerData>(FindObjectsSortMode.None);
            if (remaining.Length == 0) return;

            Debug.Log($"[GameRoomManager] 이전 게임 PlayerData {remaining.Length}개 정리");
            foreach (var pd in remaining)
                NetworkServer.Destroy(pd.gameObject);
        }

        // ── 캐릭터 선택 확정 흐름 ────────────────────────────────────

        /// <summary>
        /// 플레이어 한 명이 캐릭터 선택을 확정할 때 서버에서 호출됩니다.
        /// 모든 플레이어가 완료되면 PlayerData를 미리 생성(DontDestroyOnLoad)한 뒤
        /// GameplayScene으로 전환합니다.
        /// Mirror의 OnRoomServerCreateGamePlayer는 CharacterSelect→Gameplay 전환 시
        /// 호출되지 않으므로, 씬 전환 전에 직접 PlayerData를 스폰합니다.
        /// </summary>
        public void OnPlayerConfirmedSelection()
        {
            if (!NetworkServer.active) return;

            foreach (var slot in roomSlots)
            {
                var player = slot as GameRoomPlayer;
                if (player == null || !player.IsChoiced)
                    return;
            }

            // ── PlayerData 미리 생성 ──────────────────────────────────
            // CharacterSelect → Gameplay 전환이라 Mirror가 OnRoomServerCreateGamePlayer를
            // 호출하지 않으므로, 씬 전환 직전에 서버에서 PlayerData를 직접 스폰합니다.
            _pingIndexCounter = 0;
            HeroNum = 0;
            foreach (var slot in roomSlots)
            {
                var roomPlayer = slot as GameRoomPlayer;
                if (roomPlayer == null) continue;
                SpawnPlayerDataForPlayer(roomPlayer.connectionToClient, roomPlayer);
            }

            Debug.Log($"[GameRoomManager] 모든 플레이어 캐릭터 선택 완료 → 게임 씬으로 전환 (HeroNum={HeroNum})");
            ServerChangeScene(GameplayScene);
        }

        /// <summary>
        /// 한 플레이어의 캐릭터 데이터(CharaterNum)를 기반으로 PlayerData 오브젝트를 스폰합니다.
        /// PlayerData는 DontDestroyOnLoad이므로 GameplayScene까지 유지됩니다.
        /// </summary>
        private void SpawnPlayerDataForPlayer(NetworkConnectionToClient conn, GameRoomPlayer roomPlayerScript)
        {
            var charaterNum = roomPlayerScript.CharaterNum;
            if (charaterNum.Count == 0)
            {
                Debug.LogWarning($"[GameRoomManager] SpawnPlayerData: CharaterNum이 비어있습니다! conn={conn}");
                return;
            }

            int myPingIndex = _pingIndexCounter++;
            roomPlayerScript.PingIndex = myPingIndex;

            for (int i = 0; i < charaterNum.Count; i++)
            {
                int index = charaterNum[i].HeroIndex;
                int pos   = charaterNum[i].HeroPos;

                if (index < 0 || index >= spawnPrefabs.Count || spawnPrefabs[index] == null)
                {
                    Debug.LogError($"[GameRoomManager] spawnPrefabs 인덱스 오류! HeroIndex={index}");
                    continue;
                }

                var prefab     = spawnPrefabs[index];
                var prefabData = prefab.GetComponent<PlayerData>();
                if (prefabData == null)
                {
                    Debug.LogError($"[GameRoomManager] spawnPrefabs[{index}]에 PlayerData가 없습니다!");
                    continue;
                }

                GameObject gamePlayer = Instantiate(prefab);
                var playerData = gamePlayer.GetComponent<PlayerData>();

                playerData.FinalHeroIndex = index;
                playerData.FinalHeroPos   = pos;
                playerData.Info           = prefabData.Info;
                playerData.PingIndex      = myPingIndex;

                NetworkServer.Spawn(gamePlayer, conn);
                HeroNum++;

                Debug.Log($"[GameRoomManager] PlayerData 스폰: HeroIndex={index}, Pos={pos}, PingIndex={myPingIndex}, HeroNum={HeroNum}");
            }
        }

        // ── Mirror 씬 전환 보호 ──────────────────────────────────────

        /// <summary>
        /// Mirror 기본 구현은 Room씬이 아닌 곳에서 AddPlayer 요청이 오면
        /// conn.Disconnect()를 호출합니다.
        /// CharacterSelect → GameplayScene 전환 시 클라이언트가 자동으로
        /// AddPlayerMessage를 보내므로, Room씬이 아닐 때는 연결을 유지한 채 스킵합니다.
        /// </summary>
        public override void OnServerAddPlayer(NetworkConnectionToClient conn)
        {
            if (!Utils.IsSceneActive(RoomScene))
            {
                Debug.Log($"[GameRoomManager] OnServerAddPlayer: Room씬 아님 → 스킵 (scene={SceneManager.GetActiveScene().name})");
                return;
            }
            base.OnServerAddPlayer(conn);
        }

        /// <summary>
        /// 클라이언트 씬 전환 완료 시 호출됩니다.
        /// Room씬에서만 base(AddPlayer 요청)를 실행하고,
        /// CharacterSelect / GameplayScene에서는 AddPlayer 요청을 방지합니다.
        /// </summary>
        public override void OnClientSceneChanged()
        {
            if (Utils.IsSceneActive(RoomScene))
            {
                base.OnClientSceneChanged();
                return;
            }
            // CharacterSelect · Gameplay씬: NetworkClient.Ready만 보장하고 AddPlayer는 생략
            if (!NetworkClient.ready)
                NetworkClient.Ready();
        }

        // ── 게임 플레이어 생성 ───────────────────────────────────────

        /// <summary>
        /// 룸 → 게임씬 전환 시 플레이어마다 호출됩니다.
        /// PlayerData(DontDestroyOnLoad)를 스폰하여 씬 전환 후에도 데이터가 유지되도록 합니다.
        /// BattleManager.SetupBattleFlow 에서 PlayerData를 찾아 GamePlayerController에 InjectData합니다.
        /// </summary>
        public override GameObject OnRoomServerCreateGamePlayer(NetworkConnectionToClient conn, GameObject roomPlayer)
        {
            var roomPlayerScript = roomPlayer.GetComponent<GameRoomPlayer>();
            if (roomPlayerScript == null)
            {
                Debug.LogError("[GameRoomManager] roomPlayer에 GameRoomPlayer 컴포넌트가 없습니다!");
                return null;
            }

            var roomPlayerCharaterNum = roomPlayerScript.CharaterNum;
            Debug.Log($"[GameRoomManager] OnRoomServerCreateGamePlayer: conn={conn}, CharaterNum.Count={roomPlayerCharaterNum.Count}");

            if (roomPlayerCharaterNum.Count == 0)
            {
                // null을 반환하면 Mirror가 playerPrefab을 폴백으로 스폰하므로 허용하지 않는다.
                // 캐릭터를 선택하지 않은 채 게임 씬에 진입하는 것은 비정상이므로 연결을 끊는다.
                Debug.LogError($"[GameRoomManager] CharaterNum이 비어 있습니다. 해당 클라이언트 연결 종료. conn={conn}");
                conn.Disconnect();
                return null;
            }

            // HeroNum / _pingIndexCounter는 OnRoomServerSceneChanged(GameplayScene)에서 이미 초기화됨.
            // 이 연결의 고정 PingIndex 부여
            int myPingIndex = _pingIndexCounter++;
            roomPlayerScript.PingIndex = myPingIndex;

            GameObject mainPlayer = null;
            for (int i = 0; i < roomPlayerCharaterNum.Count; i++)
            {
                HeroNum++;
                int index = roomPlayerCharaterNum[i].HeroIndex;
                int pos   = roomPlayerCharaterNum[i].HeroPos;

                if (index < 0 || index >= spawnPrefabs.Count)
                {
                    Debug.LogError($"[GameRoomManager] spawnPrefabs 인덱스 범위 초과! HeroIndex={index}");
                    return mainPlayer;
                }
                if (spawnPrefabs[index] == null)
                {
                    Debug.LogError($"[GameRoomManager] spawnPrefabs[{index}]가 null입니다!");
                    return mainPlayer;
                }

                GameObject gamePlayer = Instantiate(spawnPrefabs[index]);

                // PlayerData 컴포넌트에 데이터 주입
                // BattleManager가 FindObjectsByType<PlayerData>()로 이 오브젝트를 찾습니다.
                var playerData = gamePlayer.GetComponent<PlayerData>();
                if (playerData == null)
                {
                    Debug.LogError($"[GameRoomManager] spawnPrefabs[{index}]에 PlayerData 컴포넌트가 없습니다! Inspector에서 프리팹을 확인하세요.");
                    Destroy(gamePlayer);
                    return mainPlayer;
                }

                playerData.FinalHeroIndex = index;
                playerData.FinalHeroPos   = pos;
                playerData.Info           = spawnPrefabs[index].GetComponent<PlayerData>().Info;
                playerData.PingIndex      = myPingIndex;

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
