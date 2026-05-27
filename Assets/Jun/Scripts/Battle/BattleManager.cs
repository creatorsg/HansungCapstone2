using Mirror;
using Mirror.BouncyCastle.Security;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using Unity.VisualScripting.Dependencies.NCalc;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// ��Ʋ�� ���������� ��ϴ� ����
namespace Jun
{
    public class BattleManager : NetworkBehaviour
    {
        [SerializeField] private BattleLogic _logic;
        public static BattleManager Instance;

        [Header("플레이어 목록")]
        public readonly SyncList<GamePlayerController> _players = new SyncList<GamePlayerController>();
        [Header("플레이어 스폰 위치")]
        [SerializeField] private List<Transform> _spawnPoints; public List<Transform> SpawnPoints => _spawnPoints;
        [Header("캐릭터 정보 창")] // [수정] 깨진 Header 문자열 복구
        [SerializeField] private CanvasGroup _unitPanel;
        [SerializeField] private Image _charaterIMG; public Image CharaterIMG => _charaterIMG;
        [Header("플레이어 정보창 UI")]
        [SerializeField] private List<Button> _skillBTN; public List<Button> SkillBTN => _skillBTN;
        [SerializeField] private Button _movePosBTN; public Button MovePosBTN => _movePosBTN;
        [SerializeField] private List<Image> _equiIMG; public List<Image> EquiIMG => _equiIMG;
        [SerializeField] private List<Button> _itemBTN; public List<Button> ItemsBTN => _itemBTN;
        [SerializeField] private TextMeshProUGUI _type; public TextMeshProUGUI Type => _type;
        [SerializeField] private TextMeshProUGUI _hp; public TextMeshProUGUI Hp => _hp;
        [SerializeField] private TextMeshProUGUI _san; public TextMeshProUGUI San => _san;
        [SerializeField] private TextMeshProUGUI _acc; public TextMeshProUGUI Acc => _acc;
        [SerializeField] private TextMeshProUGUI _crit; public TextMeshProUGUI Crit => _crit;
        [SerializeField] private TextMeshProUGUI _dmg; public TextMeshProUGUI Dmg => _dmg;
        [SerializeField] private TextMeshProUGUI _prot; public TextMeshProUGUI Prot => _prot;
        [SerializeField] private TextMeshProUGUI _res; public TextMeshProUGUI Res => _res;
        [SerializeField] private TextMeshProUGUI _dodge; public TextMeshProUGUI Dodge => _dodge;
        [SerializeField] private TextMeshProUGUI _name; public TextMeshProUGUI Name => _name;

        [Header("스테이지")]
        [SerializeField] private int _stageNum = 1; public int StageNum => _stageNum;
        [SerializeField] private List<BattleEnemyInfo> _enemys; public List<BattleEnemyInfo> Enemys => _enemys;
        [Header("적 정보창 UI")]
        [SerializeField] private GameObject _enemyPanel; public GameObject EnemyPanel => _enemyPanel;
        [SerializeField] private Image _enemyUI;
        [SerializeField] private TextMeshProUGUI _enemyName;
        [SerializeField] private TextMeshProUGUI _enemyType; public TextMeshProUGUI EnemyType => _enemyType;
        [SerializeField] private List<Button> _enemySkillBTN; public List<Button> EnemySkillBTN => _enemySkillBTN;
        [SerializeField] private TextMeshProUGUI _enemyhp; public TextMeshProUGUI EnemyHp => _enemyhp;
        [SerializeField] private TextMeshProUGUI _enemysan; public TextMeshProUGUI EnemySan => _enemysan;
        [SerializeField] private TextMeshProUGUI _enemyacc; public TextMeshProUGUI EnemyAcc => _enemyacc;
        [SerializeField] private TextMeshProUGUI _enemycrit; public TextMeshProUGUI EnemyCrit => _enemycrit;
        [SerializeField] private TextMeshProUGUI _enemydmg; public TextMeshProUGUI EnemyDmg => _enemydmg;
        [SerializeField] private TextMeshProUGUI _enemyprot; public TextMeshProUGUI EnemyProt => _enemyprot;
        [SerializeField] private TextMeshProUGUI _enemyres; public TextMeshProUGUI EnemyRes => _enemyres;
        [SerializeField] private TextMeshProUGUI _enemydodge; public TextMeshProUGUI EnemyDodge => _enemydodge;
        public int EnemyNum;

        [Header("�� ����")]
        [SerializeField] private Transform _turnPanel;  //�� �����ִ� ���
        [SerializeField] private Image _turnUi;  // ���� �������� �̹���
        [SerializeField] private List<Image> _turnUIList; 
        [SerializeField] private TextMeshProUGUI _turnUI;  
        public List<TurnData> _turnList = new List<TurnData>();
        public GamePlayerController CurrentTurnUnit { get; private set; }

        [SerializeField] private RootingSystem _rootingSystem;

        [Header("배틀 결과창")] // [수정] 깨진 Header 문자열 복구
        [SerializeField] private BattleResultPanel _battleResultPanel;

        // 동의 카운터 (서버 전용)
        private int  _agreeCount;
        private bool _resultIsVictory;

