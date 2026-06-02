using System.Collections;
using System.Collections.Generic;
using Jun;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 캐릭터 선택 씬 UI 전체를 관리합니다.
///
/// ── 선택 흐름 (공동 큐 방식) ──────────────────────────────────────────
/// 카드 클릭 → CMDChoiceHero (서버) → GameRoomManager.TrySelectCharacter
///   → GlobalQueue 업데이트 → RpcSyncGlobalQueue → OnGlobalQueueSynced
///   → 모든 클라이언트 UI 갱신 (공유 슬롯 + 카드 잠금 상태)
///
/// 확정 버튼 → CmdConfirmSelection (선택은 이미 큐에 있음)
/// 전원 확정 → OnPlayerConfirmedSelection → HomeScene
/// ─────────────────────────────────────────────────────────────────────
/// </summary>
public class CharacterSelectManager : MonoBehaviour
{
    public static CharacterSelectManager Instance { get; private set; }

    // ── 카드 목록 ────────────────────────────────────
    [Header("캐릭터 카드 (순서 무관 — CharacterCode를 키로 자동 매핑)")]
    [SerializeField] private List<CharacterCard> characterCards;

    // ── 프리뷰 이미지 ─────────────────────────────────
    [Header("프리뷰")]
    [Tooltip("카드에 마우스를 올리면 이 Image에 캐릭터 이미지가 표시됩니다.")]
    [SerializeField] private Image previewImage;

    [Tooltip("아무것도 선택되지 않았을 때 표시할 기본 스프라이트 (선택사항)")]
    [SerializeField] private Sprite previewDefaultSprite;

    // ── 스탯 패널 (우측) ────────────────────────────
    [Header("스탯 패널")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI attackText;
    [SerializeField] private TextMeshProUGUI defenseText;
    [SerializeField] private TextMeshProUGUI accuracyText;
    [SerializeField] private TextMeshProUGUI evasionText;
    [SerializeField] private TextMeshProUGUI speedText;
    [SerializeField] private TextMeshProUGUI criticalText;
    [SerializeField] private TextMeshProUGUI effectResText;
    [SerializeField] private TextMeshProUGUI stunResText;
    [SerializeField] private TextMeshProUGUI stressText;
    [SerializeField] private TextMeshProUGUI levelText;

    // ── 확정 버튼 ────────────────────────────────────
    [Header("확정 버튼")]
    [SerializeField] private Button          confirmButton;
    [SerializeField] private TextMeshProUGUI confirmButtonText;

    // ── 스킬 슬롯 ────────────────────────────────────
    [System.Serializable]
    private class SkillSlotUI
    {
        public Image icon;  // 캐릭터 호버 시 스킬 아이콘 표시용
    }

    [Header("스킬 슬롯 (순서대로 최대 4칸)")]
    [SerializeField] private List<SkillSlotUI> skillSlots;

    [Tooltip("스킬 아이콘에 마우스를 올렸을 때만 표시되는 공유 이름 텍스트")]
    [SerializeField] private TextMeshProUGUI skillNameText;

    [Tooltip("스킬 아이콘에 마우스를 올렸을 때만 표시되는 공유 설명 텍스트")]
    [SerializeField] private TextMeshProUGUI skillDescText;

    // ── 공유 선택 슬롯 ────────────────────────────────
    [Header("공유 선택 슬롯 (전체 큐 순서대로 표시 — 최대 4칸)")]
    [SerializeField] private List<Image> mySelectedSlots;

    // ─────────────────────────────────────────────────
    // 런타임 상태
    // ─────────────────────────────────────────────────

    /// <summary>code → CharacterCard 빠른 조회용 딕셔너리 (InitCards에서 빌드)</summary>
    private readonly Dictionary<string, CharacterCard> _cardMap = new Dictionary<string, CharacterCard>();

    /// <summary>서버에서 수신한 최신 전역 큐 스냅샷</summary>
    private SelectionEntry[] _lastKnownQueue = System.Array.Empty<SelectionEntry>();

    /// <summary>마지막으로 호버한 캐릭터 코드 — HoverExit 후에도 패널 유지용</summary>
    private string _lastHoveredCode = null;

    private int _maxSelect = 1;
    private GameRoomPlayer _localRoomPlayer;

    // ─────────────────────────────────────────────────

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private static GameRoomPlayer FindLocalRoomPlayer()
    {
        var p = NetworkClient.localPlayer?.GetComponent<GameRoomPlayer>();
        if (p != null) return p;

        foreach (var candidate in Object.FindObjectsByType<GameRoomPlayer>(FindObjectsSortMode.None))
            if (candidate.isLocalPlayer) return candidate;

        return null;
    }

    private void Start()
    {
        if (!CharacterDatabase.IsLoaded)
        {
            Debug.LogError("[CharacterSelectManager] CharacterDatabase가 로드되지 않았습니다.");
            return;
        }

        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(OnClickConfirm);
        }

        StartCoroutine(InitAfterNetworkReady());
    }

