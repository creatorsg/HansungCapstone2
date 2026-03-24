using Mirror;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting.Dependencies.NCalc;
using UnityEngine;
using UnityEngine.UI;

// 배틀을 전반적으로 운영하는 서버
namespace Jun
{
    public class BattleManager : NetworkBehaviour
    {
        [SerializeField] private BattleLogic _logic;
        public static BattleManager Instance;

        [Header("전투 유닛 리스트")]
        public readonly SyncList<GamePlayerController> _players = new SyncList<GamePlayerController>();
        [Header("영웅 생성 위치")]
        [SerializeField] private List<Transform> _spawnPoints; public List<Transform> SpawnPoints => _spawnPoints;
        [Header("캐릭터들의 이미지")]
        [SerializeField] private List<Sprite> _playerImages; public List<Sprite> PlayerImages => _playerImages;
        [Header("캐릭터 정보 창")]
        [SerializeField] private CanvasGroup _unitPanel;
        [SerializeField] private Image _charaterIMG; public Image CharaterIMG => _charaterIMG;
        [Header("캐릭터 관련 UI들")]
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

        [Header("적")]// 실험을 위해 잠시 버튼으로 만듬, 후에 GameObject로 바꿀 예정
        [SerializeField] private int _stageNum = 1; public int StageNum => _stageNum; 
        [SerializeField] private List<BattleEnemyInfo> _enemys; public List<BattleEnemyInfo> Enemys => _enemys;
        [SerializeField] private GameObject _enemyPanel; public GameObject EnemyPanel => _enemyPanel;
        [SerializeField] private Image _enemyUI;
        [SerializeField] private TextMeshProUGUI _enemyName;

        public int EnemyNum;

        [Header("턴 정보")]
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
            //  패널 비활성 처리
            _unitPanel.interactable = false;
            _unitPanel.blocksRaycasts = false;
            _unitPanel.alpha =  0.5f;
            EnemyNum = _enemys[StageNum - 1].Enemys.Count;
        }
        public override void OnStartServer()
        {
            base.OnStartServer();

            // 기존에 쌓인 쓰레기 데이터 초기화 
            _players.Clear();
            _turnList.Clear();
            Order = -1;

            // 서버에서만 적 턴 데이터 추가.
            for (int i = 0; i < _enemys[StageNum - 1].Enemys.Count; i++)
            {
                _turnList.Add(new TurnData("Enemy", _enemys[StageNum - 1].Enemys[i].GetComponent<EnemyController>().Info.Spd, i));
            }
        }
        //접속 후 플레이어 추가(관리하기 위해)
        public void RegisterPlayer(GamePlayerController pl)
        {
            var manager = NetworkManager.singleton as GameRoomManager;
            // 현재 플레이어 추가
            _players.Add(pl);
            _turnList.Add(new TurnData("Player", pl.Info.Spd, _players.Count-1));
            
            pl.gameObject.SetActive(true);
            if (isServer && _players.Count == manager.HeroNum)
            {
                // 이미 실행 중인 Invoke가 있다면 취소해서 중복 방지
                CancelInvoke(nameof(StartFirstTurn));
                Invoke(nameof(StartFirstTurn), 0.5f);
            }
        }
        [Server]
        private void StartFirstTurn()
        {
            Order = -1; // 확실하게 초기화
            _turnList.Sort((a, b) => b.speed.CompareTo(a.speed));
            NextTurn();
        }
        // 다음 턴으로 진행
        [Server]
        public void NextTurn()
        {
            Order = (Order + 1) % _turnList.Count;
            Debug.Log(Order);
            // 이번 턴의 타입과 번호를 서버가 확인
            string currentType = _turnList[Order].type;
            int currentNum = _turnList[Order].num;
            Debug.Log("현재 적의 수: " + EnemyNum);
            if (EnemyNum == 0)
            {
                Debug.Log("EndStage");
                NextStage();
                return;
            }

            RpcChangeTurn(currentType, currentNum);
        }
        // 턴 변경
        [ClientRpc]
        public void RpcChangeTurn(string typeTurn, int turnNum)
        {
            if (typeTurn == "Enemy")
            {
                CurrentTurnUnit = null; // 적 턴이니까 
                _turnUI.text = "Enemy" + turnNum.ToString();
                //  패널 비활성 처리
                _unitPanel.interactable = false;
                _unitPanel.blocksRaycasts = false;
                _unitPanel.alpha = 0.5f;
                StartCoroutine(EnemyTurn());
            }
            else
            {
                var targetPlayer = _players[turnNum];
                CurrentTurnUnit = targetPlayer; //현재 턴 유닛 정보 저장
                _turnUI.text = "Turn: " + _players[turnNum].Info.Id.ToString();

                bool isMyTurn = targetPlayer.isOwned && (CurrentTurnUnit != null && targetPlayer.Info.Id == CurrentTurnUnit.Info.Id);
                if (isMyTurn)
                {
                    UpdateUnitUI(targetPlayer);
                }
                targetPlayer.MyTurn(isMyTurn);
                // 내턴인지에 따라 패널 활성/비활성 처리
                _unitPanel.interactable = isMyTurn;
                _unitPanel.blocksRaycasts = isMyTurn;
                _unitPanel.alpha = isMyTurn ? 1.0f : 0.5f;
            }
        }
        // 적 턴일떄 잠시 코루틴으로 넘기기
        IEnumerator EnemyTurn()
        {
            yield return new WaitForSeconds(1.0f);
            NextTurn();
        }
        public void UpdateUnitUI(GamePlayerController unit)
        {
            bool isUnitTurn = unit.isOwned && (CurrentTurnUnit != null && unit.Info.Id == CurrentTurnUnit.Info.Id);

            // 선택된 유닛의 차례와 내 소유인지에 따라 패널 활성/비활성 처리
            _unitPanel.interactable = isUnitTurn;
            _unitPanel.blocksRaycasts = isUnitTurn;
            _unitPanel.alpha = isUnitTurn ? 1.0f : 0.5f;
            // 선택된 유닛의 정보로 교체
            _charaterIMG.sprite = unit.GetComponent<SpriteRenderer>().sprite;
            _hp.text = unit.Info.Hp.ToString();
            _san.text = unit.Info.San.ToString();
            _acc.text = unit.Info.Acc.ToString();
            _crit.text = unit.Info.Crit.ToString();
            _dmg.text = unit.Info.Atk.ToString();
            _prot.text = unit.Info.Def.ToString();
            _res.text = unit.Info.Hp.ToString();
            _dodge.text = unit.Info.Dodge.ToString();

            // 스킬 버튼 이벤트 재연결
            for (int i = 0; i < _skillBTN.Count; i++)
            {
                int index = i;
                _skillBTN[i].onClick.RemoveAllListeners();
                _skillBTN[i].onClick.AddListener(() => unit.OnClickSkillBtn(index));

                // 스킬 아이콘도 유닛에 맞게 변경 가능
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
        //적 선택 이미지 변경
        public void UpdateEnemyUI(int index)
        {
            var enemy = _enemys[StageNum-1].Enemys[index].GetComponent<EnemyController>();
            _enemyPanel.SetActive(true);
            _enemyUI.sprite = enemy.GetComponent<Image>().sprite;
        }
        //무결성 검사
        public void VerifyClientRequest(GamePlayerController caster, int skillIndex, int itemIndex, bool isEnemy,List<int> targets)
        {
            // 규칙 확인 후(아직 생각안해둠)



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
            //  패널 비활성 처리
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