        [System.Serializable]
        public class PingSystem
        {
            public GameObject MyPing;
            public GameObject IsPing = null;
           
        }

        [Header("�� �ý���")]
        [SerializeField] private List<GameObject> _pingList = new List<GameObject>();



        public int Order = -1;


        private void Update()
        {
            if (Mouse.current == null) return;
            if (!Mouse.current.leftButton.wasPressedThisFrame) return;

            // UI 위에서 클릭하면 Physics2D 무시
            if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;

            // Physics2D 직접 레이캐스트 — EventSystem / Canvas에 영향받지 않습니다.
            Vector2 screenPos = Mouse.current.position.ReadValue();
            Vector2 worldPos  = Camera.main.ScreenToWorldPoint(screenPos);
            Collider2D hit = Physics2D.OverlapPoint(worldPos);
            if (hit == null) return;

            var unit = hit.GetComponent<GamePlayerController>();
            if (unit != null) unit.OnClickedUnit();
        }

        private void Awake()
        {
            Instance = this;
            //  �г� ��Ȱ�� ó��
            _unitPanel.interactable = false;
            _unitPanel.blocksRaycasts = false;
            _unitPanel.alpha =  0.5f;
            EnemyNum = _enemys[StageNum - 1].Enemys.Count;
        }
        public override void OnStartServer()
        {
            base.OnStartServer();

            // ������ ���� ������ ������ �ʱ�ȭ 
            _players.Clear();
            _turnList.Clear();
            Order = -1;

            // ���������� �� �� ������ �߰�.
            for (int i = 0; i < _enemys[StageNum - 1].Enemys.Count; i++)
            {
                _turnList.Add(new TurnData("Enemy", _enemys[StageNum - 1].Enemys[i].GetComponent<EnemyController>().Info.Spd, i));
            }
            StartCoroutine(SetupBattleFlow());
        }
        [Server]
        private IEnumerator SetupBattleFlow()
        {
            var roomManager = NetworkManager.singleton as GameRoomManager;
            Debug.Log($"[SetupBattleFlow] 시작. HeroNum={roomManager?.HeroNum}");

            // ── CharacterRegistry 상태 확인 ──────────────────────────────
            Debug.Log($"[SetupBattleFlow] CharacterRegistry 등록 수: {CharacterRegistry.All.Count}");
            foreach (var kv in CharacterRegistry.All)
                Debug.Log($"  Registry: {kv.Key} / PlayerDataPrefab={kv.Value.PlayerDataPrefab?.name ?? "null"} / BattleUnitPrefab={kv.Value.BattleUnitPrefab?.name ?? "null"}");

            // HeroNum이 확정될 때까지 최대 5초 대기 (OnRoomServerSceneChanged 타이밍 차이 대응)
            float heroNumWait = 5f;
            while (roomManager != null && roomManager.HeroNum <= 0 && heroNumWait > 0f)
            {
                heroNumWait -= Time.deltaTime;
                yield return null;
            }

            if (roomManager == null || roomManager.HeroNum <= 0)
            {
                Debug.LogError($"[SetupBattleFlow] HeroNum이 0입니다! " +
                               $"CharacterSelect에서 CmdConfirmSelection이 호출됐는지, " +
                               $"GameRoomManager._playerDataPreSpawned가 true인지 확인하세요.");
                yield break;
            }

            Debug.Log($"[SetupBattleFlow] HeroNum 확인: {roomManager.HeroNum}. PlayerData 대기 시작.");

            PlayerData[] survivors;
            int waitFrames = 0;
            const int MAX_WAIT_FRAMES = 1800; // ~30초 (60fps 기준)
            while (true)
            {
                survivors = FindObjectsByType<PlayerData>(FindObjectsSortMode.None);
                if (waitFrames % 60 == 0)
                    Debug.Log($"[SetupBattleFlow] 대기 중... survivors={survivors.Length}, HeroNum={roomManager.HeroNum}");
                waitFrames++;

                if (survivors.Length == roomManager.HeroNum) break;

                if (waitFrames >= MAX_WAIT_FRAMES)
                {
                    Debug.LogError($"[SetupBattleFlow] 타임아웃! survivors={survivors.Length}, HeroNum={roomManager.HeroNum}\n" +
                                   $"원인: PlayerData.Awake()에서 DontDestroyOnLoad가 호출되지 않아 씬 전환 시 파괴됐거나,\n" +
                                   $"SpawnPlayerDataForPlayer()에서 스폰이 실패했을 수 있습니다.");
                    yield break;
                }

                yield return null;
            }

            Debug.Log($"[SetupBattleFlow] 전원 도착! survivors={survivors.Length}, HeroNum={roomManager.HeroNum}");

            foreach (var data in survivors)
            {
                Debug.Log($"[SetupBattleFlow] PlayerData 처리: code='{data.FinalHeroCode}', Pos={data.FinalHeroPos}, conn={data.connectionToClient}");

                // ── 유효성 검사 ────────────────────────────────────────────
                if (string.IsNullOrEmpty(data.FinalHeroCode))
                {
                    Debug.LogError($"[SetupBattleFlow] FinalHeroCode가 비어 있습니다! " +
                                   $"CMDChoiceHero가 호출되기 전에 CmdConfirmSelection이 처리됐을 수 있습니다.");
                    continue;
                }

                if (data.FinalHeroPos < 0 || data.FinalHeroPos >= SpawnPoints.Count)
                {
                    Debug.LogError($"[SetupBattleFlow] FinalHeroPos={data.FinalHeroPos}가 SpawnPoints 범위를 벗어납니다! " +
                                   $"SpawnPoints 수={SpawnPoints.Count}");
                    continue;
                }

                // 1. CharacterRegistry에서 코드 기반으로 BattleUnit 프리팹 조회
                if (!CharacterRegistry.TryGet(data.FinalHeroCode, out var entry))
                {
                    Debug.LogError($"[SetupBattleFlow] CharacterRegistry에 '{data.FinalHeroCode}'가 없습니다!\n" +
                                   $"CharacterCard의 CharacterCode가 PlayFab ItemId와 일치하는지,\n" +
                                   $"CharacterSelectManager.InitCards()가 정상 완료됐는지 확인하세요.");
                    continue;
                }

                if (entry.BattleUnitPrefab == null)
                {
                    Debug.LogError($"[SetupBattleFlow] '{data.FinalHeroCode}'의 BattleUnitPrefab이 null입니다! " +
                                   $"CharacterCard Inspector에서 battleUnitPrefab을 연결하세요.");
                    continue;
                }

                // 2. 스폰 위치에 배틀 유닛 인스턴스화
                GameObject battleObj = Instantiate(
                    entry.BattleUnitPrefab,
                    SpawnPoints[data.FinalHeroPos].position,
                    Quaternion.identity);

                // 3. GamePlayerController에 PlayerData 주입
                var controller = battleObj.GetComponent<GamePlayerController>();
                if (controller == null)
                {
                    Debug.LogError($"[SetupBattleFlow] '{entry.BattleUnitPrefab.name}'에 GamePlayerController가 없습니다!");
                    Destroy(battleObj);
                    continue;
                }
                controller.InjectData(data);

                // 4. 해당 클라이언트 소유권으로 스폰
                NetworkServer.Spawn(battleObj, data.connectionToClient);

                // 5. 배틀 플레이어 + 턴 목록에 추가
                _players.Add(controller);
                _turnList.Add(new TurnData("Player", data.Info.Spd, _players.Count - 1));

                Debug.Log($"[SetupBattleFlow] 스폰 완료: {entry.BattleUnitPrefab.name}, Pos={data.FinalHeroPos}, PingIndex={data.PingIndex}");
            }

            if (_players.Count == 0)
            {
                Debug.LogError("[SetupBattleFlow] 스폰된 플레이어가 0명입니다! 위의 에러 로그를 확인하세요.");
                yield break;
            }

            Debug.Log($"[SetupBattleFlow] 전체 스폰 완료. 플레이어={_players.Count}명. 1초 후 첫 턴 시작.");
            Invoke(nameof(StartFirstTurn), 1.0f);
        }