    private IEnumerator InitAfterNetworkReady()
    {
        float timeout = 5f;

        while (_localRoomPlayer == null && timeout > 0f)
        {
            _localRoomPlayer = FindLocalRoomPlayer();
            if (_localRoomPlayer == null)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }
        }

        if (_localRoomPlayer == null)
            Debug.LogWarning("[CharacterSelectManager] GameRoomPlayer를 찾지 못했습니다. 네트워크 없이 실행합니다.");
        else
        {
            _maxSelect = _localRoomPlayer.CharCount;
            // 현재 큐 상태 요청 (씬 진입 시 서버에서 이미 브로드캐스트했지만, 타이밍 보정용)
            _localRoomPlayer.CmdRequestQueueSync();
        }

        InitCards();
        InitSkillSlots();
        RefreshConfirmButton();
        ClearStatPanel();
        ClearSkillPanel();
    }

    // ─────────────────────────────────────────────────
    //  카드 초기화
    // ─────────────────────────────────────────────────

    private void InitCards()
    {
        if (characterCards == null) return;

        var player = inseon.Playfab.User.PlayfabUserManage.Player;

        CharacterRegistry.Clear();
        _cardMap.Clear();

        foreach (var card in characterCards)
        {
            if (card == null) continue;

            string code = card.CharacterCode;
            if (string.IsNullOrEmpty(code))
            {
                Debug.LogWarning($"[CharacterSelectManager] {card.gameObject.name} 의 CharacterCode가 비어있습니다.");
                continue;
            }

            if (_cardMap.ContainsKey(code))
            {
                Debug.LogWarning($"[CharacterSelectManager] 중복 CharacterCode '{code}' 감지 — {card.gameObject.name} 무시");
                continue;
            }

            _cardMap[code] = card;

            CharacterRegistry.Register(code, new CharacterRegistry.Entry
            {
                PlayerDataPrefab = card.PlayerDataPrefab,
                BattleUnitPrefab = card.BattleUnitPrefab,
                CharacterSprite  = card.BattleSprite,
                Skills           = card.Skills ?? new List<SkillInfo>(),
                Items            = card.Items  ?? new List<ConsumableInfo>(),
                Weapon      = card.Weapon,
                Armor       = card.Armor,
                UniqueTrait = card.UniqueTrait,
            });

            // 캐릭터 카드의 장비 ItemSO를 ItemManager에 미리 등록합니다.
            // ItemManager는 DontDestroyOnLoad이므로 씬이 바뀌어도 등록이 유지됩니다.
            // 이렇게 해야 Home씬 인벤토리에서 장착 해제한 장비의 아이콘을 찾을 수 있습니다.
            Lsy.ItemManager.Instance?.RegisterItemSO(card.WeaponSO);
            Lsy.ItemManager.Instance?.RegisterItemSO(card.ArmorSO);

            TryRegisterPrefab(card.PlayerDataPrefab);
            TryRegisterPrefab(card.BattleUnitPrefab);

            var charInfo    = CharacterDatabase.Get(code);
            string ownerKey = (charInfo.HasValue && !string.IsNullOrEmpty(charInfo.Value.characterName))
                              ? charInfo.Value.characterName
                              : code;
            bool owned = player != null && player.OwnsCharacter(ownerKey);

            string captured = code;
            card.Setup(
                owned,
                card.CardIcon,
                onClick:      () => OnCardClicked(captured),
                onHoverEnter: () => OnCardHoverEnter(captured),
                onHoverExit:  () => OnCardHoverExit()
            );
        }

        // CharacterCard.Items(ConsumableInfo 직접 할당) 소모품 아이콘을 ItemManager에 등록
        Lsy.ItemManager.Instance?.RefreshFromCharacterRegistry();

        Debug.Log($"[CharacterSelectManager] 카드 초기화 완료. 등록 수: {_cardMap.Count}");
    }

    // ─────────────────────────────────────────────────
    //  호버
    // ─────────────────────────────────────────────────

    private void OnCardHoverEnter(string characterCode)
    {
        _lastHoveredCode = characterCode;
        ShowStatPanel(characterCode);
        ShowSkillPanel(characterCode);

        if (previewImage != null)
        {
            Sprite preview = GetCard(characterCode)?.PreviewSprite;
            previewImage.sprite = preview != null ? preview : previewDefaultSprite;
            previewImage.enabled = true;
        }
    }

    // 의도적으로 비워둡니다 — 새 카드에 올라가기 전까지 마지막 패널 상태를 유지합니다.
    private void OnCardHoverExit() { }

    // ─────────────────────────────────────────────────
    //  클릭 (실시간 선택 — 확정 버튼과 분리)
    // ─────────────────────────────────────────────────

    private void OnCardClicked(string characterCode)
    {
        if (_localRoomPlayer == null) return;

        // 현재 내 선택 상태 파악
        bool   isMySelection = false;
        string oldestMyCode  = null;
        int    myCount       = 0;

        foreach (var e in _lastKnownQueue)
        {
            if (e.ownerNetId != _localRoomPlayer.netId) continue;
            myCount++;
            if (oldestMyCode == null) oldestMyCode = e.heroCode;
            if (e.heroCode == characterCode) isMySelection = true;
        }

        if (isMySelection)
        {
            // 이미 선택한 카드 → 해제 (낙관적 UI)
            GetCard(characterCode)?.SetSelected(false);
            _localRoomPlayer.CMDChoiceHero(characterCode);
        }
        else
        {
            // 한도 초과 시 가장 오래된 선택부터 해제
            if (myCount >= _maxSelect && oldestMyCode != null)
            {
                GetCard(oldestMyCode)?.SetSelected(false);
                _localRoomPlayer.CMDChoiceHero(oldestMyCode);
            }

            // 새 카드 선택 (낙관적 UI)
            GetCard(characterCode)?.SetSelected(true);
            _localRoomPlayer.CMDChoiceHero(characterCode);
        }
    }

    // ─────────────────────────────────────────────────
    //  전역 큐 수신 (RpcSyncGlobalQueue → 여기로)
    // ─────────────────────────────────────────────────

    public void OnGlobalQueueSynced(SelectionEntry[] queue)
    {
        _lastKnownQueue = queue ?? System.Array.Empty<SelectionEntry>();
        RefreshSharedSlots();
        RefreshCardStates();
        RefreshConfirmButton();
    }

    // ─────────────────────────────────────────────────
    //  공유 슬롯 갱신 (전역 큐 순서대로 표시)
    // ─────────────────────────────────────────────────

    private void RefreshSharedSlots()
    {
        if (mySelectedSlots == null) return;

        for (int i = 0; i < mySelectedSlots.Count; i++)
        {
            if (mySelectedSlots[i] == null) continue;

            if (i < _lastKnownQueue.Length)
            {
                mySelectedSlots[i].sprite  = GetCardIcon(_lastKnownQueue[i].heroCode);
                mySelectedSlots[i].enabled = true;
            }
            else
            {
                mySelectedSlots[i].sprite  = null;
                mySelectedSlots[i].enabled = false;
            }
        }
    }

    // ─────────────────────────────────────────────────
    //  카드 상태 갱신 (내 선택 하이라이트 + 타인 선택 잠금)
    // ─────────────────────────────────────────────────

    private void RefreshCardStates()
    {
        if (_localRoomPlayer == null) return;

        foreach (var pair in _cardMap)
        {
            bool isMySelection = false;
            bool isTaken       = false;

            foreach (var e in _lastKnownQueue)
            {
                if (e.heroCode != pair.Key) continue;
                if (e.ownerNetId == _localRoomPlayer.netId) isMySelection = true;
                else                                        isTaken       = true;
                break;
            }

            pair.Value.SetSelected(isMySelection);
            pair.Value.SetTaken(isTaken);
        }
    }

    // ─────────────────────────────────────────────────
    //  스탯 패널
    // ─────────────────────────────────────────────────

    private void ShowStatPanel(string characterCode)
    {
        var info = CharacterDatabase.Get(characterCode);
        if (!info.HasValue) return;

        Character c = info.Value;
        SetText(nameText,      string.IsNullOrEmpty(c.characterName) ? c.characterCode : c.characterName);
        SetText(levelText,     c.level.ToString());
        SetText(hpText,        c.hp.ToString());
        SetText(attackText,    c.attack.ToString());
        SetText(defenseText,   c.defense.ToString());
        SetText(accuracyText,  c.accuracy.ToString());
        SetText(evasionText,   c.evasion.ToString());
        SetText(speedText,     c.speed.ToString());
        SetText(criticalText,  c.critical.ToString());
        SetText(effectResText, c.effectResistance.ToString());
        SetText(stunResText,   c.stunResistance.ToString());
        SetText(stressText,    c.stress.ToString());
    }

    private void ClearStatPanel()
    {
        string dash = "-";
        SetText(nameText, dash);      SetText(levelText, dash);    SetText(hpText, dash);
        SetText(attackText, dash);    SetText(defenseText, dash);  SetText(accuracyText, dash);
        SetText(evasionText, dash);   SetText(speedText, dash);    SetText(criticalText, dash);
        SetText(effectResText, dash); SetText(stunResText, dash);  SetText(stressText, dash);
    }

    // ─────────────────────────────────────────────────
    //  스킬 슬롯
    // ─────────────────────────────────────────────────

    /// <summary>씬 초기화 시 각 슬롯 아이콘에 SkillSlotHover를 붙입니다.</summary>
    private void InitSkillSlots()
    {
        if (skillSlots == null) return;

        foreach (var slot in skillSlots)
        {
            if (slot?.icon == null) continue;
            // 이미 붙어있으면 재사용, 없으면 새로 추가
            if (slot.icon.GetComponent<SkillSlotHover>() == null)
                slot.icon.gameObject.AddComponent<SkillSlotHover>();
        }
    }

    /// <summary>캐릭터 호버 시 각 슬롯 아이콘을 갱신하고 SkillSlotHover에 스킬 정보를 전달합니다.</summary>
    private void ShowSkillPanel(string characterCode)
    {
        if (skillSlots == null) return;

        var card      = GetCard(characterCode);
        var skillList = card?.Skills;

        for (int i = 0; i < skillSlots.Count; i++)
        {
            var slot = skillSlots[i];
            if (slot?.icon == null) continue;

            bool     hasSkill = skillList != null && i < skillList.Count;
            SkillInfo skill   = hasSkill ? skillList[i] : null;

            // 아이콘 이미지 갱신
            slot.icon.sprite  = hasSkill && skill.icon != null ? skill.icon : null;
            slot.icon.enabled = hasSkill && skill.icon != null;

            // hover 컴포넌트에 스킬 정보 전달
            slot.icon.GetComponent<SkillSlotHover>()?.SetSkill(skill);
        }

        // 캐릭터가 바뀌면 텍스트는 초기화
        SetText(skillNameText, "");
        SetText(skillDescText,  "");
    }

    private void ClearSkillPanel()
    {
        if (skillSlots == null) return;

        foreach (var slot in skillSlots)
        {
            if (slot?.icon == null) continue;
            slot.icon.sprite  = null;
            slot.icon.enabled = false;
            slot.icon.GetComponent<SkillSlotHover>()?.SetSkill(null);
        }

        SetText(skillNameText, "");
        SetText(skillDescText,  "");
    }

    /// <summary>SkillSlotHover에서 호출 — 스킬 이름과 설명을 공유 텍스트에 표시합니다.</summary>
    public void OnSkillHoverEnter(SkillInfo skill)
    {
        SetText(skillNameText, skill.Name        ?? "");
        SetText(skillDescText,  skill.description ?? "");
    }

    /// <summary>SkillSlotHover에서 호출 — 공유 텍스트를 비웁니다.</summary>
    public void OnSkillHoverExit()
    {
        SetText(skillNameText, "");
        SetText(skillDescText,  "");
    }

    // ─────────────────────────────────────────────────
    //  확정 버튼
    // ─────────────────────────────────────────────────

    private void RefreshConfirmButton()
    {
        int myCount = CountMySelections();

        if (confirmButtonText != null)
            confirmButtonText.text = $"선택 완료 ({myCount}/{_maxSelect})";

        if (confirmButton != null)
            confirmButton.interactable = (myCount == _maxSelect);
    }

    /// <summary>확정 버튼 OnClick에 연결합니다.</summary>
    public void OnClickConfirm()
    {
        if (_localRoomPlayer == null)
            _localRoomPlayer = FindLocalRoomPlayer();

        if (_localRoomPlayer == null)
        {
            Debug.LogError("[CharacterSelectManager] 로컬 GameRoomPlayer를 찾을 수 없습니다.");
            return;
        }

        int myCount = CountMySelections();
        if (myCount != _maxSelect)
        {
            Debug.LogWarning($"[CharacterSelectManager] 선택 수 불일치 ({myCount}/{_maxSelect})");
            return;
        }
        
        List<string> myCodes = new();

        foreach (var e in _lastKnownQueue)
        {
            if (e.ownerNetId == _localRoomPlayer.netId)
                myCodes.Add(e.heroCode);
        }

        _localRoomPlayer.CmdApplyConfirmedSelection(myCodes.ToArray());
        _localRoomPlayer.CmdConfirmSelection();

        if (confirmButton != null)
            confirmButton.interactable = false;

        Debug.Log("[CharacterSelectManager] 캐릭터 선택 확정 완료");
    }

    // ─────────────────────────────────────────────────
    //  유틸
    // ─────────────────────────────────────────────────

    private int CountMySelections()
    {
        if (_localRoomPlayer == null) return 0;
        int count = 0;
        foreach (var e in _lastKnownQueue)
            if (e.ownerNetId == _localRoomPlayer.netId) count++;
        return count;
    }

    private CharacterCard GetCard(string code)
    {
        _cardMap.TryGetValue(code, out var card);
        return card;
    }

    private Sprite GetCardIcon(string code)
    {
        return GetCard(code)?.CardIcon;
    }

    private static void SetText(TextMeshProUGUI label, string value)
    {
        if (label != null) label.text = value;
    }

    private static void TryRegisterPrefab(GameObject prefab)
    {
        if (prefab == null) return;
        var identity = prefab.GetComponent<NetworkIdentity>();
        if (identity == null)
        {
            Debug.LogError($"[CharacterSelectManager] {prefab.name} 에 NetworkIdentity가 없습니다! Mirror 스폰 불가.");
            return;
        }
        if (NetworkClient.prefabs.ContainsKey(identity.assetId))
            return;

        NetworkClient.RegisterPrefab(prefab);
    }
}
