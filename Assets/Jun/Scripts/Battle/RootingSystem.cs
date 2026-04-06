using JetBrains.Annotations;
using Jun;
using Mirror;
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
        public int rootId; //������ ���� Id
        public Image unitText; // ������ ���� �Ʒ� ��Ÿ���� �̹���(�ؽ�Ʈ)
        public Button unitBTN; //�ڽ��� unit��ư
        public Image RPCIMG; //���������� �̹���
        public UnitRoot() { }
        public UnitRoot(GamePlayerController unit, int rootId, Button unitBTN)
        {
            this.unit = unit;
            this.rootId = rootId;
            this.unitBTN = unitBTN;
            Transform child = unitBTN.transform.Find("RPSIcon");

            if (child != null)
            {
                // 2. �ڽ��� ã�Ҵٸ�, �� �ȿ� �ִ� Image ������Ʈ�� RPCIMG�� �־��ݴϴ�.
                this.RPCIMG = child.GetComponent<Image>();

                // 3. ���� RPCIMG�� �� �̻� Null�� �ƴϹǷ�, ������ ������ ������ �˴ϴ�!
                this.RPCIMG.gameObject.SetActive(false);
            }
        }
    }
    [System.Serializable]
    public struct RewardInfo
    {
        public string name;      // ���� �̸�
        public Sprite icon;      // ������ �̹���
        public bool isEquipment; // ��񿩺� (true�� ����������, false�� ��ȭ)
        public int amount;       // ��ȭ�� ��� �ݾ�
    }

    [SerializeField] private BattleManager _manager;
    [SerializeField] private GameObject _panel;    //root�г�
    [SerializeField] private List<Image> _rootIMG;  //���� �̹���
    [SerializeField] private List<Button> _rootBTN;    //���� ���� ��ư

    [SerializeField] private Transform _unitTF;    //ĳ���� ���� ��ư ��ġ
    [SerializeField] private List<UnitRoot> _allUnit = new List<UnitRoot>();// Ŭ��� ���� ��ΰ� ������ �ִ� ����� ���� ����Ʈ
    [SerializeField] private Button _unitPrefab;   //���� ��ư ������
    [SerializeField] private Image _idPrefab;   // ���� ���� �� ��Ÿ���� ���� id
    [SerializeField] private List<Transform> _selectTF;  //���� ���� �� ��Ÿ���� ���� id�� ��ġ
    [SerializeField] private int[] _selectRootNum = new int[4]; // �� ���� �� ������ ������ ��
    [SerializeField] private int endSelectUnit = 0;  

    [SyncVar(hook = nameof(StartRooting))]
    public bool isEndStage = false;
    public int selectedUnit = -1;
    [Header("���������� ������")]
    [SerializeField] private List<int> RPS;
    [SerializeField] private List<Sprite> RPSImage; // ���������� �̹���
    [SerializeField] private Image RPSStartIMG;

    [Header("���� ������")]
    [SerializeField] private List<RewardInfo> _rootDatas; // ��� ���� ������
    [SerializeField] private List<RewardInfo> _currentReward; //���� ���� ������
    readonly SyncList<int> _currentRewardIndices = new SyncList<int>();
    private void Start() { _panel.SetActive(false); }

    // �����ʿ��� ���������� �����ٸ� ���� ��
    [Server]
    public void ServerEndStage() 
    {
        _currentRewardIndices.Clear();
        for (int i = 0; i < 4; i++)
        {
            int rand = UnityEngine.Random.Range(0, _rootDatas.Count);
            _currentRewardIndices.Add(rand); // SyncList�� �ִ� ���� ��� Ŭ�� ���޵�
        }
        isEndStage = true;
    }

    public void StartRooting(bool oldVal, bool newVal)
    {
        if (newVal)
        {
            _panel.SetActive(true);
            // �г��� �����鼭 OnEnable�� ����� ���Դϴ�.
            InitRooting();
        }
    }
    // ���ýý��� �ʱ� ������

    public void InitRooting()
    {
        Debug.Log("PanelOnEnable");
        // �ߺ� ���� ������ ���� ����Ʈ�� �ڽ� ������Ʈ �ʱ�ȭ
        foreach (Transform child in _unitTF)
            Destroy(child.gameObject);
        _allUnit.Clear();
        endSelectUnit = 0;

        // ������� �� ���̰� ����
        for (int i = 0; i < _currentRewardIndices.Count; i++)
        {
            int dataIdx = _currentRewardIndices[i];
            _currentReward.Add(_rootDatas[dataIdx]);
            _rootIMG[i].sprite = _rootDatas[dataIdx].icon;
        }
        // ��� ���ֵ��� ���ư��� ���� ��ư ���� �� ����
        foreach (var unit in _manager._players)
        {
            var unitBTN = Instantiate(_unitPrefab, _unitTF);
            unitBTN.image.sprite = unit.GetComponent<SpriteRenderer>().sprite;

            _allUnit.Add(new UnitRoot(unit, -1, unitBTN));
            int capturedIndex = _allUnit.Count - 1;
            unitBTN.onClick.AddListener(() => OnClickedUnitBTN(capturedIndex));
            Debug.Log(_allUnit.Count - 1);

            //�� �����϶��� Ȱ��ȭ �� Ȱ��ȭ �� �������� ���
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
    // ���� ��ư�� ������ �� ������ �̺�Ʈ
    public void OnClickedUnitBTN(int unitIdx)
    {
        Debug.Log("������ ������ id: " + _allUnit[unitIdx].unit.Info.Id + " ������ ������ index: " + unitIdx);
        selectedUnit = unitIdx;
        RootBTNActivate(true);
    }

    // ������ ��ư�� ������ �� ������ �̺�Ʈ
    public void OnClickedItem(int itemIndex)
    {
        if (selectedUnit == -1) return;
        CMDOnClickedRootBTN(selectedUnit, itemIndex);
    }

    //�������� ���õ� ���ְ� ���������� ������Ʈ ��û
    [Command(requiresAuthority = false)]
    public void CMDOnClickedRootBTN(int unitIdx, int itemIdx)
    {
        if (_allUnit[unitIdx].rootId != -1) { Destroy(_allUnit[unitIdx].unitText.gameObject); endSelectUnit--; _selectRootNum[itemIdx]--; }

        _allUnit[unitIdx].rootId = itemIdx;
        endSelectUnit++;
        _selectRootNum[itemIdx]++;

        Debug.Log($"���� ���� - ����:{_allUnit[unitIdx].unit.Info.Id}, ������:{itemIdx}");

        // ��� Ŭ���̾�Ʈ�� UI�� ������Ʈ�ϵ��� RPC ȣ��
        RpcRequestSelectRoot(unitIdx, itemIdx);

        if (endSelectUnit == _allUnit.Count) EndRooting();
    }

    // ��� Ŭ���̾�Ʈ���� �ش� ���� ���� ���� UI ����
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
                RPS.Clear(); //���������� ������
                for (int j = 0; j < _allUnit.Count; j++)
                {
                    if (_allUnit[j].rootId == i)
                    {
                        RPS.Add(j);
                        Debug.Log(_allUnit[j].unit.Info.Id);
                    }
                }
                yield return StartCoroutine(RockPaperScissors(RPS)); // �� �Լ��� ���������� ����
            }
        }

        Debug.Log("��� ĭ�� ���������� �Ϸ�.");
        // ���⼭ ���� �ܰ� �����
        // ������ ����� �����ϱ� ������
    }
    //���������� ����
    IEnumerator RockPaperScissors(List<int> RPS)
    {
        Debug.Log("���������� ����");
        RpcStartRPS(true);
        yield return new WaitForSeconds(1.0f);
        RpcStartRPS(false);

        bool isDraw = true;
        while (isDraw) {
            Debug.Log("���������� ��");
            List<int> hands = new List<int>();
            for (int i = 0; i < RPS.Count; i++) hands.Add(UnityEngine.Random.Range(0, 3));

            RpcShowRPS(RPS, hands);
            yield return new WaitForSeconds(1.5f); 

            bool hasRock = hands.Exists(x => x == 0);
            bool hasPaper = hands.Exists(x => x == 1);
            bool hasScissors = hands.Exists(x => x == 2);
            int winner = -1;

            if (hasRock && hasPaper && hasScissors) winner = -1; //��ΰ� �� �ٸ���
            else if (hasRock && hasScissors) winner = 0;
            else if (hasPaper && hasRock) winner = 1;
            else if (hasScissors && hasPaper) winner = 2;
            else winner = -1;

            if (winner == -1) { Debug.Log("���º� ����"); continue; } // ���ºΰ� ���ý� �ٽ� ����

            // 4. �й��� ���� (�ڿ������� �����ؾ� �ε����� �� ����)
            for (int i = RPS.Count - 1; i >= 0; i--)
            {
                if (hands[i] != winner)
                {
                    int loserIdx = RPS[i];
                    Debug.Log($"�й��� Ż��: {_allUnit[loserIdx].unit.Info.Id}");

                    RpcHideRPSIcon(loserIdx);
                    RPS.RemoveAt(i);
                }
            }
            if (RPS.Count <= 1)
            {
                isDraw = false;
                Debug.Log("�¸���: " + _allUnit[RPS[0]].unit.Info.Id);
                //RpcHideRPSIcon(RPS[0]);
            }
        }
    }
    [ClientRpc]
    public void RpcStartRPS(bool isStart)
    {
        RPSStartIMG.gameObject.SetActive(isStart);
    }
    // ���������� �̹��� �����ֱ�
    [ClientRpc]
    public void RpcShowRPS(List<int> RPS, List<int> hands)
    {
        Debug.Log("���������� ���");
        for(int i = 0; i < RPS.Count; i++)
        {
            _allUnit[RPS[i]].RPCIMG.sprite = RPSImage[hands[i]];
            _allUnit[RPS[i]].RPCIMG.gameObject.SetActive(true);
            Debug.Log("���������� ���:"+ _allUnit[RPS[i]].unit.Info.Id + " hand: "+ hands[i]);
        }
    }
    // ���������� �̹��� ����� ����
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