        [Server]
        private void StartFirstTurn()
        {
            Order = -1; // Ȯ���ϰ� �ʱ�ȭ
            _turnList.Sort((a, b) => b.speed.CompareTo(a.speed));
            RpcTurnListUpdate(_turnList.ToArray());
            NextTurn();
        }
        [ClientRpc]
        private void RpcTurnListUpdate(TurnData[] turnDataArray)
        {
            _turnList = new List<TurnData>(turnDataArray);

            for (int i = 0; i < turnDataArray.Length; i++)
            {
                Image turnUI = Instantiate(_turnUi, _turnPanel);
                Sprite sp = null;

                int targetNum = turnDataArray[i].num;

                if (turnDataArray[i].type == "Enemy")
                {
                    sp = _enemys[StageNum - 1].Enemys[targetNum].GetComponent<SpriteRenderer>().sprite;
                }
                else
                {
                    // �÷��̾�� ������ �� �ð��� �ɸ� �� ������ ��� �ڵ� ���
                    if (targetNum < _players.Count)
                    {
                        sp = _players[targetNum].GetCharacterSprite();
                    }
                    else
                    {
                        Debug.LogWarning($"���� {targetNum}�� �÷��̾ �� ���Խ��ϴ�.");
                    }
                }

                turnUI.sprite = sp;
                _turnUIList.Add(turnUI);
            }
        }
        [ClientRpc]
        public void RpcSetHighlight(int index, bool isTurn)
        {
            if (index < 0 || index >= _turnUIList.Count) return;

            Debug.Log($"�� UI ������Ʈ - �ε���: {index}, ����: {isTurn}");
            GameObject go = _turnUIList[index].transform.Find("HighLight").gameObject;
            go.SetActive(isTurn);
        }
        // ���� ������ ����
        [Server]
        public void NextTurn()
        {
            //���� �� ���̶���Ʈ ����
            RpcSetHighlight(Order, false);

            Order = (Order + 1) % _turnList.Count;
            if (Order == 0) _turnList.Sort((a, b) => b.speed.CompareTo(a.speed));

            string currentType = _turnList[Order].type;
            int currentNum = _turnList[Order].num;

            Debug.Log("남은 적 수: " + EnemyNum);
            if (EnemyNum == 0)
            {
                Debug.Log("EndStage");
                NextStage();
                return;
            }

            // 죽은 유닛 턴 스킵
            if (currentType == "Player")
            {
                if (currentNum < 0 || currentNum >= _players.Count ||
                    _players[currentNum] == null || _players[currentNum].Info == null ||
                    _players[currentNum].Info.Hp <= 0)
                {
                    Debug.Log($"[NextTurn] 죽은 플레이어({currentNum}) 턴 스킵");

                    // 전원 사망 체크 → 패배 처리
                    if (GetAlivePlayers().Count == 0)
                    {
                        Debug.Log("[NextTurn] 모든 플레이어 사망 → 패배");
                        ShowBattleResult(false);
                        return;
                    }

                    NextTurn();
                    return;
                }
            }
            else if (currentType == "Enemy")
            {
                var enemyList = _enemys[StageNum - 1].Enemys;
                if (currentNum < 0 || currentNum >= enemyList.Count ||
                    enemyList[currentNum] == null || enemyList[currentNum].Info == null ||
                    enemyList[currentNum].Info.Hp <= 0 || !enemyList[currentNum].gameObject.activeSelf)
                {
                    Debug.Log($"[NextTurn] 죽은 적({currentNum}) 턴 스킵");
                    NextTurn();
                    return;
                }
            }

            RpcChangeTurn(currentType, currentNum);
            RpcSetHighlight(Order, true);

            // 적 턴이면 서버에서 AI → 전투 → NextTurn 코루틴 시작
            if (currentType == "Enemy")
            {
                StartCoroutine(ServerEnemyTurn(currentNum));
            }
        }

