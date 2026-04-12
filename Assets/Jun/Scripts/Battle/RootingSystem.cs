using JetBrains.Annotations;
using Jun;
using Mirror;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;



public class RootingSystem : NetworkBehaviour
{
    [System.Serializable]
    class UnitRoot
    {
        public GamePlayerController unit;
        public int rootId; //선택한 보상 Id
        public Image unitText; // 선택후 보상 아래 나타나는 이미지(텍스트)
        public Button unitBTN; //자신의 unit버튼
        public Image RPCIMG; //가위바위보 이미지
        public UnitRoot() { }
        public UnitRoot(GamePlayerController unit, int rootId, Button unitBTN)
        {
            this.unit = unit;
            this.rootId = rootId;
            this.unitBTN = unitBTN;
            Transform child = unitBTN.transform.Find("RPSIcon");

            if (child != null)
            {
                // 2. 자식을 찾았다면, 그 안에 있는 Image 컴포넌트를 RPCIMG에 넣어줍니다.
                this.RPCIMG = child.GetComponent<Image>();

                // 3. 이제 RPCIMG는 더 이상 Null이 아니므로, 마음껏 명령을 내려도 됩니다!
                this.RPCIMG.gameObject.SetActive(false);
            }
        }
    }
    [System.Serializable]
    public struct RewardInfo
    {
        public string name;      // 보상 이름
        public Sprite icon;      // 보여줄 이미지
        public bool isEquipment; // 장비여부 (true면 가위바위보, false면 금화)
        public int amount;       // 금화일 경우 금액
    }

    [SerializeField] private BattleManager _manager;
    [SerializeField] private GameObject _panel;    //root패널
    [SerializeField] private List<Image> _rootIMG;  //보상 이미지
    [SerializeField] private List<Button> _rootBTN;    //보상 선택 버튼

    [SerializeField] private Transform _unitTF;    //캐릭터 선택 버튼 위치
    [SerializeField] private List<UnitRoot> _allUnit = new List<UnitRoot>();// 클라와 서버 모두가 가지고 있는 공통된 유닛 리스트
    [SerializeField] private Button _unitPrefab;   //유닛 버튼 프리팹
    [SerializeField] private Image _idPrefab;   // 보상 선택 후 나타나는 유닛 id
    [SerializeField] private List<Transform> _selectTF;  //보상 선택 후 나타나는 유닛 id의 위치
    [SerializeField] private int[] _selectRootNum = new int[4]; // 각 보상 별 선택한 유닛의 수
    [SerializeField] private int endSelectUnit = 0;  

    [SyncVar(hook = nameof(StartRooting))]
    public bool isEndStage = false;
    public int selectedUnit = -1;
    [Header("가위바위보 정보들")]
    [SerializeField] private List<int> RPS;
    [SerializeField] private List<Sprite> RPSImage; // 가위바위보 이미지
    [SerializeField] private Image RPSStartIMG;

    [Header("보상 정보들")]
    [SerializeField] private List<RewardInfo> _rootDatas; // 모든 보상 정보들
    [SerializeField] private List<RewardInfo> _currentReward; //현재 보상 정보들
    readonly SyncList<int> _currentRewardIndices = new SyncList<int>();
    private void Start() { _panel.SetActive(false); }

    // 서버쪽에서 스테이지가 끝난다면 실행 됨
    [Server]
    public void ServerEndStage() 
    {
        _currentRewardIndices.Clear();
        for (int i = 0; i < 4; i++)
        {
            int rand = UnityEngine.Random.Range(0, _rootDatas.Count);
            _currentRewardIndices.Add(rand); // SyncList에 넣는 순간 모든 클라에 전달됨
        }
        isEndStage = true;
    }

    public void StartRooting(bool oldVal, bool newVal)
    {
        if (newVal)
        {
            _panel.SetActive(true);
            // 패널이 켜지면서 OnEnable이 실행될 것입니다.
            InitRooting();
        }
    }
    // 루팅시스템 초기 설정들

