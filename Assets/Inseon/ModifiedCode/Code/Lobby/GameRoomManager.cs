using Lsy;
using Mirror;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Jun {
    public class GameRoomManager : NetworkRoomManager
    {
        // 현재 방의 고유 ID (PlayFab에 등록된 roomId)
        public string RoomId     = "";
        public string RoomName   = "";
        public bool   IsPrivate  = false;

        // 게임에 참여하는 캐릭터의 수
        public int HeroNum = 0;

        // 연결 순서 기준 PingIndex 카운터 (서버 전용)
        private int _pingIndexCounter = 0;

        // CharacterSelect → Gameplay 경로에서 PlayerData를 미리 스폰했는지 여부.
        // true일 때 OnServerReady에서 SceneLoadedForPlayer 체인을 차단합니다.
        private bool _playerDataPreSpawned = false;

        /// <summary>
        /// 캐릭터 선택 전역 큐 (서버 전용).
        /// 인덱스 = FinalHeroPos. 먼저 선택할수록 낮은 번호를 받습니다.
        /// </summary>
        private readonly List<SelectionEntry> _globalQueue = new List<SelectionEntry>();

        /// <summary>
        /// 캐릭터 선택 씬 이름. Inspector에서 Build Settings의 씬 이름과 동일하게 입력하세요.
        /// </summary>
        public string CharacterSelectScene = "CharacterSelect";

        /// <summary>
        /// Home(아지트) 씬 이름. Build Settings의 씬 이름과 동일하게 입력하세요.
        /// </summary>
        public string HomeScene = "Home";

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
                // HTTP 요청이 전송될 최소 시간 확보 (비동기 요청이라 보장은 안 되지만 확률을 높임)
                System.Threading.Thread.Sleep(300);
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
            // host 연결(conn == localConnection)이 끊기는 경우는 OnStopHost()에서 RemoveRoom으로 처리되므로
            // 여기서는 클라이언트 연결만 LeaveRoom 처리합니다.
            if (!string.IsNullOrEmpty(RoomId) && conn != NetworkServer.localConnection)
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

            if (newSceneName == CharacterSelectScene)
            {
                // 새로운 선택 라운드를 위해 전역 큐 초기화
                _globalQueue.Clear();
                Debug.Log("[GameRoomManager] CharacterSelect 진입 - GlobalQueue 초기화");
            }
            else if (newSceneName == GameplayScene)
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

            // ── 전역 큐 순서대로 PlayerData 스폰 ─────────────────────
            // GlobalQueue 인덱스 = FinalHeroPos (먼저 선택한 캐릭터가 낮은 번호)
            _pingIndexCounter = 0;
            HeroNum           = 0;
            var pingByNetId   = new Dictionary<uint, int>();

            for (int queuePos = 0; queuePos < _globalQueue.Count; queuePos++)
            {
                SelectionEntry entry = _globalQueue[queuePos];
                string code = entry.heroCode;

                if (!NetworkServer.spawned.TryGetValue(entry.ownerNetId, out NetworkIdentity identity))
                {
                    Debug.LogWarning($"[GameRoomManager] ownerNetId={entry.ownerNetId} 오브젝트를 찾을 수 없음. 스킵.");
                    continue;
                }
                NetworkConnectionToClient conn = identity.connectionToClient;

                // 같은 connection의 첫 번째 캐릭터에만 새 PingIndex 부여
                if (!pingByNetId.ContainsKey(entry.ownerNetId))
                    pingByNetId[entry.ownerNetId] = _pingIndexCounter++;
                int pingIdx = pingByNetId[entry.ownerNetId];

                if (!CharacterRegistry.TryGet(code, out var regEntry))
                {
                    Debug.LogError($"[GameRoomManager] GlobalQueue: '{code}'이 CharacterRegistry에 없음");
                    continue;
                }
                if (regEntry.PlayerDataPrefab == null)
                {
                    Debug.LogError($"[GameRoomManager] '{code}'의 PlayerDataPrefab이 null");
                    continue;
                }

                GameObject go = Instantiate(regEntry.PlayerDataPrefab);
                var pd = go.GetComponent<PlayerData>();
                if (pd == null) { Destroy(go); continue; }

                pd.FinalHeroCode  = code;
                pd.FinalHeroPos   = queuePos;   // 큐 인덱스 = heroPos
                pd.FinalHeroIndex = CharacterDatabase.Stats.TryGetValue(code, out var cd) ? cd.index : -1;
                pd.Info           = BuildPlayerInfoFromRegistry(code, pingIdx, regEntry);
                pd.PingIndex      = pingIdx;

                // GameRoomPlayer의 PingIndex도 갱신
                foreach (var slot in roomSlots)
                {
                    var rp = slot as GameRoomPlayer;
                    if (rp != null && rp.connectionToClient == conn)
                    { rp.PingIndex = pingIdx; break; }
                }

                NetworkServer.Spawn(go, conn);
                HeroNum++;
                Debug.Log($"[GameRoomManager] PlayerData 스폰: code={code}, heroPos={queuePos}, pingIdx={pingIdx}");
            }

            _playerDataPreSpawned = true;

            Debug.Log($"[GameRoomManager] 모든 플레이어 캐릭터 선택 완료 → Home씬으로 이동 (HeroNum={HeroNum})");
            ServerChangeScene(HomeScene);
        }

        // ── 전역 큐 관리 (서버 전용) ────────────────────────────────

        /// <summary>
        /// 캐릭터 선택/해제 토글을 전역 큐에 반영합니다.
        /// 성공/실패 무관하게 현재 큐 상태를 브로드캐스트합니다.
        /// </summary>
        [Server]
        public void TrySelectCharacter(uint ownerNetId, string heroCode, int charCountLimit)
        {
            // 이미 내가 선택한 캐릭터면 → 해제
            for (int i = 0; i < _globalQueue.Count; i++)
            {
                if (_globalQueue[i].heroCode == heroCode && _globalQueue[i].ownerNetId == ownerNetId)
                {
                    _globalQueue.RemoveAt(i);
                    Debug.Log($"[GameRoomManager] 선택 해제: code={heroCode}, ownerNetId={ownerNetId}");
                    BroadcastQueue();
                    return;
                }
            }

            // 다른 플레이어가 이미 선택했으면 → 거부
            foreach (var e in _globalQueue)
            {
                if (e.heroCode == heroCode)
                {
                    Debug.Log($"[GameRoomManager] 선택 거부(이미 선택됨): code={heroCode}");
                    BroadcastQueue(); // 클라이언트 낙관적 UI 교정용
                    return;
                }
            }

            // 내 선택 수 초과 → 거부
            int myCount = 0;
            foreach (var e in _globalQueue)
                if (e.ownerNetId == ownerNetId) myCount++;

            if (myCount >= charCountLimit)
            {
                Debug.Log($"[GameRoomManager] 선택 거부(한도 초과): ownerNetId={ownerNetId}, limit={charCountLimit}");
                BroadcastQueue();
                return;
            }

            _globalQueue.Add(new SelectionEntry { heroCode = heroCode, ownerNetId = ownerNetId });
            Debug.Log($"[GameRoomManager] 선택 추가: code={heroCode}, heroPos={_globalQueue.Count - 1}");
            BroadcastQueue();
        }

        /// <summary>
        /// 현재 전역 큐 상태를 모든 클라이언트에 브로드캐스트합니다.
        /// </summary>
        [Server]
        public void BroadcastQueue()
        {
            int count = _globalQueue.Count;
            string[] codes = new string[count];
            uint[]   ids   = new uint[count];
            for (int i = 0; i < count; i++)
            {
                codes[i] = _globalQueue[i].heroCode;
                ids[i]   = _globalQueue[i].ownerNetId;
            }

            // 첫 번째 roomSlot의 RPC를 이용해 전체 클라이언트에 브로드캐스트
            foreach (var slot in roomSlots)
            {
                var p = slot as GameRoomPlayer;
                if (p != null)
                {
                    p.RpcSyncGlobalQueue(codes, ids);
                    return;
                }
            }
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
                    Id = pingIndex,
                    Skills = new List<SkillInfo>(entry.Skills ?? new List<SkillInfo>()),
                    Items       = new List<InventoryItem>(),
                    Expendables = new List<ConsumableInfo>(entry.Items ?? new List<ConsumableInfo>()),
                    Weapon      = entry.Weapon,
                    Armor       = entry.Armor,
                };
            }

            // 스킬/아이템은 CharacterCard에서 Inspector로 직접 설정된 값을 사용합니다.
            var info = new PlayerInfo
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
                UniqueTraitLv = 0,  // 고유 특성 미강화 상태로 시작 (0 = 스탯 기여 없음)
                Skills        = new List<SkillInfo>(entry.Skills ?? new List<SkillInfo>()),
                Items       = new List<InventoryItem>(),
                Expendables = new List<ConsumableInfo>(entry.Items ?? new List<ConsumableInfo>()),
                Weapon      = entry.Weapon,
                Armor       = entry.Armor,
            };

            // ── 무기 스탯 합산 ──────────────────────────────────────────
            ApplyEqpStats(ref info, entry.Weapon);

            // ── 방어구 스탯 합산 ────────────────────────────────────────
            ApplyEqpStats(ref info, entry.Armor);

            // ── 고유 특성 스탯 합산 (UniqueTraitLv 기준, 0이면 미적용) ──
            ApplyTraitStats(ref info, entry.UniqueTrait, info.UniqueTraitLv);

            Debug.Log($"[BuildPlayerInfo] {c.characterName} — " +
                      $"HP:{info.Hp} ATK:{info.Atk} DEF:{info.Def} SPD:{info.Spd} " +
                      $"(무기:{entry.Weapon?.Name ?? "없음"} 방어구:{entry.Armor?.Name ?? "없음"} " +
                      $"특성Lv:{info.Trk1})");

            return info;
        }

        /// <summary>EqpInfo 스탯을 PlayerInfo에 누산합니다.</summary>
        private static void ApplyEqpStats(ref PlayerInfo info, EqpInfo eqp)
        {
            if (eqp == null) return;
            info.Hp    += eqp.Hp;
            info.San   += eqp.San;
            info.Atk   += eqp.Atk;
            info.Def   += eqp.Def;
            info.Spd   += eqp.Spd;
            info.Crit  += eqp.Crit;
            info.Ctm   += eqp.Ctm;
            info.Dodge += eqp.Dodge;
            info.Acc   += eqp.Acc;
            info.Res   += eqp.Res;
        }

        /// <summary>UniqueTraitSO의 지정 단계 스탯을 PlayerInfo에 누산합니다.</summary>
        private static void ApplyTraitStats(ref PlayerInfo info, UniqueTraitSO trait, int level)
        {
            if (trait == null || level < 1) return;
            TraitLevelData d = trait.GetLevel(Mathf.Clamp(level, 1, UniqueTraitSO.MaxLevel));
            info.Hp    += d.hp;
            info.San   += d.san;
            info.Atk   += d.atk;
            info.Def   += d.def;
            info.Spd   += d.spd;
            info.Crit  += d.crit;
            info.Ctm   += d.ctm;
            info.Dodge += d.dodge;
            info.Acc   += d.acc;
            info.Res   += d.res;
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

            // Home 씬: Ready만 전송. PlayerAccount 교체는 서버의 OnServerReady가 담당.
            if (Utils.IsSceneActive(HomeScene))
            {
                if (!NetworkClient.ready) NetworkClient.Ready();
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
            // CharacterSelect 씬: 클라이언트가 Ready되면 현재 큐 상태를 브로드캐스트
            if (Utils.IsSceneActive(CharacterSelectScene))
            {
                NetworkServer.SetClientReady(conn);
                BroadcastQueue();
                Debug.Log($"[GameRoomManager] CharacterSelect Ready 완료, 큐 브로드캐스트. conn={conn}");
                return;
            }








            // Home 씬: SetClientReady 후 PlayerAccount 프리팹으로 플레이어를 직접 교체
            // (클라이언트의 AddPlayer 요청을 기다리지 않으므로 localPlayer 블로킹 문제를 우회)
            if (Utils.IsSceneActive(HomeScene))
            {
                NetworkServer.SetClientReady(conn);

                if (playerPrefab == null)
                {
                    Debug.LogError("[GameRoomManager] OnServerReady(Home): playerPrefab이 null입니다! Inspector에서 PlayerAccount 프리팹을 등록하세요.");
                    return;
                }

                GameObject accountObj = Instantiate(playerPrefab);
                NetworkServer.ReplacePlayerForConnection(conn, accountObj, ReplacePlayerOptions.KeepAuthority);
                Debug.Log($"[GameRoomManager] Home씬 PlayerAccount 스폰 및 교체 완료: conn={conn}");
                return;
            }

            bool shouldBlock = (Utils.IsSceneActive(GameplayScene) && _playerDataPreSpawned);

            if (shouldBlock)
            {
                // NetworkServer.SetClientReady(conn) 는 내부적으로
                //   conn.isReady = true  +  SpawnObserversForConnection(conn)
                // 을 수행합니다.
                // base.OnServerReady 전체를 건너뛰면 SceneLoadedForPlayer 체인
                // (→ OnRoomServerCreateGamePlayer 중복 호출)은 실행되지 않으면서
                // SyncList/SyncVar 최신 상태는 클라이언트에 정상 전달됩니다.

                // [Home 경로 버그 수정]
                // Home → Battle 경로에서 conn.identity 가 PlayerAccount / CharacterUnit 으로
                // 교체된 뒤 Home 씬이 언로드되면 해당 오브젝트가 파괴되어 conn.identity = null.
                // Mirror SetClientReady 내부: "if (conn.identity != null) SpawnObserversForConnection"
                // → identity 가 null 이면 SpawnObserversForConnection 이 스킵되어
                //   DontDestroyOnLoad PlayerData 가 클라이언트에게 전달되지 않음.
                // GameRoomPlayer 는 DontDestroyOnLoad 로 항상 생존하므로
                // ReplacePlayerForConnection 으로 identity 를 복원한다.
                if (conn.identity == null)
                {
                    foreach (var slot in roomSlots)
                    {
                        if (slot is GameRoomPlayer rp && rp.connectionToClient == conn)
                        {
                            Debug.Log($"[GameRoomManager] conn.identity null 감지 → GameRoomPlayer(netId={rp.netId}) 로 identity 복원");
                            NetworkServer.ReplacePlayerForConnection(conn, rp.gameObject, ReplacePlayerOptions.KeepAuthority);
                            break;
                        }
                    }
                }

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
                playerData.PingIndex = myPingIndex;

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
