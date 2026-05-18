using Jun;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;



public class RootingSystem : NetworkBehaviour
{
    [System.Serializable]
    class UnitRoot
    {
        public GamePlayerController unit;
        public int rootId; // 루팅 보상 Id
        public Image unitText;
        public Button unitBTN; // 유닛 버튼
        public Image RPCIMG;
        public UnitRoot() { }
        public UnitRoot(GamePlayerController unit, int rootId, Button unitBTN)
        {
            this.unit = unit;
            this.rootId = rootId;
            this.unitBTN = unitBTN;
            Transform child = unitBTN.transform.Find("RPSIcon");

            if (child != null)
            {
                this.RPCIMG = child.GetComponent<Image>();
                this.RPCIMG.gameObject.SetActive(false);
            }
        }
    }
    [System.Serializable]
    public struct RewardInfo
    {
        public string name;
        public Sprite icon;
        public bool isEquipment; // 장비 여부 (true: 장비, false: 소모품)
        public int amount;       // 소모품 수량
    }

    [SerializeField] private BattleManager _manager;
    [SerializeField] private GameObject _panel;    // 루팅 패널 루트
    [SerializeField] private List<Image> _rootIMG;
    [SerializeField] private List<Button> _rootBTN;    // 보상 아이템 버튼

    [SerializeField] private Transform _unitTF;    // 캐릭터 유닛 버튼 부모
    [SerializeField] private List<UnitRoot> _allUnit = new List<UnitRoot>();// 모든 유닛 루팅 정보 목록
    [SerializeField] private Button _unitPrefab;   // 유닛 버튼 프리팹
    [SerializeField] private Image _idPrefab;   // 선택된 보상 표시용 아이콘 id
    [SerializeField] private List<Transform> _selectTF;  // 선택된 보상 표시용 아이콘 id 부모
    [SerializeField] private int[] _selectRootNum = new int[4];
    [SerializeField] private int endSelectUnit = 0;



    private readonly Dictionary<int, int> _serverSelections = new Dictionary<int, int>();

    [SyncVar(hook = nameof(StartRooting))]
    public bool isEndStage = false;
    public int selectedUnit = -1;
    [Header("가위바위보 설정")]
    [SerializeField] private List<int> RPS;
    [SerializeField] private List<Sprite> RPSImage;
    [SerializeField] private Image RPSStartIMG;

    [Header("루팅 보상 목록")]
    [SerializeField] private List<RewardInfo> _rootDatas; // 전체 보상 데이터
    [SerializeField] private List<RewardInfo> _currentReward; // 현재 라운드 보상 목록
    readonly SyncList<int> _currentRewardIndices = new SyncList<int>();
    private void Start() { _panel.SetActive(false); }

    [Server]
    public void ServerEndStage()
    {

        _serverSelections.Clear();
        endSelectUnit = 0;
        for (int i = 0; i < _selectRootNum.Length; i++) _selectRootNum[i] = 0;

        _currentRewardIndices.Clear();
        _currentReward.Clear(); // ??뺤쒔?癒?퐣??_currentReward ?λ뜃由??
        for (int i = 0; i < 4; i++)
        {
            int rand = UnityEngine.Random.Range(0, _rootDatas.Count);
            _currentRewardIndices.Add(rand);
            _currentReward.Add(_rootDatas[rand]);
        }
        isEndStage = true;
    }

    public void StartRooting(bool oldVal, bool newVal)
    {
        if (newVal)
        {
            _panel.SetActive(true);
            // SyncVar 및 SyncList 동기화가 완료될 때까지 한 프레임 대기
            // 다음 프레임에 InitRooting 호출
            StartCoroutine(InitRootingNextFrame());
        }
    }

    private System.Collections.IEnumerator InitRootingNextFrame()
    {
        yield return null; // SyncList 동기화 대기
        InitRooting();
    }
    // 루팅 보상 초기화