        /// <summary>
        /// 서버 전용. 적 AI 판단 → 스킬 실행 → 애니메이션 대기 → 다음 턴.
        /// </summary>
        [Server]
        private IEnumerator ServerEnemyTurn(int enemyIndex)
        {
            yield return new WaitForSeconds(0.5f); // 턴 전환 연출 대기

            // 적 가져오기
            var enemyList = _enemys[StageNum - 1].Enemys;
            if (enemyIndex < 0 || enemyIndex >= enemyList.Count)
            {
                Debug.LogError($"[ServerEnemyTurn] enemyIndex={enemyIndex} 범위 초과");
                NextTurn();
                yield break;
            }

            var enemyCtrl = enemyList[enemyIndex];
            if (enemyCtrl == null || enemyCtrl.Info == null || enemyCtrl.Info.Hp <= 0)
            {
                Debug.Log("[ServerEnemyTurn] 죽은 적의 턴 → 스킵");
                NextTurn();
                yield break;
            }

            // 스턴 체크
            if (CombatCalculator.IsStunned(enemyCtrl.Effects))
            {
                Debug.Log($"[ServerEnemyTurn] {enemyCtrl.Info.Name} 스턴 상태 → 스킵");
                NextTurn();
                yield break;
            }

            // 살아있는 플레이어 체크
            if (GetAlivePlayers().Count == 0)
            {
                Debug.Log("[ServerEnemyTurn] 살아있는 플레이어 없음 → 패배");
                ShowBattleResult(false);
                yield break;
            }

            // AI 판단 (원본 리스트를 넘겨서 원본 인덱스를 반환받음)
            var allEnemies = _enemys[StageNum - 1].Enemys;
            var action = EnemyAI.ChooseAction(enemyCtrl, _players, allEnemies);
            if (action == null)
            {
                Debug.Log($"[ServerEnemyTurn] {enemyCtrl.Info.Name} 행동 불가 → 스킵");
                yield return new WaitForSeconds(0.5f);
                NextTurn();
                yield break;
            }

            var chosenAction = action.Value;
            var skill = enemyCtrl.Info.Skills[chosenAction.skillIndex];
            Debug.Log($"[ServerEnemyTurn] {enemyCtrl.Info.Name} → 스킬 '{skill.Name}' 사용");

            // 전투 실행 (targets는 원본 _players/_enemys 인덱스)
            _logic.EnemyBattleAction(enemyCtrl, chosenAction.skillIndex, chosenAction.targets);

            // 애니메이션 대기 (코루틴 방식)
            float animWait = string.IsNullOrEmpty(skill.anim) ? 0.5f : 1.5f;
            yield return new WaitForSeconds(animWait);

            enemyCtrl.RpcStopAnim();

            NextTurn();
        }

        /// <summary>살아있는 플레이어 목록 반환</summary>
        public List<GamePlayerController> GetAlivePlayers()
        {
            var alive = new List<GamePlayerController>();
            foreach (var p in _players)
            {
                if (p != null && p.Info != null && p.Info.Hp > 0)
                    alive.Add(p);
            }
            return alive;
        }

