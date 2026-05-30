using JetBrains.Annotations;
using Jun;
using Lsy;
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
        public int rootId; //  Id
        public Image unitText; //   Ʒ Ÿ ̹(ؽƮ)
        public Button unitBTN; //ڽ unitư
        public Image RPCIMG; // ̹
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
        public bool    isEquipment; // 장비여부 (true면 아이템, false면 골드)
        public int     amount;      // 골드일 경우 금액 (isEquipment=false 일 때 사용)
        public ItemSO  itemSO;      // 아이템 데이터 (isEquipment=true 일 때 연결)
        public Equipment equipment;
        public Sprite  fallbackIcon; // itemSO가 없을 때(골드 등) 사용할 아이콘

        /// <summary>표시할 아이콘. itemSO가 있으면 우선 사용, 없으면 fallbackIcon.</summary>
        public Sprite Icon => equipment?.EqpItem?.icon ?? fallbackIcon;
    }

    [SerializeField] private BattleManager _manager;
    [SerializeField] private GameObject _panel;    //rootг
    [SerializeField] private List<Image> _rootIMG;  // ̹
    [SerializeField] private List<Button> _rootBTN;    //  ư

    [SerializeField] private Transform _unitTF;    //ĳ  ư ġ
    [SerializeField] private List<UnitRoot> _allUnit = new List<UnitRoot>();// Ŭ  ΰ  ִ   Ʈ
    [SerializeField] private Button _unitPrefab;   // ư 
    [SerializeField] private Image _idPrefab;   //    Ÿ  id
    [SerializeField] private List<Transform> _selectTF;  //   Ÿ  id ġ
    [SerializeField] private int[] _selectRootNum = new int[4]; // 각 보상 칸별 선택 인원 수
    [SerializeField] private int endSelectUnit = 0;

    // 서버 전용: _allUnit 없이 선택 상태 추적 (unitIdx → itemIdx)
    // SyncVar 훅은 클라이언트에서만 실행되어 서버의 _allUnit이 비어있으므로 별도 관리
    private readonly Dictionary<int, int> _serverSelections = new Dictionary<int, int>();

    [SyncVar(hook = nameof(StartRooting))]
    public bool isEndStage = false;
    public int selectedUnit = -1;
    [Header(" ")]
    [SerializeField] private List<int> RPS;
    [SerializeField] private List<Sprite> RPSImage; //  ̹
    [SerializeField] private Image RPSStartIMG;

    [Header(" ")]
    [SerializeField] private List<RewardInfo> _rootDatas; //   
    [SerializeField] private List<RewardInfo> _currentReward; //  
    readonly SyncList<int> _currentRewardIndices = new SyncList<int>();
    private void Start() { _panel.SetActive(false); }

    [Server]
    public void ServerEndStage()
    {
        // 서버 상태 초기화
        _serverSelections.Clear();
        endSelectUnit = 0;
        for (int i = 0; i < _selectRootNum.Length; i++) _selectRootNum[i] = 0;

        _currentRewardIndices.Clear();
        _currentReward.Clear(); // 서버에서도 _currentReward 초기화
        for (int i = 0; i < 4; i++)
        {
            int rand = UnityEngine.Random.Range(0, _rootDatas.Count);
            _currentRewardIndices.Add(rand);
            _currentReward.Add(_rootDatas[rand]); // 서버에서도 보상 데이터 채우기
        }
        isEndStage = true;
    }

    public void StartRooting(bool oldVal, bool newVal)
    {
        if (newVal)
        {
            _panel.SetActive(true);
            // SyncVar 훅은 SyncList보다 먼저 도착할 수 있으므로
            // 한 프레임 대기 후 InitRooting을 실행합니다.
            StartCoroutine(InitRootingNextFrame());
        }
    }

    private System.Collections.IEnumerator InitRootingNextFrame()
    {
        yield return null; // SyncList 동기화 대기
        InitRooting();
    }

    public void InitRooting()
    {
        Debug.Log("PanelOnEnable");
        // 중복 방지를 위해 리스트와 자식 오브젝트 초기화
        foreach (Transform child in _unitTF)
            Destroy(child.gameObject);
        _allUnit.Clear();
        _currentReward.Clear(); // 재진입 시 중복 누적 방지
        endSelectUnit = 0;

        // 보상칸의 아이콘 세팅
        for (int i = 0; i < _currentRewardIndices.Count; i++)
        {
            int dataIdx = _currentRewardIndices[i];
            var reward = _rootDatas[dataIdx];
            _currentReward.Add(reward);
            // itemSO가 있으면 그 아이콘, 없으면 fallbackIcon 사용
            _rootIMG[i].sprite = reward.Icon;
        }
            //ֵ ư ư 
        foreach (var unit in _manager._players)
        {
            var unitBTN = Instantiate(_unitPrefab, _unitTF);
            unitBTN.image.sprite = unit.GetCharacterSprite();

            _allUnit.Add(new UnitRoot(unit, -1, unitBTN));
            int capturedIndex = _allUnit.Count - 1;
            unitBTN.onClick.AddListener(() => OnClickedUnitBTN(capturedIndex));
            Debug.Log(_allUnit.Count - 1);

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
            //ư ̺Ʈ
    public void OnClickedUnitBTN(int unitIdx)
    {
 Debug.Log(" id: " + _allUnit[unitIdx].unit.Info.Id + " index: " + unitIdx);
        selectedUnit = unitIdx;
        RootBTNActivate(true);
    }

            //ư ̺Ʈ
    public void OnClickedItem(int itemIndex)
    {
        if (selectedUnit == -1) return;
        CMDOnClickedRootBTN(selectedUnit, itemIndex);
    }

    // 서버 전용 딕셔너리(_serverSelections)로 선택 추적 — _allUnit은 클라이언트 전용
    [Command(requiresAuthority = false)]
    public void CMDOnClickedRootBTN(int unitIdx, int itemIdx)
    {
        int prevItemIdx = -1;

        // 이전에 선택한 보상이 있으면 취소
        if (_serverSelections.TryGetValue(unitIdx, out int prev))
        {
            prevItemIdx = prev;
            _selectRootNum[prev]--;
            endSelectUnit--;
        }

        _serverSelections[unitIdx] = itemIdx;
        _selectRootNum[itemIdx]++;
        endSelectUnit++;

        Debug.Log($"[RootingSystem] 선택 - unitIdx:{unitIdx}, itemIdx:{itemIdx}, 완료:{endSelectUnit}/{_manager._players.Count}");

        // 클라이언트 UI 업데이트 (prevItemIdx 전달 → 이전 아이콘 제거용)
        RpcRequestSelectRoot(unitIdx, itemIdx, prevItemIdx);

        if (endSelectUnit == _manager._players.Count) EndRooting();
    }

    [ClientRpc]
    void RpcRequestSelectRoot(int unitIdx, int itemIdx, int prevItemIdx)
    {
        // 이전 선택 아이콘 제거 (변경 시)
        if (prevItemIdx != -1 && _allUnit[unitIdx].unitText != null)
        {
            Destroy(_allUnit[unitIdx].unitText.gameObject);
            _allUnit[unitIdx].unitText = null;
        }

        // 새 선택 아이콘 생성
        var id = Instantiate(_idPrefab, _selectTF[itemIdx]);
        _allUnit[unitIdx].unitText = id;
        _allUnit[unitIdx].unitText.GetComponentInChildren<TextMeshProUGUI>().text
            = _allUnit[unitIdx].unit.Info.Name.ToString();

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
        // slotWinner[i] = 슬롯 i의 최종 수령자 unitIdx (-1이면 아무도 없음)
        int[] slotWinner = new int[4];
        for (int i = 0; i < 4; i++) slotWinner[i] = -1;

        for (int i = 0; i < 4; i++)
        {
            if (_selectRootNum[i] == 0) continue; // 아무도 선택 안 한 슬롯

            if (_selectRootNum[i] == 1)
            {
                // 단독 선택 → 바로 수령
                foreach (var kvp in _serverSelections)
                {
                    if (kvp.Value == i) { slotWinner[i] = kvp.Key; break; }
                }
            }
            else if (_currentReward[i].isEquipment)
            {
                // 복수 선택 + 장비 → RPS
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
                    Debug.LogWarning($"[ProcessAllBattles] 슬롯 {i}: 참가자 {RPS.Count}명, RPS 생략");
                    if (RPS.Count == 1) slotWinner[i] = RPS[0];
                    continue;
                }

                yield return StartCoroutine(RockPaperScissors(RPS));

                // RPS 종료 후 RPS 리스트에 남은 한 명이 승자
                if (RPS.Count == 1) slotWinner[i] = RPS[0];
            }
            else
            {
                // 복수 선택 + 골드 → 모두에게 지급 (slotWinner는 사용 안 함, 아래서 별도 처리)
                slotWinner[i] = -2; // -2 = 골드 전체 지급 표시
            }
        }

        // ── 아이템 / 골드 지급 ───────────────────────────────────────────
        AwardRewards(slotWinner);

        Debug.Log("모든 칸 처리 완료. 씬 전환.");
        var roomManager = NetworkManager.singleton as GameRoomManager;
        string homeScene = roomManager != null && !string.IsNullOrEmpty(roomManager.HomeScene)
            ? roomManager.HomeScene
            : "Home";

        NetworkManager.singleton.ServerChangeScene(homeScene);
    }

    /// <summary>
    /// 슬롯별 승자에게 보상을 지급합니다. 서버 전용.
    /// </summary>
    [Server]
    private void AwardRewards(int[] slotWinner)
    {
        for (int i = 0; i < 4; i++)
        {
            RewardInfo reward = _currentReward[i];

            if (slotWinner[i] == -2)
            {
                if (!reward.isEquipment && reward.amount > 0)
                {
                    foreach (var kvp in _serverSelections)
                    {
                        if (kvp.Value == i) 
                        {
                            int unitIdx = kvp.Key;
                            var jointWinner = _manager._players[unitIdx];
                            if (jointWinner == null) continue;

                            // 선택된 캐릭터에게 공동 보상
                            var playerInfo = jointWinner.Info;
                            playerInfo.Gold += reward.amount;
                            jointWinner.Info = playerInfo;

                            jointWinner.FlushInfoToPlayerData();
                            Debug.Log($"[보상 - 중복골드] {jointWinner.Info.Name} ← {reward.amount}G 획득 (공동 수령)");
                        }
                    }
                }
                continue; // 중복 골드 처리 완료
            }

            if (slotWinner[i] < 0) continue; // 수령자 없음

            // 아이템 지급
            int winnerIdx = slotWinner[i];
            if (winnerIdx < 0 || winnerIdx >= _manager._players.Count) continue;

            var winner = _manager._players[winnerIdx];
            if (winner == null) continue;

            if (reward.isEquipment && reward.equipment != null)
            {
                // ItemSO → InventoryItem 변환 후 Info.Items에 추가
                var playerInfo = winner.Info;
                if (playerInfo.Items == null) playerInfo.Items = new List<InventoryItem>();
                playerInfo.Items.Add(reward.equipment.ToInventoryItem(1));
                winner.Info = playerInfo;
                Debug.Log($"[보상] {winner.Info.Name} ← {reward.equipment.EqpItem.Name} 획득");

                // 씬 전환 후에도 살아남는 PlayerData에 즉시 반영
                winner.FlushInfoToPlayerData();
            }
            else if (!reward.isEquipment)
            {
                if (reward.amount >= 0)
                {
                    var playerInfo = winner.Info;
                    playerInfo.Gold += reward.amount;
                    winner.Info = playerInfo;
                    winner.FlushInfoToPlayerData();
                    Debug.LogWarning($"[보상] 슬롯{i} 골드 보상인데 amount={reward.amount} 입니다. Inspector에서 amount를 설정하세요.");
                }
                else
                    Debug.Log($"[보상] 슬롯{i} 골드 {reward.amount} → 골드 지급은 PlayerAccount 연동 필요 (TODO)");
            }
        }
    }
            //
    IEnumerator RockPaperScissors(List<int> RPS)
    {
 Debug.Log(" ");
        RpcStartRPS(true);
        yield return new WaitForSeconds(1.0f);
        RpcStartRPS(false);

        bool isDraw = true;
        while (isDraw) {
 Debug.Log(" ");
            List<int> hands = new List<int>();
            for (int i = 0; i < RPS.Count; i++) hands.Add(UnityEngine.Random.Range(0, 3));

            RpcShowRPS(RPS, hands);
            yield return new WaitForSeconds(1.5f); 

            bool hasRock = hands.Exists(x => x == 0);
            bool hasPaper = hands.Exists(x => x == 1);
            bool hasScissors = hands.Exists(x => x == 2);
            int winner = -1;

            if (hasRock && hasPaper && hasScissors) winner = -1; //ΰ  ٸ
            else if (hasRock && hasScissors) winner = 0;
            else if (hasPaper && hasRock) winner = 1;
            else if (hasScissors && hasPaper) winner = 2;
            else winner = -1;

 if (winner == -1) { Debug.Log("º "); continue; } // ºΰ ý ٽ 

            //4. й (ڿ ؾ ε )
            for (int i = RPS.Count - 1; i >= 0; i--)
            {
                if (hands[i] != winner)
                {
                    int loserIdx = RPS[i];
 Debug.Log($"й Ż: {_allUnit[loserIdx].unit.Info.Id}");

                    RpcHideRPSIcon(loserIdx);
                    RPS.RemoveAt(i);
                }
            }
            if (RPS.Count <= 1)
            {
                isDraw = false;
 Debug.Log("¸: " + _allUnit[RPS[0]].unit.Info.Id);
                //RpcHideRPSIcon(RPS[0]);
            }
        }
    }
    [ClientRpc]
    public void RpcStartRPS(bool isStart)
    {
        RPSStartIMG.gameObject.SetActive(isStart);
    }
            //̹ ֱ
    [ClientRpc]
    public void RpcShowRPS(List<int> RPS, List<int> hands)
    {
 Debug.Log(" ");
        for(int i = 0; i < RPS.Count; i++)
        {
            _allUnit[RPS[i]].RPCIMG.sprite = RPSImage[hands[i]];
            _allUnit[RPS[i]].RPCIMG.gameObject.SetActive(true);
 Debug.Log(" :"+ _allUnit[RPS[i]].unit.Info.Id + " hand: "+ hands[i]);
        }
    }
            //̹ 
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
