using Mirror;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ��Ʋ�� ���������� ��ϴ� ����
namespace Jun
{
    public class BattleManager : NetworkBehaviour
    {
        [SerializeField] private BattleLogic _logic;
        public static BattleManager Instance;

        [Header("���� ���� ����Ʈ")]
        public readonly SyncList<GamePlayerController> _players = new SyncList<GamePlayerController>();
        [Header("���� ���� ��ġ")]
        [SerializeField] private List<Transform> _spawnPoints; public List<Transform> SpawnPoints => _spawnPoints;
        [Header("ĳ���͵��� �̹���")]
        [SerializeField] private List<Sprite> _playerImages; public List<Sprite> PlayerImages => _playerImages;

        [Header("���� ���� ������")]
        [SerializeField] private List<GameObject> _battleUnitPrefabs; public List<GameObject> BattleUnitPrefabs => _battleUnitPrefabs;
        [Header("ĳ���� ���� â")]
        [SerializeField] private CanvasGroup _unitPanel;
        [SerializeField] private Image _charaterIMG; public Image CharaterIMG => _charaterIMG;
        [Header("ĳ���� ���� UI��")]
        [SerializeField] private List<Button> _skillBTN; public List<Button> SkillBTN => _skillBTN;
        [SerializeField] private Button _movePosBTN; public Button MovePosBTN => _movePosBTN;
        [SerializeField] private List<Image> _equiIMG; public List<Image> EquiIMG => _equiIMG;
        [SerializeField] private List<Button> _items; public List<Button> Items => _items;
        [SerializeField] private TextMeshProUGUI _hp; public TextMeshProUGUI Hp => _hp;
        [SerializeField] private TextMeshProUGUI _san; public TextMeshProUGUI San => _san;
        [SerializeField] private TextMeshProUGUI _acc; public TextMeshProUGUI Acc => _acc;
        [SerializeField] private TextMeshProUGUI _crit; public TextMeshProUGUI Crit => _crit;
        [SerializeField] private TextMeshProUGUI _dmg; public TextMeshProUGUI Dmg => _dmg;
        [SerializeField] private TextMeshProUGUI _prot; public TextMeshProUGUI Prot => _prot;
        [SerializeField] private TextMeshProUGUI _res; public TextMeshProUGUI Res => _res;
        [SerializeField] private TextMeshProUGUI _dodge; public TextMeshProUGUI Dodge => _dodge;
        [SerializeField] private TextMeshProUGUI _name; public TextMeshProUGUI Name => _name;

        [Header("��")]// ������ ���� ��� ��ư���� ����, �Ŀ� GameObject�� �ٲ� ����
        [SerializeField] private int _stageNum = 1; public int StageNum => _stageNum; 
        [SerializeField] private List<BattleEnemyInfo> _enemys; public List<BattleEnemyInfo> Enemys => _enemys;
        [SerializeField] private GameObject _enemyPanel; public GameObject EnemyPanel => _enemyPanel;
        [SerializeField] private Image _enemyUI;
        [SerializeField] private TextMeshProUGUI _enemyName;

        public int EnemyNum;

        [Header("�� ����")]
        [SerializeField] private Transform _turnPanel;  //�� �����ִ� ���
        [SerializeField] private Image _turnUi;  // ���� �������� �̹���
        [SerializeField] private List<Image> _turnUIList; 
        [SerializeField] private TextMeshProUGUI _turnUI;  
        public List<TurnData> _turnList = new List<TurnData>();
        public GamePlayerController CurrentTurnUnit { get; private set; }
        private Coroutine _enemyTurnRoutine;

        [SerializeField] private RootingSystem _rootingSystem;

        [System.Serializable]
        public class PingSystem
        {
            public GameObject MyPing;
            public GameObject IsPing = null;
           
        }

        [Header("�� �ý���")]
        [SerializeField] private List<GameObject> _pingList = new List<GameObject>();



        public int Order = -1;


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