        /// <summary>살아있는 적 목록 반환</summary>
        public List<EnemyController> GetAliveEnemies()
        {
            var alive = new List<EnemyController>();
            var enemyList = _enemys[StageNum - 1].Enemys;
            foreach (var e in enemyList)
            {
                if (e != null && e.Info != null && e.Info.Hp > 0 && e.gameObject.activeSelf)
                    alive.Add(e);
            }
            return alive;
        }
        // �� ����
        [ClientRpc]
        public void RpcChangeTurn(string typeTurn, int turnNum)
        {
            if (CurrentTurnUnit != null && CurrentTurnUnit is GamePlayerController)
            {
                CurrentTurnUnit.MyTurn(false);
            }

            if (typeTurn == "Enemy")
            {
                CurrentTurnUnit = null;
                _turnUI.text = "Enemy" + turnNum.ToString();
                _unitPanel.interactable = false;
                _unitPanel.blocksRaycasts = false;
                _unitPanel.alpha = 0.5f;
                // UI만 갱신. 적 전투 로직은 서버의 ServerEnemyTurn()에서 처리.
            }
            else
            {
                if (turnNum < 0 || turnNum >= _players.Count)
                {
                    Debug.LogError($"[RpcChangeTurn] _players 범위 초과: turnNum={turnNum}, _players.Count={_players.Count}. SpawnObserversForConnection이 누락됐을 가능성이 있습니다.");
                    return;
                }

                var targetPlayer = _players[turnNum];
                if (targetPlayer == null)
                {
                    Debug.LogError($"[RpcChangeTurn] _players[{turnNum}]이 null입니다.");
                    return;
                }

                CurrentTurnUnit = targetPlayer;
                _turnUI.text = "Turn: " + targetPlayer.Info.Name;

                targetPlayer.MyTurn(true);

                bool isMyTurn = targetPlayer.isOwned;
                if (isMyTurn)
                {
                    UpdateUnitUI(targetPlayer);
                }
                _unitPanel.interactable = isMyTurn;
                _unitPanel.blocksRaycasts = isMyTurn;
                _unitPanel.alpha = isMyTurn ? 1.0f : 0.5f;
            }
        }
        /// <summary>
        /// 아이템 1개 소모 직후 호출: 해당 플레이어의 아이템 버튼만 재렌더링합니다.
        /// </summary>
        [ClientRpc]
        public void RpcRefreshItemButtons(GamePlayerController unit)
        {
            if (unit == null || !unit.isOwned) return;

            for (int i = 0; i < _itemBTN.Count; i++)
            {
                int index = i;
                _itemBTN[i].onClick.RemoveAllListeners();

                bool hasItem = unit.Info.Expendables != null && i < unit.Info.Expendables.Count;

                if (hasItem)
                {
                    _itemBTN[i].onClick.AddListener(() => unit.OnClickItemBtn(index));

                    var label = _itemBTN[i].GetComponentInChildren<TMPro.TextMeshProUGUI>();
                    if (label != null) label.text = unit.Info.Expendables[i].Name;

                    // 아이콘: 네트워크 전송 시 Sprite=null → CharacterRegistry에서 로컬 조회
                    Sprite icon = unit.Info.Expendables[i].icon;
                    if (icon == null &&
                        CharacterRegistry.TryGet(unit.FinalHeroCode, out var entry) &&
                        entry.Items != null && i < entry.Items.Count)
                    {
                        icon = entry.Items[i].icon;
                    }
                    var iconTf = _itemBTN[i].transform.Find("Icon");
                    var iconImg = iconTf != null ? iconTf.GetComponent<Image>() : null;
                    if (iconImg != null) { iconImg.sprite = icon; iconImg.enabled = icon != null; }
                }
                else
                {
                    // 소진된 슬롯: 버튼 비활성화 + 라벨 초기화
                    _itemBTN[i].interactable = false;
                    var label = _itemBTN[i].GetComponentInChildren<TMPro.TextMeshProUGUI>();
                    if (label != null) label.text = "-";
                    var iconTf = _itemBTN[i].transform.Find("Icon");
                    var iconImg = iconTf != null ? iconTf.GetComponent<Image>() : null;
                    if (iconImg != null) iconImg.enabled = false;
                }
            }
        }