    public void InitRooting()
    {
        Debug.Log("PanelOnEnable");
        // 중복 생성 방지를 위해 리스트와 자식 오브젝트 초기화
        foreach (Transform child in _unitTF)
            Destroy(child.gameObject);
        _allUnit.Clear();
        endSelectUnit = 0;

        // 보상들이 다 보이게 설정
        for (int i = 0; i < _currentRewardIndices.Count; i++)
        {
            int dataIdx = _currentRewardIndices[i];
            _currentReward.Add(_rootDatas[dataIdx]);
            _rootIMG[i].sprite = _rootDatas[dataIdx].icon;
        }
        // 모든 유닛들을 돌아가며 유닛 버튼 생성 및 연결
        foreach (var unit in _manager._players)
        {
            var unitBTN = Instantiate(_unitPrefab, _unitTF);
            unitBTN.image.sprite = _manager.PlayerImages[unit.Info.Id];

            _allUnit.Add(new UnitRoot(unit, -1, unitBTN));
            int capturedIndex = _allUnit.Count - 1;
            unitBTN.onClick.AddListener(() => OnClickedUnitBTN(capturedIndex));
            Debug.Log(_allUnit.Count - 1);

            //내 유닛일때만 활성화 비 활성화 등 여러가지 기능
            if (unit.isOwned)
            {
                unitBTN.interactable = true;
                unitBTN.image.color = Color.white;
            }
            else
            {
                unitBTN.interactable = false;
                unitBTN.image.color = new Color(1, 1, 1, 0.4f);
            }
        }
        RootBTNActivate(false);
    }
    // 유닛 버튼을 눌렀을 때 나오는 이벤트
    public void OnClickedUnitBTN(int unitIdx)
    {
        Debug.Log("선택한 유닛의 id: " + _allUnit[unitIdx].unit.Info.Id + " 선택한 유닛의 index: " + unitIdx);
        selectedUnit = unitIdx;
        RootBTNActivate(true);
    }

    // 아이템 버튼을 눌렀을 때 나오는 이벤트
    public void OnClickedItem(int itemIndex)
    {
        if (selectedUnit == -1) return;
        CMDOnClickedRootBTN(selectedUnit, itemIndex);
    }

    //서버에게 선택된 유닛과 아이템으로 업데이트 요청
    [Command(requiresAuthority = false)]
    public void CMDOnClickedRootBTN(int unitIdx, int itemIdx)
    {
        if (_allUnit[unitIdx].rootId != -1) { Destroy(_allUnit[unitIdx].unitText.gameObject); endSelectUnit--; _selectRootNum[itemIdx]--; }

        _allUnit[unitIdx].rootId = itemIdx;
        endSelectUnit++;
        _selectRootNum[itemIdx]++;

        Debug.Log($"서버 수신 - 유닛:{_allUnit[unitIdx].unit.Info.Id}, 아이템:{itemIdx}");

        // 모든 클라이언트의 UI를 업데이트하도록 RPC 호출
        RpcRequestSelectRoot(unitIdx, itemIdx);

        if (endSelectUnit == _allUnit.Count) EndRooting();
    }

