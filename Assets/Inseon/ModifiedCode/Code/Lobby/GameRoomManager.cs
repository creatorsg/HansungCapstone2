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

        // CharacterSelect → Gameplay 경로에서 PlayerData를 미리 스폰했는지 여부.
        // true일 때 OnServerReady에서 SceneLoadedForPlayer 체인을 차단합니다.
        private bool _playerDataPreSpawned = false;

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
                if (!_playerDataPreSpawned)
                {
                    // Jun 경로(방→게임 직행): OnRoomServerCreateGamePlayer 호출 전에 초기화
                    _pingIndexCounter = 0;
                    HeroNum = 0;
                    Debug.Log("[GameRoomManager] GameplayScene 진입 - HeroNum/PingIndex 초기화 (Jun 직행 경로)");
                }
                else
                {
                    // Inseon 경로(CharacterSelect→Game): HeroNum은 OnPlayerConfirmedSelection에서
                    // 이미 올바르게 설정됐으므로 덮어쓰지 않는다.
                    Debug.Log($"[GameRoomManager] GameplayScene 진입 - HeroNum 유지: {HeroNum} (CharacterSelect 경로)");
                }
            }
            else if (newSceneName == RoomScene)
            {
                // 게임이 끝나고 방으로 돌아왔을 때 이전 게임의 PlayerData를 정리한다.
                CleanUpPlayerData();
                _playerDataPreSpawned = false;   // 다음 게임을 위해 플래그 초기화
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

            // 클라이언트가 Gameplay 씬 로드 후 Ready를 보낼 때
            // OnRoomServerCreateGamePlayer가 중복 호출되는 것을 막기 위한 플래그
            _playerDataPreSpawned = true;

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
                string code = charaterNum[i].HeroCode;

                if (!CharacterRegistry.TryGet(code, out var entry))
                {
                    Debug.LogError($"[GameRoomManager] SpawnPlayerData: CharacterRegistry에 '{code}'가 없습니다!");
                    continue;
                }
                if (entry.PlayerDataPrefab == null)
                {
                    Debug.LogError($"[GameRoomManager] '{code}'의 PlayerDataPrefab이 null입니다! CharacterCard Inspector를 확인하세요.");
                    continue;
                }
                if (entry.PlayerDataPrefab.GetComponent<PlayerData>() == null)
                {
                    Debug.LogError($"[GameRoomManager] '{code}'의 PlayerDataPrefab에 PlayerData 컴포넌트가 없습니다!");
                    continue;
                }

                GameObject gamePlayer = Instantiate(entry.PlayerDataPrefab);
                var playerData = gamePlayer.GetComponent<PlayerData>();

                int pos = HeroNum; // 전역 순번으로 스폰 위치 할당 (중복 없음)

                playerData.FinalHeroCode  = code;
                playerData.FinalHeroIndex = charaterNum[i].HeroIndex; // 레거시 UI용
                playerData.FinalHeroPos   = pos;
                playerData.Info           = BuildPlayerInfoFromRegistry(code, myPingIndex, entry);
                playerData.PingIndex      = myPingIndex;

                NetworkServer.Spawn(gamePlayer, conn);
                HeroNum++;

                Debug.Log($"[GameRoomManager] PlayerData 스폰: code={code}, Pos={pos}, PingIndex={myPingIndex}, HeroNum={HeroNum}");
            }
        }

        /// <summary>
        /// CharacterRegistry(프리팹/스킬)와 CharacterDatabase(스탯)를 합쳐 PlayerInfo를 빌드합니다.
        /// </summary>
        private static PlayerInfo BuildPlayerInfoFromRegistry(string heroCode, int pingIndex, CharacterRegistry.Entry entry)
        {
            // 스탯은 CharacterDatabase(PlayFab 카탈로그)에서 가져옵니다.
            if (!CharacterDatabase.Stats.TryGetValue(heroCode, out var c))
            {
                Debug.LogWarning($"[GameRoomManager] BuildPlayerInfo: CharacterDatabase에서 '{heroCode}'를 찾지 못했습니다. 기본값 사용.");
                return new PlayerInfo
                {
                    Id     = pingIndex,
                    Skills = new List<SkillInfo>(entry.Skills ?? new List<SkillInfo>()),
                    Items  = new List<ItemInfo>(entry.Items  ?? new List<ItemInfo>()),
                };
            }

            // 스킬/아이템은 CharacterCard에서 Inspector로 직접 설정된 값을 사용합니다.
            return new PlayerInfo
            {
                Id    = pingIndex,
                Name  = c.characterName,
                Hp    = c.hp,
                Atk   = c.attack,
                Def   = c.defense,
                Acc   = c.accuracy,
                Dodge = c.evasion,
                Spd   = c.speed,
                Crit  = c.critical,
                San   = c.stress,
                Res   = c.effectResistance,
                Skills = new List<SkillInfo>(entry.Skills ?? new List<SkillInfo>()),
                Items  = new List<ItemInfo>(entry.Items  ?? new List<ItemInfo>()),
            };
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

        /// <summary>
        /// 클라이언트가 씬 로드 완료 후 Ready 상태를 알릴 때 서버에서 호출됩니다.
        /// NetworkRoomManager의 기본 구현은 내부에서
        ///   OnServerReady → SceneLoadedForPlayer → OnRoomServerCreateGamePlayer
        /// 체인을 실행합니다.
        /// CharacterSelect 씬에서는 캐릭터를 아직 선택하지 않아 CharaterNum이 비어있으므로
        /// 이 체인이 실행되면 연결이 강제 종료됩니다.
        /// CharacterSelect 씬일 때는 conn.isReady만 설정하고 체인을 차단합니다.
        /// </summary>
        public override void OnServerReady(NetworkConnectionToClient conn)
        {
            // CharacterSelect 씬: 캐릭터를 아직 선택하지 않아 CharaterNum이 비어있으므로 차단
            // Gameplay 씬 + 사전 스폰: OnPlayerConfirmedSelection에서 PlayerData를 이미 생성했으므로
            //   SceneLoadedForPlayer → OnRoomServerCreateGamePlayer 체인이 중복 실행되지 않도록 차단
            bool shouldBlock = Utils.IsSceneActive(CharacterSelectScene)
                            || (Utils.IsSceneActive(GameplayScene) && _playerDataPreSpawned);

            if (shouldBlock)
            {
                // NetworkServer.SetClientReady(conn) 는 내부적으로
                //   conn.isReady = true  +  SpawnObserversForConnection(conn)
                // 을 수행합니다.
                // base.OnServerReady 전체를 건너뛰면 SceneLoadedForPlayer 체인
                // (→ OnRoomServerCreateGamePlayer 중복 호출)은 실행되지 않으면서
                // SyncList/SyncVar 최신 상태는 클라이언트에 정상 전달됩니다.
                NetworkServer.SetClientReady(conn);

                Debug.Log($"[GameRoomManager] OnServerReady: SceneLoadedForPlayer 차단 (SetClientReady 수동 호출). conn={conn}, scene={UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
                return;
            }

            base.OnServerReady(conn);
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
                string code = roomPlayerCharaterNum[i].HeroCode;

                if (!CharacterRegistry.TryGet(code, out var entry))
                {
                    Debug.LogError($"[GameRoomManager] OnRoomServerCreateGamePlayer: CharacterRegistry에 '{code}'가 없습니다!");
                    return mainPlayer;
                }
                if (entry.PlayerDataPrefab == null)
                {
                    Debug.LogError($"[GameRoomManager] '{code}'의 PlayerDataPrefab이 null입니다! CharacterCard Inspector를 확인하세요.");
                    return mainPlayer;
                }

                var playerData_prefab = entry.PlayerDataPrefab.GetComponent<PlayerData>();
                if (playerData_prefab == null)
                {
                    Debug.LogError($"[GameRoomManager] '{code}'의 PlayerDataPrefab에 PlayerData 컴포넌트가 없습니다!");
                    return mainPlayer;
                }

                GameObject gamePlayer = Instantiate(entry.PlayerDataPrefab);
                var playerData = gamePlayer.GetComponent<PlayerData>();

                int pos = HeroNum; // 전역 순번으로 스폰 위치 할당
                HeroNum++;

                playerData.FinalHeroCode  = code;
                playerData.FinalHeroIndex = roomPlayerCharaterNum[i].HeroIndex; // 레거시 UI용
                playerData.FinalHeroPos   = pos;
                playerData.Info           = BuildPlayerInfoFromRegistry(code, myPingIndex, entry);
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