        public void UpdateUnitUI(GamePlayerController unit)
        {
            Debug.Log($"[UpdateUnitUI] 호출됨: name={unit.name}, code={unit.FinalHeroCode}, Skills={unit.Info.Skills?.Count ?? -1}, Items={unit.Info.Items?.Count ?? -1}");

            bool isUnitTurn = unit.isOwned && (CurrentTurnUnit != null && unit.Info.Id == CurrentTurnUnit.Info.Id);

            // ���õ� ������ ���ʿ� �� ���������� ���� �г� Ȱ��/��Ȱ�� ó��
            _unitPanel.interactable = isUnitTurn;
            _unitPanel.blocksRaycasts = isUnitTurn;
            _unitPanel.alpha = isUnitTurn ? 1.0f : 0.5f;
            // 선택된 유닛의 이미지 및 스탯 표시
            // GetCharacterSprite(): CharacterRegistry → SpriteRenderer 순으로 조회하므로 null-safe
            _charaterIMG.sprite = unit.GetComponent<SpriteRenderer>().sprite;
            _name.text = unit.Info.Name;
            _type.text = unit.Info.Type;
            _hp.text = $"{unit.Info.MaxHp} / {unit.Info.Hp}";
            _san.text = $"{unit.Info.MaxSan} / {unit.Info.San}";
            _acc.text = unit.EffectiveAcc.ToString();
            _crit.text = unit.Info.Crit.ToString();
            _dmg.text = unit.EffectiveAtk.ToString();
            _prot.text = unit.EffectiveDef.ToString();
            _res.text = unit.Info.Res.ToString();
            _dodge.text = unit.EffectiveDodge.ToString();

            // 스킬 버튼 이벤트 연결 + 스킬 이름 표시
            for (int i = 0; i < _skillBTN.Count; i++)
            {
                int index = i;
                _skillBTN[i].onClick.RemoveAllListeners();

                bool hasSkill = unit.Info.Skills != null && i < unit.Info.Skills.Count;
                _skillBTN[i].gameObject.SetActive(hasSkill);

                if (hasSkill)
                {
                    _skillBTN[i].onClick.AddListener(() => unit.OnClickSkillBtn(index));

                    // 버튼 자식의 TMP 텍스트에 스킬 이름 표시
                    var label = _skillBTN[i].GetComponentInChildren<TMPro.TextMeshProUGUI>();
                    if (label != null) label.text = unit.Info.Skills[i].Name;

                    // 스킬 아이콘 표시: 네트워크 전송 시 icon=null이므로 CharacterRegistry에서 로컬로 가져옴
                    Sprite icon = unit.Info.Skills[i].icon;
                    if (icon == null &&
                        CharacterRegistry.TryGet(unit.FinalHeroCode, out var entry) &&
                        entry.Skills != null &&
                        i < entry.Skills.Count)
                    {
                        icon = entry.Skills[i].icon;
                    }

                    var iconTransform = _skillBTN[i].transform.Find("Icon");
                    var iconImage = iconTransform != null ? iconTransform.GetComponent<Image>() : null;
                    if (iconImage != null)
                    {
                        iconImage.sprite = icon;
                        iconImage.enabled = icon != null;
                    }
                    Hover hover = _skillBTN[i].GetComponent<Hover>();
                    if (hover != null)
                    {

                        if (unit.Info.Skills != null && index < unit.Info.Skills.Count)
                        {
                            hover.SetInfo(unit.Info.Skills[index].Name, unit.Info.Skills[index].description);
                        }
                        else
                        {
                            hover.SetInfo("", "");
                        }
                    }
                }
            }

            // 아이템 버튼 이벤트 연결 + 아이템 이름 표시
            for (int i = 0; i < _itemBTN.Count; i++)
            {
                int index = i;
                _itemBTN[i].onClick.RemoveAllListeners();

                bool hasItem = unit.Info.Expendables != null && i < unit.Info.Expendables.Count;
                //_itemBTN[i].gameObject.SetActive(hasItem);

                if (hasItem)
                {
                    _itemBTN[i].onClick.AddListener(() => unit.OnClickItemBtn(index));

                    var label = _itemBTN[i].GetComponentInChildren<TMPro.TextMeshProUGUI>();
                    if (label != null) label.text = unit.Info.Expendables[i].Name;
                }
                // 아이템 아이콘 표시: 네트워크 전송 시 icon=null일 수 있으므로 이름으로 로컬 데이터에서 다시 찾음
                Sprite icon = null;

                if (hasItem)
                {
                    var currentItem = unit.Info.Items[i];
                    icon = currentItem != null ? currentItem.icon : null;

                    if (icon == null &&
                        currentItem != null &&
                        !string.IsNullOrEmpty(currentItem.Name) &&
                        CharacterRegistry.TryGet(unit.FinalHeroCode, out var entry) &&
                        entry.Items != null)
                    {
                        foreach (var registryItem in entry.Items)
                        {
                            if (registryItem == null) continue;
                            if (registryItem.Name != currentItem.Name) continue;
                            icon = registryItem.icon;
                            break;
                        }
                    }
                }
                var iconTransform = _itemBTN[i].transform.Find("Icon");
                var iconImage = iconTransform != null ? iconTransform.GetComponent<Image>() : null;
                if (iconImage != null)
                {
                    Debug.Log("아이템 아이콘 넣기");
                    iconImage.sprite = icon;
                    iconImage.enabled = icon != null;
                }
                Hover hover = _itemBTN[i].GetComponent<Hover>();
                if (hover != null)
                {

                    if (unit.Info.Expendables != null && index < unit.Info.Expendables.Count)
                    {
                        hover.SetInfo(unit.Info.Expendables[index].Name, unit.Info.Expendables[index].description);
                    }
                    else
                    {
                        hover.SetInfo("", "");
                    }
                }
            }

            // 장비 아이콘 표시 (무기, 방어구)
            for (int i = 0; i < _equiIMG.Count; i++)
            {
                if (_equiIMG[i] == null) continue;

                bool hasEquip = false;
                if (i == 0)
                    hasEquip = unit.Info.Weapon != null && !string.IsNullOrEmpty(unit.Info.Weapon.Name);
                else if (i == 1)
                    hasEquip = unit.Info.Armor != null && !string.IsNullOrEmpty(unit.Info.Armor.Name);

                Debug.Log($"[장비] i={i} hasEquip={hasEquip} Weapon={unit.Info.Weapon?.Name} Armor={unit.Info.Armor?.Name}");

                Sprite eqpIcon = null;
                if (hasEquip)
                {
                    var currentEquip = i == 0 ? unit.Info.Weapon : unit.Info.Armor;
                    eqpIcon = currentEquip.icon;

                    if (eqpIcon == null &&
                        currentEquip != null &&
                        !string.IsNullOrEmpty(currentEquip.Name) &&
                        CharacterRegistry.TryGet(unit.FinalHeroCode, out var eqpEntry))
                    {
                        if (i == 0 && eqpEntry.Weapon != null && eqpEntry.Weapon.Name == currentEquip.Name)
                            eqpIcon = eqpEntry.Weapon.icon;
                        else if (i == 1 && eqpEntry.Armor != null && eqpEntry.Armor.Name == currentEquip.Name)
                            eqpIcon = eqpEntry.Armor.icon;
                    }
                }

                _equiIMG[i].sprite = eqpIcon;
                _equiIMG[i].enabled = eqpIcon != null;
            }

            // 적 버튼 이벤트 연결 + PlayerView.EnemyBtn 동기화 (프리팹에서 연결 불가한 씬 오브젝트이므로 런타임 설정)
            var enemyButtons = new List<Button>();
            for (int i = 0; i < _enemys[StageNum-1].Enemys.Count; i++)
            {
                int index = i;
                var EnemyBTN = _enemys[StageNum-1].Enemys[i].GetComponent<Button>();
                if (EnemyBTN == null) continue;
                EnemyBTN.onClick.RemoveAllListeners();
                EnemyBTN.onClick.AddListener(() => unit.OnClickEnemyBtn(index));
                enemyButtons.Add(EnemyBTN);
            }
            // 스킬 선택 후 SetButtonsInteractable(true, EnemyBtn)이 올바르게 동작하도록 동기화
            unit.View.EnemyBtn = enemyButtons;

            _movePosBTN.onClick.RemoveAllListeners();
            _movePosBTN.onClick.AddListener(() => unit.OnClickMoveBtn());
        }
        //�� ���� �̹��� ����
        public void UpdateEnemyUI(int index)
        {
            var enemy = _enemys[StageNum - 1].Enemys[index].GetComponent<EnemyController>();
            _enemyPanel.SetActive(true);
            _enemyUI.sprite = enemy.GetComponent<SpriteRenderer>().sprite;
            _enemyName.text = enemy.Info.Name;
            _enemyType.text = enemy.Info.Type;
            _enemyhp.text = $"{enemy.Info.MaxHp} / {enemy.Info.Hp}";
            _enemysan.text = $"{enemy.Info.MaxSan} / {enemy.Info.San}";
            _enemyacc.text = enemy.EffectiveAcc.ToString();
            _enemycrit.text = enemy.Info.Crit.ToString();
            _enemydmg.text = enemy.EffectiveAtk.ToString();
            _enemyprot.text = enemy.EffectiveDef.ToString();
            _enemyres.text = enemy.Info.Res.ToString();
            _enemydodge.text = enemy.EffectiveDodge.ToString();

            for (int i = 0; i < _skillBTN.Count; i++)
            {
                _enemySkillBTN[i].image.sprite = enemy.Info.Skills[i].icon;

                Hover hover = _enemySkillBTN[i].GetComponent<Hover>();
                if (hover != null)
                {

                    if (enemy.Info.Skills != null && index < enemy.Info.Skills.Count)
                    {
                        string name = enemy.Info.Skills[i].Name;
                        string desc = enemy.Info.Skills[i].description;
                        hover.SetInfo(name, desc);
                    }
                    else
                    {
                        hover.SetInfo("", "");
                    }
                }

            }
        }
        //���� ��ġ �̵�
        [Server]
        public void ChangeUnitPos(GamePlayerController unit1, GamePlayerController unit2)
        {
            int tempPos = unit1.FinalHeroPos;
            unit1.FinalHeroPos = unit2.FinalHeroPos;
            unit2.FinalHeroPos = tempPos;

            NextTurn();
        }