    // 모든 클라이언트에서 해당 유닛 위에 보상 UI 생성
    [ClientRpc]
    void RpcRequestSelectRoot(int unitIdx, int itemIdx)
    {
        var id = Instantiate(_idPrefab, _selectTF[itemIdx]);
        _allUnit[unitIdx].unitText = id;
        _allUnit[unitIdx].unitText.GetComponentInChildren<TextMeshProUGUI>().text = _allUnit[unitIdx].unit.Info.Id.ToString();

        RootBTNActivate(false);
    }
    [Server]
    void EndRooting()
    {
        Debug.Log("EndRooting");
        StartCoroutine(ProcessAllBattlesRoutine());
        
    }
    IEnumerator ProcessAllBattlesRoutine()
    {
        for (int i = 0; i < 4; i++)
        {
            if (_selectRootNum[i] > 1 && _currentReward[i].isEquipment)
            {
                RPS.Clear(); //가위바위보 참가자
                for (int j = 0; j < _allUnit.Count; j++)
                {
                    if (_allUnit[j].rootId == i)
                    {
                        RPS.Add(j);
                        Debug.Log(_allUnit[j].unit.Info.Id);
                    }
                }
                yield return StartCoroutine(RockPaperScissors(RPS)); // 이 함수가 끝날때까지 정지
            }
        }

        Debug.Log("모든 칸의 가위바위보 완료.");
        // 여기서 다음 단계 만들기
        // 선택한 보상들 적용하기 귀찮다
    }
    //가위바위보 로직
    IEnumerator RockPaperScissors(List<int> RPS)
    {
        Debug.Log("가위바위보 시작");
        RpcStartRPS(true);
        yield return new WaitForSeconds(1.0f);
        RpcStartRPS(false);

        bool isDraw = true;
        while (isDraw) {
            Debug.Log("가위바위보 중");
            List<int> hands = new List<int>();
            for (int i = 0; i < RPS.Count; i++) hands.Add(UnityEngine.Random.Range(0, 3));

            RpcShowRPS(RPS, hands);
            yield return new WaitForSeconds(1.5f); 

            bool hasRock = hands.Exists(x => x == 0);
            bool hasPaper = hands.Exists(x => x == 1);
            bool hasScissors = hands.Exists(x => x == 2);
            int winner = -1;

            if (hasRock && hasPaper && hasScissors) winner = -1; //모두가 다 다를때
            else if (hasRock && hasScissors) winner = 0;
            else if (hasPaper && hasRock) winner = 1;
            else if (hasScissors && hasPaper) winner = 2;
            else winner = -1;

            if (winner == -1) { Debug.Log("무승부 재경기"); continue; } // 무승부가 나올시 다시 진행

            // 4. 패배자 제거 (뒤에서부터 삭제해야 인덱스가 안 꼬임)
            for (int i = RPS.Count - 1; i >= 0; i--)
            {
                if (hands[i] != winner)
                {
                    int loserIdx = RPS[i];
                    Debug.Log($"패배자 탈락: {_allUnit[loserIdx].unit.Info.Id}");

                    RpcHideRPSIcon(loserIdx);
                    RPS.RemoveAt(i);
                }
            }
            if (RPS.Count <= 1)
            {
                isDraw = false;
                Debug.Log("승리자: " + _allUnit[RPS[0]].unit.Info.Id);
                //RpcHideRPSIcon(RPS[0]);
            }
        }
    }
    [ClientRpc]
    public void RpcStartRPS(bool isStart)
    {
        RPSStartIMG.gameObject.SetActive(isStart);
    }
    // 가위바위보 이미지 보여주기
    [ClientRpc]
    public void RpcShowRPS(List<int> RPS, List<int> hands)
    {
        Debug.Log("가위바위보 결과");
        for(int i = 0; i < RPS.Count; i++)
        {
            _allUnit[RPS[i]].RPCIMG.sprite = RPSImage[hands[i]];
            _allUnit[RPS[i]].RPCIMG.gameObject.SetActive(true);
            Debug.Log("가위바위보 결과:"+ _allUnit[RPS[i]].unit.Info.Id + " hand: "+ hands[i]);
        }
    }
    // 가위바위보 이미지 끄라고 명령
    [ClientRpc]
    public void RpcHideRPSIcon(int Idx)
    {
        _allUnit[Idx].RPCIMG.gameObject.SetActive(false);
    }
    public void RootBTNActivate(bool IsAct)
    {
        foreach (Button BTN in _rootBTN) BTN.gameObject.SetActive(IsAct);
    }
}