            PlayerData[] survivors;
            float deadline = Time.time + 10f;
            while (true)
            {
                survivors = FindObjectsByType<PlayerData>(FindObjectsSortMode.None);
                int expectedHeroCount = roomManager != null ? roomManager.HeroNum : 0;

                if (expectedHeroCount > 0 && survivors.Length >= expectedHeroCount) break;
                if (expectedHeroCount <= 0 && survivors.Length > 0) break;
                if (Time.time >= deadline)
                {
                    Debug.LogWarning($"[BattleManager] PlayerData wait timeout. found={survivors.Length}, expected={expectedHeroCount}");
                    break;
                }
                yield return null;
            }

            Debug.Log("���� ����! ���� ������ �����մϴ�.");

            if (survivors.Length == 0)
            {
                Debug.LogError("[BattleManager] PlayerData가 없어 전투를 시작할 수 없습니다.");
                yield break;
            }

            foreach (var data in survivors)
            {
                if (data.FinalHeroIndex < 0 || data.FinalHeroIndex >= BattleUnitPrefabs.Count)
                {
                    Debug.LogWarning($"[BattleManager] Invalid hero index: {data.FinalHeroIndex}");
                    continue;
                }

                if (data.FinalHeroPos < 0 || data.FinalHeroPos >= SpawnPoints.Count)
                {
                    Debug.LogWarning($"[BattleManager] Invalid hero position: {data.FinalHeroPos}");
                    continue;
                }

                if (BattleUnitPrefabs[data.FinalHeroIndex] == null)
                {
                    Debug.LogWarning($"[BattleManager] BattleUnitPrefab is null. index={data.FinalHeroIndex}");
                    continue;
                }
                // 1. ������(FinalHeroIndex)�� �´� ������ ������ ����! (��ġ�� ���� ����Ʈ��)
                GameObject battleObj = Instantiate(BattleUnitPrefabs[data.FinalHeroIndex], SpawnPoints[data.FinalHeroPos].position, Quaternion.identity);

                // 2. ������ ���� ��Ʈ�ѷ��� ������ ������(��ȥ) ����!
                var controller = battleObj.GetComponent<GamePlayerController>();
                if (controller == null)
                {
                    Debug.LogWarning($"[BattleManager] GamePlayerController missing on prefab index={data.FinalHeroIndex}");
                    Destroy(battleObj);
                    continue;
                }

                controller.InjectData(data);

                // 3. [���� �߿�] ������ �����ϸ鼭, �ش� Ŭ���̾�Ʈ���� ���� ���� �ֱ�!
                // PlayerData�� ������ �� ���� ������ ����(connectionToClient)�� �� ���뿡 �������ݴϴ�.
                NetworkServer.Spawn(battleObj, data.connectionToClient);

                // 4. ���� ����Ʈ�� ��Ʈ�ѷ� ���
                _players.Add(controller);
                _turnList.Add(new TurnData("Player", data.Info.Spd, _players.Count - 1));
            }

            // 5. �� ����
            if (_players.Count == 0)
            {
                Debug.LogError("[BattleManager] 생성된 플레이어 유닛이 없어 전투를 시작할 수 없습니다.");
                yield break;
            }

            Invoke(nameof(StartFirstTurn), 1.0f);
        }