        [ClientRpc]
        public void RpcShowPing(int pingIndex, GameObject targetObj)
        {
            Transform pingLayout = null;

            GamePlayerController player = targetObj.GetComponent<GamePlayerController>();
            if (player != null)
            {
                pingLayout = player.PingLayout;
            }
            else
            {
                EnemyController enemy = targetObj.GetComponent<EnemyController>();
                pingLayout = enemy.PingLayout;
            }
            if (_pingList[pingIndex] != null) {
                _pingList[pingIndex].SetActive(false);
            }
            // �ڽ� �� Ȯ��
            Debug.Log($"pingIndex: {pingIndex} / PingLayout �ڽ� ��: {pingLayout.childCount}");

            if (pingIndex >= pingLayout.childCount)
            {
                Debug.LogWarning("PingLayout �ڽ��� ������!");
                return;
            }

            // �ش� �ε��� �ڽ� ������Ʈ �ѱ�
            GameObject ping = pingLayout.GetChild(pingIndex).gameObject;
            ping.SetActive(true);
            _pingList[pingIndex] = ping;

            StartCoroutine(HidePing(ping));
        }

        private IEnumerator HidePing(GameObject ping)
        {
            yield return new WaitForSeconds(2.0f);
            ping.SetActive(false);
        }

        //���Ἲ �˻�
        public void VerifyClientRequest(GamePlayerController caster, int skillIndex, int itemIndex, bool isEnemy,List<int> targets)
        {
            // ��Ģ Ȯ�� ��(���� �������ص�)



            _logic.BattleAction(caster, skillIndex,itemIndex, isEnemy, targets);
        }
        [ClientRpc]
        public void RpcShowCombatResult(CombatResult result)
        {
            if (!result.isHit)
            {
                Debug.Log("[CLIENT] MISS");

                return;
            }
            string label = result.isCrit ? $"CRIT {result.value:F0}!" : $"{result.value:F0}";

            Debug.Log($"[CLIENT] {label} isEnemy:{result.isEnemy} idx:{result.targetIndex}");
        }
        /// <summary>
        /// 서버에서 즉시 EnemyNum을 감소시키고, 0이 되면 바로 NextStage를 호출합니다.
        /// EnemyController.CMDDead()에서 호출됩니다.
        /// </summary>
        [Server]
        public void OnEnemyDead(GameObject enemyObj)
        {
            EnemyNum--;
            Debug.Log($"[BattleManager] 적 사망. 남은 적: {EnemyNum}");
            RcpEnemyDead(enemyObj);

            if (EnemyNum <= 0)
            {
                Debug.Log("[BattleManager] 모든 적 사망 → NextStage");
                NextStage();
            }
        }

