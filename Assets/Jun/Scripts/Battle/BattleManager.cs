using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
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
        [Header("ĳ���� ���� â")]
        [SerializeField] private CanvasGroup _unitPanel;
        [SerializeField] private Image _charaterIMG; public Image CharaterIMG => _charaterIMG;
        [Header("ĳ���� ���� UI��")]
        [SerializeField] private List<Button> _skillBTN; public List<Button> SkillBTN => _skillBTN;
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
        [SerializeField] private Transform _turnPanel;
        [SerializeField] private TextMeshProUGUI _turnUI;
        [SerializeField] private GameObject _turnUi;
        public List<TurnData> _turnList = new List<TurnData>();
        public GamePlayerController CurrentTurnUnit { get; private set; }

        [SerializeField] private RootingSystem _rootingSystem;
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
        }
        //���� �� �÷��̾� �߰�(�����ϱ� ����)
        public void RegisterPlayer(GamePlayerController pl)
        {
            var manager = NetworkManager.singleton as GameRoomManager;
            // ���� �÷��̾� �߰�
            _players.Add(pl);
            _turnList.Add(new TurnData("Player", pl.Info.Spd, _players.Count-1));
            
            pl.gameObject.SetActive(true);
            if (isServer && _players.Count == manager.HeroNum)
            {
                // �̹� ���� ���� Invoke�� �ִٸ� ����ؼ� �ߺ� ����
                CancelInvoke(nameof(StartFirstTurn));
                Invoke(nameof(StartFirstTurn), 0.5f);
            }
        }
        [Server]
        private void StartFirstTurn()
        {
            Order = -1; // Ȯ���ϰ� �ʱ�ȭ
            _turnList.Sort((a, b) => b.speed.CompareTo(a.speed));
            NextTurn();
        }
        // ���� ������ ����
        [Server]
        public void NextTurn()
        {
            Order = (Order + 1) % _turnList.Count;
            Debug.Log(Order);
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
        }
        // �� ����
        [ClientRpc]
        public void RpcChangeTurn(string typeTurn, int turnNum)
        {
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

                bool isMyTurn = targetPlayer.isOwned && (CurrentTurnUnit != null && targetPlayer.Info.Id == CurrentTurnUnit.Info.Id);
                if (isMyTurn)
                {
                    UpdateUnitUI(targetPlayer);
                }
                targetPlayer.MyTurn(isMyTurn);
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
        }
        //�� ���� �̹��� ����
        public void UpdateEnemyUI(int index)
        {
            var enemy = _enemys[StageNum-1].Enemys[index].GetComponent<EnemyController>();
            _enemyPanel.SetActive(true);
            _enemyUI.sprite = enemy.GetComponent<Image>().sprite;
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