        [Server]
        private void StartFirstTurn()
        {
            Order = -1;
            _turnList.Sort((a, b) => b.speed.CompareTo(a.speed));
            RpcTurnListUpdate(_turnList.ToArray());
            RpcResetAllHighlights();   // prefab 기본값이 active 인 경우 대비
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
                    sp = _enemys[StageNum - 1].Enemys[targetNum].image.sprite;
                }
                else
                {
                    // �÷��̾�� ������ �� �ð��� �ɸ� �� ������ ��� �ڵ� ���
                    if (targetNum < _players.Count)
                    {
                        sp = _players[targetNum].GetComponent<SpriteRenderer>().sprite;
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
            if (index < 0 || _turnUIList == null || index >= _turnUIList.Count) return;

            var img = _turnUIList[index];
            if (img == null) return;

            var t = img.transform.Find("HighLight");
            if (t == null)
            {
                Debug.LogWarning($"[BattleManager] _turnUi 프리팹에 'HighLight' 자식이 없습니다. index={index}");
                return;
            }

            Debug.Log($"[BattleManager] Highlight - index:{index}, on:{isTurn}");
            t.gameObject.SetActive(isTurn);
        }

        // 라운드 시작 / 새 턴 리스트 빌드 시 모든 highlight 를 일괄 OFF (prefab 기본값이 active 인 경우 대비)
        [ClientRpc]
        private void RpcResetAllHighlights()
        {
            if (_turnUIList == null) return;
            foreach (var img in _turnUIList)
            {
                if (img == null) continue;
                var t = img.transform.Find("HighLight");
                if (t != null) t.gameObject.SetActive(false);
            }
        }
        // ���� ������ ����
        [Server]
        public void NextTurn()
        {
            if (_turnList == null || _turnList.Count == 0)
            {
                Debug.LogWarning("[BattleManager] NextTurn called with empty turn list.");
                return;
            }

            // 패배 / 승리 종료 체크
            int alivePlayers = 0;
            foreach (var p in _players)
                if (p != null && p.Info != null && p.Info.Hp > 0f) alivePlayers++;

            if (alivePlayers == 0)
            {
                Debug.Log("[BattleManager] 전멸 — defeat");
                var rmFail = RoundManager.Instance;
                if (rmFail != null) rmFail.OnAllPlayersDead();
                else NetworkManager.singleton.ServerChangeScene("Home 1");
                return;
            }

            if (EnemyNum <= 0)
            {
                Debug.Log("[BattleManager] 적 전멸 — round clear");
                NextStage();
                return;
            }

            RpcSetHighlight(Order, false);

            // 살아있는 다음 유닛 찾기 (죽은 유닛은 스킵)
            int safety = 0;
            do
            {
                Order = (Order + 1) % _turnList.Count;
                if (Order == 0) _turnList.Sort((a, b) => b.speed.CompareTo(a.speed));
                if (++safety > _turnList.Count + 1)
                {
                    Debug.LogWarning("[BattleManager] No alive units in turn list.");
                    return;
                }
            } while (IsTurnEntryDead(_turnList[Order]));

            string currentType = _turnList[Order].type;
            int currentNum = _turnList[Order].num;

            Debug.Log($"[BattleManager] Turn: {currentType}#{currentNum} (적:{EnemyNum} 생존:{alivePlayers})");

            RpcChangeTurn(currentType, currentNum);
            RpcSetHighlight(Order, true);

            if (currentType == "Enemy")
            {
                if (_enemyTurnRoutine != null) StopCoroutine(_enemyTurnRoutine);
                _enemyTurnRoutine = StartCoroutine(EnemyTurnServer());
            }
        }

        // 턴 리스트 항목이 사망 상태인지 (스킵 판정)
        private bool IsTurnEntryDead(TurnData t)
        {
            if (t == null) return true;
            if (t.type == "Enemy")
            {
                if (Enemys == null) return true;
                int sIdx = StageNum - 1;
                if (sIdx < 0 || sIdx >= Enemys.Count) return true;
                if (t.num < 0 || t.num >= Enemys[sIdx].Enemys.Count) return true;
                var btn = Enemys[sIdx].Enemys[t.num];
                if (btn == null) return true;
                var ec = btn.GetComponent<EnemyController>();
                return ec == null || ec.Info == null || ec.Info.Hp <= 0f;
            }
            if (t.num < 0 || t.num >= _players.Count) return true;
            var pl = _players[t.num];
            return pl == null || pl.Info == null || pl.Info.Hp <= 0f;
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
                CurrentTurnUnit = null; // �� ���̴ϱ� 
                _turnUI.text = "Enemy" + turnNum.ToString();
                //  �г� ��Ȱ�� ó��
                _unitPanel.interactable = false;
                _unitPanel.blocksRaycasts = false;
                _unitPanel.alpha = 0.5f;
            }
            else
            {
                var targetPlayer = _players[turnNum];
                CurrentTurnUnit = targetPlayer; //���� �� ���� ���� ����
                _turnUI.text = "Turn: " + _players[turnNum].Info.Id.ToString();
                
                //���� ���̵� ������� ������ ��� ���� ǥ��
                targetPlayer.MyTurn(true);

                bool isMyTurn = targetPlayer.isOwned;
                if (isMyTurn)
                {
                    UpdateUnitUI(targetPlayer);
                }
                // ���������� ���� �г� Ȱ��/��Ȱ�� ó��
                _unitPanel.interactable = isMyTurn;
                _unitPanel.blocksRaycasts = isMyTurn;
                _unitPanel.alpha = isMyTurn ? 1.0f : 0.5f;
            }
        }
        // 적 차례 — EnemyAI로 스킬/타깃 결정 후 BattleLogic.EnemyAction 실행
        [Server]
        private IEnumerator EnemyTurnServer()
        {
            yield return new WaitForSeconds(0.7f);

            if (_turnList == null || Order < 0 || Order >= _turnList.Count)
            {
                _enemyTurnRoutine = null;
                NextTurn();
                yield break;
            }

            int eIdx = _turnList[Order].num;
            int sIdx = StageNum - 1;

            EnemyController enemy = null;
            UnityEngine.UI.Button btn = null;
            if (Enemys != null && sIdx >= 0 && sIdx < Enemys.Count
                && eIdx >= 0 && eIdx < Enemys[sIdx].Enemys.Count)
            {
                btn = Enemys[sIdx].Enemys[eIdx];
                if (btn != null) enemy = btn.GetComponent<EnemyController>();
            }

            if (enemy == null || enemy.Info == null || enemy.Info.Hp <= 0f)
            {
                _enemyTurnRoutine = null;
                NextTurn();
                yield break;
            }

            // EnemyAI 가 있으면 가중치 기반 스킬 픽, 없으면 기본 공격
            SkillInfo skill;
            List<int> targets;
            var ai = btn.GetComponent<EnemyAI>();
            if (ai != null)
            {
                skill = ai.PickSkill();
                targets = ai.PickTargets(skill, _players);
            }
            else
            {
                skill = new SkillInfo
                {
                    Name = "Bash",
                    Type = SkillType.Atk,
                    DamageMultiplier = 1f,
                    Target = TargetType.SingleEnemy,
                    TagetNum = 1,
                    StatusEffects = new List<StatusApply>()
                };
                targets = new List<int>();
                for (int i = 0; i < _players.Count; i++)
                {
                    if (_players[i] != null && _players[i].Info != null && _players[i].Info.Hp > 0f)
                    {
                        targets.Add(i);
                        break;
                    }
                }
            }

            if (_logic != null && targets != null && targets.Count > 0)
            {
                _logic.EnemyAction(enemy, skill, targets);
            }

            yield return new WaitForSeconds(0.5f);
            _enemyTurnRoutine = null;
            NextTurn();
        }
        public void UpdateUnitUI(GamePlayerController unit)
        {
            bool isUnitTurn = unit.isOwned && (CurrentTurnUnit != null && unit.Info.Id == CurrentTurnUnit.Info.Id);

            // ���õ� ������ ���ʿ� �� ���������� ���� �г� Ȱ��/��Ȱ�� ó��
            _unitPanel.interactable = isUnitTurn;
            _unitPanel.blocksRaycasts = isUnitTurn;
            _unitPanel.alpha = isUnitTurn ? 1.0f : 0.5f;
            // ���õ� ������ ������ ��ü
            _charaterIMG.sprite = unit.GetComponent<SpriteRenderer>().sprite;
            _hp.text = unit.Info.Hp.ToString();
            _san.text = unit.Info.San.ToString();
            _acc.text = unit.EffectiveAcc.ToString();
            _crit.text = unit.Info.Crit.ToString();
            _dmg.text = unit.EffectiveAtk.ToString();
            _prot.text = unit.EffectiveDef.ToString();
            _res.text = unit.Info.Res.ToString();
            _dodge.text = unit.EffectiveDodge.ToString();

            // ��ų ��ư �̺�Ʈ �翬��
            for (int i = 0; i < _skillBTN.Count; i++)
            {
                int index = i;
                _skillBTN[i].onClick.RemoveAllListeners();
                _skillBTN[i].onClick.AddListener(() => unit.OnClickSkillBtn(index));

                // ��ų �����ܵ� ���ֿ� �°� ���� ����
                // _skillBTN[i].image.sprite = unit.SkillSprites[i];

            }
            for (int i = 0; i < _enemys[StageNum-1].Enemys.Count; i++)
            {
                int index = i;
                var EnemyBTN = _enemys[StageNum-1].Enemys[i].GetComponent<Button>();
                EnemyBTN.onClick.RemoveAllListeners();
                EnemyBTN.onClick.AddListener(() => unit.OnClickEnemyBtn(index));
            }
            _movePosBTN.onClick.RemoveAllListeners();
            _movePosBTN.onClick.AddListener(() => unit.OnClickMoveBtn());
        }
        //�� ���� �̹��� ����
        public void UpdateEnemyUI(int index)
        {
            var enemy = _enemys[StageNum-1].Enemys[index].GetComponent<EnemyController>();
            _enemyPanel.SetActive(true);
            _enemyUI.sprite = enemy.GetComponent<Image>().sprite;
        }
        //���� ��ġ �̵�
        [Server]
        public void ChangeUnitPos(GamePlayerController unit1, GamePlayerController unit2)
        {
            int tempPos = unit1.FinalHeroPos;
            unit1.FinalHeroPos = unit2.FinalHeroPos;
            unit2.FinalHeroPos = tempPos;

            // ���� �� ������ tick ó�� �� NextTurn
            var current = _players[_turnList[Order].num];
            var ticked = CombatCalculator.TickEffects(current.Effects);
            current.Effects.Clear();
            foreach (var e in ticked) current.Effects.Add(e);

            NextTurn();
        }