        [ClientRpc]
        public void RcpEnemyDead(GameObject go)
        {
            go.SetActive(false);
        }
        public void NextStage()
        {
            Debug.Log("NextStage");
            //  �г� ��Ȱ�� ó��
            _unitPanel.interactable = false;
            _unitPanel.blocksRaycasts = false;
            _unitPanel.alpha = 0.5f;
            StartCoroutine(StageClearDelay()); 
        }
        private IEnumerator StageClearDelay()
        {
            yield return new WaitForSeconds(2.0f);  
            StageClear();
        }
        public void StageClear()
        {
            Debug.Log("StageClear → 결과창 표시 (승리)");
            ShowBattleResult(true);
        }

        // ──────────────────────────────────────────────────────────────────
        //  배틀 결과 처리
        // ──────────────────────────────────────────────────────────────────

        /// <summary>서버에서 결과창을 모든 클라이언트에 띄웁니다.</summary>
        [Server]
        private void ShowBattleResult(bool isVictory)
        {
            _agreeCount      = 0;
            _resultIsVictory = isVictory;
            // 캐릭터 수(_players)가 아닌 실제 접속 클라이언트 수로 동의 기준을 잡습니다.
            int clientCount  = NetworkServer.connections.Count;
            RpcShowBattleResult(isVictory, clientCount);
        }

        [ClientRpc]
        private void RpcShowBattleResult(bool isVictory, int totalPlayers)
        {
            if (_battleResultPanel != null)
                _battleResultPanel.Show(isVictory, totalPlayers);
        }

        /// <summary>
        /// 클라이언트가 동의 버튼을 눌렀을 때 서버로 전달.
        /// requiresAuthority=false 이므로 어느 클라이언트든 호출 가능합니다.
        /// </summary>
        [Command(requiresAuthority = false)]
        public void CmdAgreeResult()
        {
            int clientCount = NetworkServer.connections.Count;
            _agreeCount++;
            Debug.Log($"[BattleResult] 동의 {_agreeCount} / {clientCount}");

            // 모든 클라이언트에 카운트 갱신
            RpcUpdateAgreeCount(_agreeCount, clientCount);

            if (_agreeCount >= clientCount)
            {
                // 전원 동의 완료 → 도장 연출 → 2초 후 이동
                RpcShowStamp();
                StartCoroutine(ProceedAfterStamp());
            }
        }

        [Server]
        private IEnumerator ProceedAfterStamp()
        {
            yield return new WaitForSeconds(2.5f);   // 도장 애니메이션(약 0.5s) + 감상 시간(2s)
            RpcHideBattleResult();

            if (_resultIsVictory)
                _rootingSystem.ServerEndStage();
            else
                NetworkManager.singleton.ServerChangeScene("Home");
        }

        [ClientRpc]
        private void RpcUpdateAgreeCount(int current, int total)
        {
            if (_battleResultPanel != null)
                _battleResultPanel.UpdateAgreeCount(current, total);
        }

        [ClientRpc]
        private void RpcShowStamp()
        {
            if (_battleResultPanel != null)
                _battleResultPanel.PlayStampAnimation();
        }

        [ClientRpc]
        private void RpcHideBattleResult()
        {
            if (_battleResultPanel != null)
                _battleResultPanel.Hide();
        }
    }
}
