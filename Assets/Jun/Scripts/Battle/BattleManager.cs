using Mirror;
using Mirror.BouncyCastle.Security;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting.Dependencies.NCalc;
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
        [SerializeField] private List<GameObject> BattleUnitPrefabs;
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
            Debug.Log($"[SetupBattleFlow] 시작. HeroNum={roomManager?.HeroNum}");

            PlayerData[] survivors;
            int waitFrames = 0;
            while (true)
            {
                survivors = FindObjectsByType<PlayerData>(FindObjectsSortMode.None);
                if (waitFrames % 60 == 0)
                    Debug.Log($"[SetupBattleFlow] 대기 중... survivors={survivors.Length}, HeroNum={roomManager.HeroNum}");
                waitFrames++;
                if (survivors.Length == roomManager.HeroNum) break;
                yield return null;
            }

            Debug.Log($"[SetupBattleFlow] 전원 도착! survivors={survivors.Length}, HeroNum={roomManager.HeroNum}");

            foreach (var data in survivors)
            {
                Debug.Log($"[SetupBattleFlow] PlayerData 처리: FinalHeroIndex={data.FinalHeroIndex}, FinalHeroPos={data.FinalHeroPos}, conn={data.connectionToClient}");
                // 1. ������(FinalHeroIndex)�� �´� ������ ������ ����! (��ġ�� ���� ����Ʈ��)
                GameObject battleObj = Instantiate(BattleUnitPrefabs[data.FinalHeroIndex], SpawnPoints[data.FinalHeroPos].position, Quaternion.identity);

                // 2. ������ ���� ��Ʈ�ѷ��� ������ ������(��ȥ) ����!
                var controller = battleObj.GetComponent<GamePlayerController>();
                controller.InjectData(data);

                // 3. [���� �߿�] ������ �����ϸ鼭, �ش� Ŭ���̾�Ʈ���� ���� ���� �ֱ�!
                // PlayerData�� ������ �� ���� ������ ����(connectionToClient)�� �� ���뿡 �������ݴϴ�.
                NetworkServer.Spawn(battleObj, data.connectionToClient);

                // 4. ���� ����Ʈ�� ��Ʈ�ѷ� ���
                _players.Add(controller);
                _turnList.Add(new TurnData("Player", data.Info.Spd, _players.Count - 1));
            }

            // 5. �� ����
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

            // �̹� ���� Ÿ�԰� ��ȣ�� ������ Ȯ��
            string currentType = _turnList[Order].type;
            int currentNum = _turnList[Order].num;

            Debug.Log("���� ���� ��: " + EnemyNum);
            if (EnemyNum == 0)
            {
                Debug.Log("EndStage");
                NextStage();
                return;
            }

            RpcChangeTurn(currentType, currentNum);
            RpcSetHighlight(Order, true);
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
                StartCoroutine(EnemyTurn());
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
        // �� ���ϋ� ��� �ڷ�ƾ���� �ѱ��
        IEnumerator EnemyTurn()
        {
            yield return new WaitForSeconds(1.0f);
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
            _acc.text = unit.Info.Acc.ToString();
            _crit.text = unit.Info.Crit.ToString();
            _dmg.text = unit.Info.Atk.ToString();
            _prot.text = unit.Info.Def.ToString();
            _res.text = unit.Info.Hp.ToString();
            _dodge.text = unit.Info.Dodge.ToString();

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
        public void RcpEnemyDead(GameObject go)
        {
            Debug.Log("Enemy Dead");
            EnemyNum -= 1;
            go.SetActive(false);
        }
        public void NextStage()
        {
            Debug.Log("NextStage");
            //  �г� ��Ȱ�� ó��
            _unitPanel.interactable = false;
            _unitPanel.blocksRaycasts = false;
            _unitPanel.alpha = 0.5f;
            StageClear();
        }
        public void StageClear()
        {
            Debug.Log("StageClaer");
            _rootingSystem.ServerEndStage();
        }
    }
}