        [ClientRpc]
        public void RpcShowPing(int pingIndex, GameObject targetObj)
        {
            if (pingIndex < 0 || targetObj == null) return;

            Transform pingLayout = null;

            GamePlayerController player = targetObj.GetComponent<GamePlayerController>();
            if (player != null)
            {
                pingLayout = player.PingLayout;
            }
            else
            {
                EnemyController enemy = targetObj.GetComponent<EnemyController>();
                if (enemy != null) pingLayout = enemy.PingLayout;
            }
            if (pingLayout == null) return;

            while (_pingList.Count <= pingIndex)
                _pingList.Add(null);

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



            if (_logic == null || caster == null || targets == null) return;
            _logic.BattleAction(caster, skillIndex,itemIndex, isEnemy, targets);
        }
        [ClientRpc]
        public void RpcShowCombatResult(CombatResult result)
        {
            if (!result.isHit)
            {
                Debug.Log("[CLIENT] MISS");
                // TODO: MISS �÷��� �ؽ�Ʈ
                return;
            }
            string label = result.isCrit ? $"CRIT {result.value:F0}!" : $"{result.value:F0}";
            // TODO: �÷��� ������/�� �ؽ�Ʈ ����
            Debug.Log($"[CLIENT] {label} isEnemy:{result.isEnemy} idx:{result.targetIndex}");
        }
        [Server]
        public void OnEnemyDead(GameObject enemyObj)
        {
            EnemyNum -= 1;
            RpcEnemyDead(enemyObj);
        }

