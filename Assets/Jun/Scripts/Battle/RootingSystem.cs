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
        public int rootId; //占쏙옙占쏙옙占쏙옙 占쏙옙占쏙옙 Id
        public Image unitText; // 占쏙옙占쏙옙占쏙옙 占쏙옙占쏙옙 占싣뤄옙 占쏙옙타占쏙옙占쏙옙 占싱뱄옙占쏙옙(占쌔쏙옙트)
        public Button unitBTN; //占쌘쏙옙占쏙옙 unit占쏙옙튼
        public Image RPCIMG; //占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙 占싱뱄옙占쏙옙
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
        public string name;      // 占쏙옙占쏙옙 占싱몌옙
        public Sprite icon;      // 占쏙옙占쏙옙占쏙옙 占싱뱄옙占쏙옙
        public bool isEquipment; // 占쏙옙澍㈉占?(true占쏙옙 占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙, false占쏙옙 占쏙옙화)
        public int amount;       // 占쏙옙화占쏙옙 占쏙옙占?占쌥억옙
    }

    [SerializeField] private BattleManager _manager;
    [SerializeField] private GameObject _panel;    //root占싻놂옙
    [SerializeField] private List<Image> _rootIMG;  //占쏙옙占쏙옙 占싱뱄옙占쏙옙
    [SerializeField] private List<Button> _rootBTN;    //占쏙옙占쏙옙 占쏙옙占쏙옙 占쏙옙튼

    [SerializeField] private Transform _unitTF;    //캐占쏙옙占쏙옙 占쏙옙占쏙옙 占쏙옙튼 占쏙옙치
    [SerializeField] private List<UnitRoot> _allUnit = new List<UnitRoot>();// 클占쏙옙占?占쏙옙占쏙옙 占쏙옙寬占?占쏙옙占쏙옙占쏙옙 占쌍댐옙 占쏙옙占쏙옙占?占쏙옙占쏙옙 占쏙옙占쏙옙트
    [SerializeField] private Button _unitPrefab;   //占쏙옙占쏙옙 占쏙옙튼 占쏙옙占쏙옙占쏙옙
    [SerializeField] private Image _idPrefab;   // 占쏙옙占쏙옙 占쏙옙占쏙옙 占쏙옙 占쏙옙타占쏙옙占쏙옙 占쏙옙占쏙옙 id
    [SerializeField] private List<Transform> _selectTF;  //占쏙옙占쏙옙 占쏙옙占쏙옙 占쏙옙 占쏙옙타占쏙옙占쏙옙 占쏙옙占쏙옙 id占쏙옙 占쏙옙치
    [SerializeField] private int[] _selectRootNum = new int[4]; // 媛?蹂댁긽 移몃퀎 ?좏깮 ?몄썝 ??
    [SerializeField] private int endSelectUnit = 0;

    // ?쒕쾭 ?꾩슜: _allUnit ?놁씠 ?좏깮 ?곹깭 異붿쟻 (unitIdx ??itemIdx)
    // SyncVar ?낆? ?대씪?댁뼵?몄뿉?쒕쭔 ?ㅽ뻾?섏뼱 ?쒕쾭??_allUnit??鍮꾩뼱?덉쑝誘濡?蹂꾨룄 愿由?
    private readonly Dictionary<int, int> _serverSelections = new Dictionary<int, int>();

    [SyncVar(hook = nameof(StartRooting))]
    public bool isEndStage = false;
    public int selectedUnit = -1;
    [Header("占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙 占쏙옙占쏙옙占쏙옙")]
    [SerializeField] private List<int> RPS;
    [SerializeField] private List<Sprite> RPSImage; // 占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙 占싱뱄옙占쏙옙
    [SerializeField] private Image RPSStartIMG;

    [Header("占쏙옙占쏙옙 占쏙옙占쏙옙占쏙옙")]
    [SerializeField] private List<RewardInfo> _rootDatas; // 占쏙옙占?占쏙옙占쏙옙 占쏙옙占쏙옙占쏙옙
    [SerializeField] private List<RewardInfo> _currentReward; //占쏙옙占쏙옙 占쏙옙占쏙옙 占쏙옙占쏙옙占쏙옙
    readonly SyncList<int> _currentRewardIndices = new SyncList<int>();
    private void Start() { _panel.SetActive(false); }

    [Server]
    public void ServerEndStage()
    {
        // ?쒕쾭 ?곹깭 珥덇린??
        _serverSelections.Clear();
        endSelectUnit = 0;
        for (int i = 0; i < _selectRootNum.Length; i++) _selectRootNum[i] = 0;

        _currentRewardIndices.Clear();
        _currentReward.Clear(); // ?쒕쾭?먯꽌??_currentReward 珥덇린??
        for (int i = 0; i < 4; i++)
        {
            int rand = UnityEngine.Random.Range(0, _rootDatas.Count);
            _currentRewardIndices.Add(rand);
            _currentReward.Add(_rootDatas[rand]); // ?쒕쾭?먯꽌??蹂댁긽 ?곗씠??梨꾩슦湲?
        }
        isEndStage = true;
    }

    public void StartRooting(bool oldVal, bool newVal)
    {
        if (newVal)
        {
            _panel.SetActive(true);
            // SyncVar ?낆? SyncList蹂대떎 癒쇱? ?꾩갑?????덉쑝誘濡?
            // ???꾨젅???湲???InitRooting???ㅽ뻾?⑸땲??
            StartCoroutine(InitRootingNextFrame());
        }
    }

    private System.Collections.IEnumerator InitRootingNextFrame()
    {
        yield return null; // SyncList ?숆린???湲?
        InitRooting();
    }
    // 占쏙옙占시시쏙옙占쏙옙 占십깍옙 占쏙옙占쏙옙占쏙옙

    public void InitRooting()
    {
        Debug.Log("PanelOnEnable");
        // 以묐났 諛⑹?瑜??꾪빐 由ъ뒪?몄? ?먯떇 ?ㅻ툕?앺듃 珥덇린??
        foreach (Transform child in _unitTF)
            Destroy(child.gameObject);
        _allUnit.Clear();
        _currentReward.Clear(); // ?ъ쭊????以묐났 ?꾩쟻 諛⑹?
        endSelectUnit = 0;

        // 蹂댁긽移몄쓽 ?꾩씠肄??명똿
        for (int i = 0; i < _currentRewardIndices.Count; i++)
        {
            int dataIdx = _currentRewardIndices[i];
            _currentReward.Add(_rootDatas[dataIdx]);
            _rootIMG[i].sprite = _rootDatas[dataIdx].icon;
        }
        // 占쏙옙占?占쏙옙占쌍듸옙占쏙옙 占쏙옙占싣곤옙占쏙옙 占쏙옙占쏙옙 占쏙옙튼 占쏙옙占쏙옙 占쏙옙 占쏙옙占쏙옙
        foreach (var unit in _manager._players)
        {
            var unitBTN = Instantiate(_unitPrefab, _unitTF);
            unitBTN.image.sprite = unit.GetCharacterSprite();

            _allUnit.Add(new UnitRoot(unit, -1, unitBTN));
            int capturedIndex = _allUnit.Count - 1;
            unitBTN.onClick.AddListener(() => OnClickedUnitBTN(capturedIndex));
            Debug.Log(_allUnit.Count - 1);

            //占쏙옙 占쏙옙占쏙옙占싹띰옙占쏙옙 활占쏙옙화 占쏙옙 활占쏙옙화 占쏙옙 占쏙옙占쏙옙占쏙옙占쏙옙 占쏙옙占?
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
    // 占쏙옙占쏙옙 占쏙옙튼占쏙옙 占쏙옙占쏙옙占쏙옙 占쏙옙 占쏙옙占쏙옙占쏙옙 占싱븝옙트
    public void OnClickedUnitBTN(int unitIdx)
    {
        Debug.Log("占쏙옙占쏙옙占쏙옙 占쏙옙占쏙옙占쏙옙 id: " + _allUnit[unitIdx].unit.Info.Id + " 占쏙옙占쏙옙占쏙옙 占쏙옙占쏙옙占쏙옙 index: " + unitIdx);
        selectedUnit = unitIdx;
        RootBTNActivate(true);
    }

    // 占쏙옙占쏙옙占쏙옙 占쏙옙튼占쏙옙 占쏙옙占쏙옙占쏙옙 占쏙옙 占쏙옙占쏙옙占쏙옙 占싱븝옙트
    public void OnClickedItem(int itemIndex)
    {
        if (selectedUnit == -1) return;
        CMDOnClickedRootBTN(selectedUnit, itemIndex);
    }

    // ?쒕쾭 ?꾩슜 ?뺤뀛?덈━(_serverSelections)濡??좏깮 異붿쟻 ??_allUnit? ?대씪?댁뼵???꾩슜
    [Command(requiresAuthority = false)]
    public void CMDOnClickedRootBTN(int unitIdx, int itemIdx)
    {
        int prevItemIdx = -1;

        // ?댁쟾???좏깮??蹂댁긽???덉쑝硫?痍⑥냼
        if (_serverSelections.TryGetValue(unitIdx, out int prev))
        {
            prevItemIdx = prev;
            _selectRootNum[prev]--;
            endSelectUnit--;
        }

        _serverSelections[unitIdx] = itemIdx;
        _selectRootNum[itemIdx]++;
        endSelectUnit++;

        Debug.Log($"[RootingSystem] ?좏깮 - unitIdx:{unitIdx}, itemIdx:{itemIdx}, ?꾨즺:{endSelectUnit}/{_manager._players.Count}");

        // ?대씪?댁뼵??UI ?낅뜲?댄듃 (prevItemIdx ?꾨떖 ???댁쟾 ?꾩씠肄??쒓굅??
        RpcRequestSelectRoot(unitIdx, itemIdx, prevItemIdx);

        if (endSelectUnit == _manager._players.Count) EndRooting();
    }

    [ClientRpc]
    void RpcRequestSelectRoot(int unitIdx, int itemIdx, int prevItemIdx)
    {
        // ?댁쟾 ?좏깮 ?꾩씠肄??쒓굅 (蹂寃???
        if (prevItemIdx != -1 && _allUnit[unitIdx].unitText != null)
        {
            Destroy(_allUnit[unitIdx].unitText.gameObject);
            _allUnit[unitIdx].unitText = null;
        }

        // ???좏깮 ?꾩씠肄??앹꽦
        var id = Instantiate(_idPrefab, _selectTF[itemIdx]);
        _allUnit[unitIdx].unitText = id;
        _allUnit[unitIdx].unitText.GetComponentInChildren<TextMeshProUGUI>().text
            = _allUnit[unitIdx].unit.Info.Id.ToString();

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
                // _allUnit? ?쒕쾭?먯꽌 鍮꾩뼱?덉쑝誘濡??쒕쾭 ?꾩슜 _serverSelections ?ъ슜
                RPS.Clear();
                foreach (var kvp in _serverSelections)
                {
                    if (kvp.Value == i)
                    {
                        RPS.Add(kvp.Key);
                        Debug.Log($"[RPS 李멸?] unitIdx={kvp.Key}");
                    }
                }

                if (RPS.Count < 2)
                {
                    Debug.LogWarning($"[ProcessAllBattles] ?щ’ {i}: 李멸???{RPS.Count}紐? RPS ?앸왂");
                    continue;
                }

                yield return StartCoroutine(RockPaperScissors(RPS));
            }
        }

        Debug.Log("[RootingSystem] Rooting 완료");

        // RoundManager 가 있으면 라운드 진행/완료 처리에 위임 (마지막 라운드면 hideout 귀환).
        // 없으면 fallback 으로 Home 1 직행.
        var rm = RoundManager.Instance;
        if (rm != null)
            rm.OnRoundCleared();
        else
            NetworkManager.singleton.ServerChangeScene("Home");
    }
    //占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙 占쏙옙占쏙옙
    IEnumerator RockPaperScissors(List<int> RPS)
    {
        Debug.Log("占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙 占쏙옙占쏙옙");
        RpcStartRPS(true);
        yield return new WaitForSeconds(1.0f);
        RpcStartRPS(false);

        bool isDraw = true;
        while (isDraw) {
            Debug.Log("占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙 占쏙옙");
            List<int> hands = new List<int>();
            for (int i = 0; i < RPS.Count; i++) hands.Add(UnityEngine.Random.Range(0, 3));

            RpcShowRPS(RPS, hands);
            yield return new WaitForSeconds(1.5f); 

            bool hasRock = hands.Exists(x => x == 0);
            bool hasPaper = hands.Exists(x => x == 1);
            bool hasScissors = hands.Exists(x => x == 2);
            int winner = -1;

            if (hasRock && hasPaper && hasScissors) winner = -1; //占쏙옙寬占?占쏙옙 占쌕몌옙占쏙옙
            else if (hasRock && hasScissors) winner = 0;
            else if (hasPaper && hasRock) winner = 1;
            else if (hasScissors && hasPaper) winner = 2;
            else winner = -1;

            if (winner == -1) { Debug.Log("占쏙옙占승븝옙 占쏙옙占쏙옙"); continue; } // 占쏙옙占승부곤옙 占쏙옙占시쏙옙 占쌕쏙옙 占쏙옙占쏙옙

            // 4. 占싻뱄옙占쏙옙 占쏙옙占쏙옙 (占쌘울옙占쏙옙占쏙옙占쏙옙 占쏙옙占쏙옙占쌔억옙 占싸듸옙占쏙옙占쏙옙 占쏙옙 占쏙옙占쏙옙)
            for (int i = RPS.Count - 1; i >= 0; i--)
            {
                if (hands[i] != winner)
                {
                    int loserIdx = RPS[i];
                    Debug.Log($"占싻뱄옙占쏙옙 탈占쏙옙: {_allUnit[loserIdx].unit.Info.Id}");

                    RpcHideRPSIcon(loserIdx);
                    RPS.RemoveAt(i);
                }
            }
            if (RPS.Count <= 1)
            {
                isDraw = false;
                Debug.Log("[RootingSystem] RPS winner decided");
                //RpcHideRPSIcon(RPS[0]);
            }
        }
    }
    [ClientRpc]
    public void RpcStartRPS(bool isStart)
    {
        RPSStartIMG.gameObject.SetActive(isStart);
    }
    // 占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙 占싱뱄옙占쏙옙 占쏙옙占쏙옙占쌍깍옙
    [ClientRpc]
    public void RpcShowRPS(List<int> RPS, List<int> hands)
    {
        Debug.Log("[RootingSystem] Show RPS");
        for(int i = 0; i < RPS.Count; i++)
        {
            _allUnit[RPS[i]].RPCIMG.sprite = RPSImage[hands[i]];
            _allUnit[RPS[i]].RPCIMG.gameObject.SetActive(true);
            Debug.Log("[RootingSystem] Show RPS");
        }
    }
    // 占쏙옙占쏙옙占쏙옙占쏙옙占쏙옙 占싱뱄옙占쏙옙 占쏙옙占쏙옙占?占쏙옙占쏙옙
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