    public void InitRooting()
    {
        Debug.Log("[RootingSystem] 루팅 패널 초기화");

        foreach (Transform child in _unitTF)
            Destroy(child.gameObject);
        _allUnit.Clear();
        _currentReward.Clear(); // 이전 보상 목록 초기화
        endSelectUnit = 0;


        for (int i = 0; i < _currentRewardIndices.Count; i++)
        {
            int dataIdx = _currentRewardIndices[i];
            _currentReward.Add(_rootDatas[dataIdx]);
            _rootIMG[i].sprite = _rootDatas[dataIdx].icon;
        }
        // 플레이어 유닛별 버튼 생성
        foreach (var unit in _manager._players)
        {
            var unitBTN = Instantiate(_unitPrefab, _unitTF);
            unitBTN.image.sprite = unit.GetCharacterSprite();

            _allUnit.Add(new UnitRoot(unit, -1, unitBTN));
            int capturedIndex = _allUnit.Count - 1;
            unitBTN.onClick.AddListener(() => OnClickedUnitBTN(capturedIndex));
            Debug.Log($"[RootingSystem] 루팅 대상 버튼 생성 - unitIdx:{_allUnit.Count - 1}");

            // 로컬 플레이어만 클릭 가능하도록 설정
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
    // 유닛 버튼 클릭 시 선택 처리
    public void OnClickedUnitBTN(int unitIdx)
    {
        Debug.Log($"[RootingSystem] 유닛 선택 - id:{_allUnit[unitIdx].unit.Info.Id}, index:{unitIdx}");
        selectedUnit = unitIdx;
        RootBTNActivate(true);
    }

    // 아이템 버튼 클릭 시 선택된 유닛에 보상 배분
    public void OnClickedItem(int itemIndex)
    {
        if (selectedUnit == -1) return;
        CMDOnClickedRootBTN(selectedUnit, itemIndex);
    }

    // 서버에서 선택 기록(_serverSelections)을 갱신하고 _allUnit의 선택 상태를 동기화
    [Command(requiresAuthority = false)]
    public void CMDOnClickedRootBTN(int unitIdx, int itemIdx)
    {
        int prevItemIdx = -1;

        // 이전 선택이 있으면 되돌리기
        if (_serverSelections.TryGetValue(unitIdx, out int prev))
        {
            prevItemIdx = prev;
            _selectRootNum[prev]--;
            endSelectUnit--;
        }

        _serverSelections[unitIdx] = itemIdx;
        _selectRootNum[itemIdx]++;
        endSelectUnit++;

        Debug.Log($"[RootingSystem] 보상 선택 - unitIdx:{unitIdx}, itemIdx:{itemIdx}, 완료:{endSelectUnit}/{_manager._players.Count}");

        // 이전 선택의 UI 업데이트 반영 (prevItemIdx가 유효한 경우 되돌림)
        RpcRequestSelectRoot(unitIdx, itemIdx, prevItemIdx);

        if (endSelectUnit == _manager._players.Count) EndRooting();
    }

    [ClientRpc]
    void RpcRequestSelectRoot(int unitIdx, int itemIdx, int prevItemIdx)
    {
        // 새로운 선택 UI 업데이트 반영 (추가)
        if (prevItemIdx != -1 && _allUnit[unitIdx].unitText != null)
        {
            Destroy(_allUnit[unitIdx].unitText.gameObject);
            _allUnit[unitIdx].unitText = null;
        }

        // 새 선택 UI 반영
        var id = Instantiate(_idPrefab, _selectTF[itemIdx]);
        _allUnit[unitIdx].unitText = id;
        _allUnit[unitIdx].unitText.GetComponentInChildren<TextMeshProUGUI>().text
            = _allUnit[unitIdx].unit.Info.Id.ToString();

        RootBTNActivate(false);
    }
    [Server]
    void EndRooting()
    {
        Debug.Log("[RootingSystem] 루팅 선택 완료");
        StartCoroutine(ProcessAllBattlesRoutine());
        
    }
    IEnumerator ProcessAllBattlesRoutine()
    {
        for (int i = 0; i < 4; i++)
        {
            if (_selectRootNum[i] > 1 && _currentReward[i].isEquipment)
            {
                // _allUnit의 서버 선택을 동기화하여 서버 선택 _serverSelections 반영
                RPS.Clear();
                foreach (var kvp in _serverSelections)
                {
                    if (kvp.Value == i)
                    {
                        RPS.Add(kvp.Key);
                        Debug.Log($"[RPS 참가] unitIdx={kvp.Key}");
                    }
                }

                if (RPS.Count < 2)
                {
                    Debug.LogWarning($"[ProcessAllBattles] 보상 {i}: 참가자가 {RPS.Count}명이라 RPS를 생략합니다.");
                    continue;
                }

                yield return StartCoroutine(RockPaperScissors(RPS));
            }
        }

        Debug.Log("[RootingSystem] 루팅 완료");



        var rm = RoundManager.Instance;
        if (rm != null)
            rm.OnRoundCleared();
        else
            NetworkManager.singleton.ServerChangeScene("Home");
    }
    // 루팅 보상 분배 완료 처리
    IEnumerator RockPaperScissors(List<int> RPS)
    {
        Debug.Log("[RootingSystem] 가위바위보 시작");
        RpcStartRPS(true);
        yield return new WaitForSeconds(1.0f);
        RpcStartRPS(false);

        bool isDraw = true;
        while (isDraw) {
            Debug.Log("[RootingSystem] 가위바위보 재시도");
            List<int> hands = new List<int>();
            for (int i = 0; i < RPS.Count; i++) hands.Add(UnityEngine.Random.Range(0, 3));

            RpcShowRPS(RPS, hands);
            yield return new WaitForSeconds(1.5f); 

            bool hasRock = hands.Exists(x => x == 0);
            bool hasPaper = hands.Exists(x => x == 1);
            bool hasScissors = hands.Exists(x => x == 2);
            int winner = -1;

            if (hasRock && hasPaper && hasScissors) winner = -1;
            else if (hasRock && hasScissors) winner = 0;
            else if (hasPaper && hasRock) winner = 1;
            else if (hasScissors && hasPaper) winner = 2;
            else winner = -1;

            if (winner == -1) { Debug.Log("[RootingSystem] 무승부 - 재시도"); continue; }


            for (int i = RPS.Count - 1; i >= 0; i--)
            {
                if (hands[i] != winner)
                {
                    int loserIdx = RPS[i];
                    Debug.Log($"[RootingSystem] RPS 탈락: {_allUnit[loserIdx].unit.Info.Id}");

                    RpcHideRPSIcon(loserIdx);
                    RPS.RemoveAt(i);
                }
            }
            if (RPS.Count <= 1)
            {
                isDraw = false;
                Debug.Log("[RootingSystem] RPS 승자 결정");
                //RpcHideRPSIcon(RPS[0]);
            }
        }
    }
    [ClientRpc]
    public void RpcStartRPS(bool isStart)
    {
        RPSStartIMG.gameObject.SetActive(isStart);
    }

    [ClientRpc]
    public void RpcShowRPS(List<int> RPS, List<int> hands)
    {
        Debug.Log("[RootingSystem] RPS 표시");
        for(int i = 0; i < RPS.Count; i++)
        {
            _allUnit[RPS[i]].RPCIMG.sprite = RPSImage[hands[i]];
            _allUnit[RPS[i]].RPCIMG.gameObject.SetActive(true);
            Debug.Log("[RootingSystem] RPS 표시");
        }
    }

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