        [ClientRpc]
        public void RpcEnemyDead(GameObject go)
        {
            Debug.Log("Enemy Dead");
            go.SetActive(false);
        }

        // ── 던전/라운드 시스템 ─────────────────────────────────
        // RoundManager 가 SyncVar hook 으로 호출. 새 라운드의 적을 활성화하고 턴 리스트 재구성.
        [Server]
        public void ServerSetupRound(int round)
        {
            if (round < 1 || _enemys == null || round - 1 >= _enemys.Count)
            {
                Debug.LogError($"[BattleManager] ServerSetupRound: invalid round {round} (stages={_enemys?.Count ?? 0})");
                return;
            }

            Debug.Log($"[BattleManager] ServerSetupRound({round}) 시작");

            if (_enemyTurnRoutine != null)
            {
                StopCoroutine(_enemyTurnRoutine);
                _enemyTurnRoutine = null;
            }

            _stageNum = round;

            // 모든 스테이지의 적 비활성 → 새 라운드만 활성화
            for (int s = 0; s < _enemys.Count; s++)
            {
                if (_enemys[s].Enemys == null) continue;
                for (int i = 0; i < _enemys[s].Enemys.Count; i++)
                {
                    bool active = (s == round - 1);
                    RpcSetEnemyActive(s, i, active);
                }
            }

            // 새 라운드 적 HP 리셋 + HP바 동기화
            var roster = _enemys[round - 1].Enemys;
            for (int i = 0; i < roster.Count; i++)
            {
                if (roster[i] == null) continue;
                var ec = roster[i].GetComponent<EnemyController>();
                if (ec == null || ec.Info == null) continue;

                if (ec.Info.MaxHp <= 0f) ec.Info.MaxHp = ec.Info.Hp;
                ec.Info.Hp = ec.Info.MaxHp;
                if (ec.Info.Statuses != null) ec.Info.Statuses.Clear();

                RpcSyncEnemyHp(i, ec.Info.Hp, ec.Info.MaxHp);
            }
            EnemyNum = roster.Count;

            // 턴 리스트 재구성 (살아있는 플레이어 + 새 적)
            _turnList.Clear();
            for (int i = 0; i < _players.Count; i++)
            {
                var p = _players[i];
                if (p != null && p.Info != null && p.Info.Hp > 0f)
                    _turnList.Add(new TurnData("Player", p.Info.Spd, i));
            }
            for (int i = 0; i < roster.Count; i++)
            {
                if (roster[i] == null) continue;
                var ec = roster[i].GetComponent<EnemyController>();
                if (ec != null && ec.Info != null)
                    _turnList.Add(new TurnData("Enemy", ec.Info.Spd, i));
            }

            Order = -1;
            _turnList.Sort((a, b) => b.speed.CompareTo(a.speed));
            RpcClearTurnUI();
            RpcTurnListUpdate(_turnList.ToArray());
            RpcResetAllHighlights();

            Invoke(nameof(NextTurn), 1.0f);
        }

