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

        [Header("전투 유닛 프리팹")]
        [SerializeField] private List<GameObject> BattleUnitPrefabs;
        [Header("캐릭터 정보 창")]
        [SerializeField] private CanvasGroup _unitPanel;
        [SerializeField] private Image _charaterIMG; public Image CharaterIMG => _charaterIMG;
        [Header("캐릭터 관련 UI들")]
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

        [Header("적")]// 실험을 위해 잠시 버튼으로 만듬, 후에 GameObject로 바꿀 예정
        [SerializeField] private int _stageNum = 1; public int StageNum => _stageNum; 
        [SerializeField] private List<BattleEnemyInfo> _enemys; public List<BattleEnemyInfo> Enemys => _enemys;
        [SerializeField] private GameObject _enemyPanel; public GameObject EnemyPanel => _enemyPanel;
        [SerializeField] private Image _enemyUI;
        [SerializeField] private TextMeshProUGUI _enemyName;

        public int EnemyNum;

        [Header("턴 정보")]
        [SerializeField] private Transform _turnPanel;  //턴 보여주는 장소
        [SerializeField] private Image _turnUi;  // 턴이 보여지는 이미지
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

        [Header("핑 시스템")]
        [SerializeField] private List<GameObject> _pingList = new List<GameObject>();



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
            StartCoroutine(SetupBattleFlow());
        }
        [Server]
        private IEnumerator SetupBattleFlow()
        {
            var roomManager = NetworkManager.singleton as GameRoomManager;

            PlayerData[] survivors;
            while (true)
            {
                survivors = FindObjectsByType<PlayerData>(FindObjectsSortMode.None);
                if (survivors.Length == roomManager.HeroNum) break;
                yield return null;
            }

            Debug.Log("전원 도착! 전투 몸통을 생성합니다.");

            foreach (var data in survivors)
            {
                // 1. 데이터(FinalHeroIndex)에 맞는 전투용 프리팹 생성! (위치는 스폰 포인트로)
                GameObject battleObj = Instantiate(BattleUnitPrefabs[data.FinalHeroIndex], SpawnPoints[data.FinalHeroPos].position, Quaternion.identity);

                // 2. 프리팹 안의 컨트롤러를 꺼내서 데이터(영혼) 주입!
                var controller = battleObj.GetComponent<GamePlayerController>();
                controller.InjectData(data);

                // 3. [가장 중요] 서버에 스폰하면서, 해당 클라이언트에게 조종 권한 주기!
                // PlayerData를 가지고 온 원래 주인의 연결(connectionToClient)을 새 몸통에 연결해줍니다.
                NetworkServer.Spawn(battleObj, data.connectionToClient);

                // 4. 전투 리스트에 컨트롤러 등록
                _players.Add(controller);
                _turnList.Add(new TurnData("Player", data.Info.Spd, _players.Count - 1));
            }

            // 5. 턴 시작
            Invoke(nameof(StartFirstTurn), 1.0f);
        }

        [Server]
        private void StartFirstTurn()
        {
            Order = -1; // 확실하게 초기화
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
                    // 플레이어는 들어오는 데 시간이 걸릴 수 있으니 방어 코드 사용
                    if (targetNum < _players.Count)
                    {
                        sp = _players[targetNum].GetComponent<SpriteRenderer>().sprite;
                    }
                    else
                    {
                        Debug.LogWarning($"아직 {targetNum}번 플레이어가 덜 들어왔습니다.");
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

            Debug.Log($"턴 UI 업데이트 - 인덱스: {index}, 상태: {isTurn}");
            GameObject go = _turnUIList[index].transform.Find("HighLight").gameObject;
            go.SetActive(isTurn);
        }
        // 다음 턴으로 진행
        [Server]
        public void NextTurn()
        {
            //이전 턴 하이라이트 끄기
            RpcSetHighlight(Order, false);

            Order = (Order + 1) % _turnList.Count;
            if (Order == 0) _turnList.Sort((a, b) => b.speed.CompareTo(a.speed));

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
            RpcSetHighlight(Order, true);
        }
        // 턴 변경
        [ClientRpc]
        public void RpcChangeTurn(string typeTurn, int turnNum)
        {
            if (CurrentTurnUnit != null && CurrentTurnUnit is GamePlayerController)
            {
                CurrentTurnUnit.MyTurn(false);
            }

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
                
                //누구 것이든 상관없이 무조건 노란 선을 표시
                targetPlayer.MyTurn(true);

                bool isMyTurn = targetPlayer.isOwned;
                if (isMyTurn)
                {
                    UpdateUnitUI(targetPlayer);
                }
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
            _movePosBTN.onClick.RemoveAllListeners();
            _movePosBTN.onClick.AddListener(() => unit.OnClickMoveBtn());
        }
        //적 선택 이미지 변경
        public void UpdateEnemyUI(int index)
        {
            var enemy = _enemys[StageNum-1].Enemys[index].GetComponent<EnemyController>();
            _enemyPanel.SetActive(true);
            _enemyUI.sprite = enemy.GetComponent<Image>().sprite;
        }
        //유닛 위치 이동
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
            // 자식 수 확인
            Debug.Log($"pingIndex: {pingIndex} / PingLayout 자식 수: {pingLayout.childCount}");

            if (pingIndex >= pingLayout.childCount)
            {
                Debug.LogWarning("PingLayout 자식이 부족함!");
                return;
            }

            // 해당 인덱스 자식 오브젝트 켜기
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