        [ClientRpc]
        private void RpcSetEnemyActive(int stageIdx, int enemyIdx, bool active)
        {
            if (_enemys == null) return;
            if (stageIdx < 0 || stageIdx >= _enemys.Count) return;
            if (_enemys[stageIdx].Enemys == null) return;
            if (enemyIdx < 0 || enemyIdx >= _enemys[stageIdx].Enemys.Count) return;
            var btn = _enemys[stageIdx].Enemys[enemyIdx];
            if (btn != null) btn.gameObject.SetActive(active);
        }

        [ClientRpc]
        private void RpcClearTurnUI()
        {
            if (_turnUIList == null) return;
            foreach (var img in _turnUIList)
                if (img != null) Destroy(img.gameObject);
            _turnUIList.Clear();
        }

        // EndMyTurn 등에서 호출. 현재는 NextTurn 직결 (큐 시스템 도입 시 확장).
        [Server]
        public void QueueNextTurn()
        {
            NextTurn();
        }

        // 적 HP 변동을 모든 클라이언트에 동기화 (EnemyModel.Damaged 가 서버에서 호출)
        [ClientRpc]
        public void RpcSyncEnemyHp(int enemyIdx, float hp, float maxHp)
        {
            if (enemyIdx < 0
                || _enemys == null
                || StageNum - 1 < 0
                || StageNum - 1 >= _enemys.Count
                || enemyIdx >= _enemys[StageNum - 1].Enemys.Count) return;

            var view = _enemys[StageNum - 1].Enemys[enemyIdx].GetComponent<EnemyView>();
            if (view != null && maxHp > 0f) view.Damaged(hp / maxHp);
        }
        [Server]
        public void NextStage()
        {
            Debug.Log("NextStage");
            _unitPanel.interactable = false;
            _unitPanel.blocksRaycasts = false;
            _unitPanel.alpha = 0.5f;

            var rm = RoundManager.Instance;
            if (rm != null && rm.State == DungeonState.InBattle && rm.CurrentRound < rm.TotalRounds)
            {
                // 다음 라운드 — Rooting 없이 바로 진행 (hook 이 ServerSetupRound 호출)
                Debug.Log($"[BattleManager] Round {rm.CurrentRound}/{rm.TotalRounds} clear → 다음 라운드");
                rm.OnRoundCleared();
                return;
            }

            // 마지막 라운드 또는 RoundManager 미사용 — 기존 Rooting 흐름
            StageClear();
        }
        public void StageClear()
        {
            Debug.Log("StageClear");
            _rootingSystem.ServerEndStage();
        }
    }
}
