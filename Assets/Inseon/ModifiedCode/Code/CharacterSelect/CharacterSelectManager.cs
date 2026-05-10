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
/// ★ 사용법 (Unity Editor)
///   1. 빈 GameObject에 이 스크립트를 붙입니다.
///   2. 각 캐릭터 버튼 GameObject에 CharacterCard 스크립트를 붙입니다.
///   3. CharacterCard 인스펙터에서 코드·스프라이트·프리팹·스킬을 채웁니다.
///      → 순서는 무관합니다. 코드(C001 등)를 키로 자동 매핑됩니다.
///   4. characterCards 리스트에 씬의 CharacterCard들을 등록합니다 (순서 무관).
///   5. previewImage: 카드 호버 시 캐릭터 이미지가 표시될 큰 Image를 연결합니다.
///   6. 우측 스탯 패널 TMP 텍스트들을 연결합니다.
///   7. confirmButton의 OnClick → OnClickConfirm() 을 연결합니다.
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

    // ── 내 선택 슬롯 (왼쪽 주황 박스) ───────────────
    [Header("내 선택 슬롯")]
    [SerializeField] private List<Image> mySelectedSlots;

    // ─────────────────────────────────────────────────
    // 런타임 상태
    // ─────────────────────────────────────────────────

    /// <summary>code → CharacterCard 빠른 조회용 딕셔너리 (InitCards에서 빌드)</summary>
    private readonly Dictionary<string, CharacterCard> _cardMap = new Dictionary<string, CharacterCard>();

    private readonly List<string> _selectedCodes = new List<string>();
    private int _maxSelect = 1;
    private GameRoomPlayer _localRoomPlayer;

    // ─────────────────────────────────────────────────

    private void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// NetworkClient.localPlayer 참조가 씬 전환 타이밍에 따라 null일 수 있으므로
    /// isLocalPlayer 플래그로 직접 탐색하는 폴백을 함께 사용합니다.
    /// </summary>
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

    /// <summary>
    /// Mirror가 씬 전환 후 GameRoomPlayer를 재스폰할 때까지 최대 5초 대기 후 UI를 초기화합니다.
    /// </summary>
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
            _maxSelect = _localRoomPlayer.CharCount;

        InitCards();
        RefreshConfirmButton();
        ClearStatPanel();
    }

    // ─────────────────────────────────────────────────
    //  카드 초기화
    // ─────────────────────────────────────────────────

    private void InitCards()
    {
        if (characterCards == null) return;

        var player = inseon.Playfab.User.PlayfabUserManage.Player;

        // CharacterRegistry 초기화 (이전 씬 잔여 데이터 제거)
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

            // ── Dictionary 등록 ─────────────────────────────────────────
            _cardMap[code] = card;

            // ── CharacterRegistry 등록 ──────────────────────────────────
            CharacterRegistry.Register(code, new CharacterRegistry.Entry
            {
                PlayerDataPrefab = card.PlayerDataPrefab,
                BattleUnitPrefab = card.BattleUnitPrefab,
                CharacterSprite  = card.BattleSprite,
                Skills           = card.Skills ?? new List<SkillInfo>(),
                Items            = card.Items  ?? new List<ItemInfo>(),
            });

            // ── Mirror 프리팹 등록 ──────────────────────────────────────
            TryRegisterPrefab(card.PlayerDataPrefab);
            TryRegisterPrefab(card.BattleUnitPrefab);

            // ── 소유권 확인 ─────────────────────────────────────────────
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

        Debug.Log($"[CharacterSelectManager] 카드 초기화 완료. 등록 수: {_cardMap.Count}");
    }

    // ─────────────────────────────────────────────────
    //  호버
    // ─────────────────────────────────────────────────

    private void OnCardHoverEnter(string characterCode)
    {
        ShowStatPanel(characterCode);

        if (previewImage != null)
        {
            Sprite icon = GetCardIcon(characterCode);
            previewImage.sprite  = icon != null ? icon : previewDefaultSprite;
            previewImage.enabled = true;
        }
    }

    private void OnCardHoverExit()
    {
        ClearStatPanel();

        if (previewImage != null)
        {
            previewImage.sprite  = previewDefaultSprite;
            previewImage.enabled = previewDefaultSprite != null;
        }
    }

    // ─────────────────────────────────────────────────
    //  클릭 (선택 토글)
    // ─────────────────────────────────────────────────

    private void OnCardClicked(string characterCode)
    {
        if (_selectedCodes.Contains(characterCode))
        {
            _selectedCodes.Remove(characterCode);
            GetCard(characterCode)?.SetSelected(false);
        }
        else
        {
            if (_selectedCodes.Count >= _maxSelect)
            {
                string oldest = _selectedCodes[0];
                _selectedCodes.RemoveAt(0);
                GetCard(oldest)?.SetSelected(false);
            }

            _selectedCodes.Add(characterCode);
            GetCard(characterCode)?.SetSelected(true);
        }

        RefreshMySlots();
        RefreshConfirmButton();
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
    //  내 선택 슬롯 갱신
    // ─────────────────────────────────────────────────

    private void RefreshMySlots()
    {
        if (mySelectedSlots == null) return;

        for (int i = 0; i < mySelectedSlots.Count; i++)
        {
            if (mySelectedSlots[i] == null) continue;

            if (i < _selectedCodes.Count)
            {
                mySelectedSlots[i].sprite  = GetCardIcon(_selectedCodes[i]);
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
    //  확정 버튼
    // ─────────────────────────────────────────────────

    private void RefreshConfirmButton()
    {
        if (confirmButtonText != null)
            confirmButtonText.text = $"선택 완료 ({_selectedCodes.Count}/{_maxSelect})";

        if (confirmButton != null)
            confirmButton.interactable = (_selectedCodes.Count == _maxSelect);
    }

    /// <summary>확정 버튼 OnClick에 연결합니다.</summary>
    public void OnClickConfirm()
    {
        if (_selectedCodes.Count != _maxSelect)
        {
            Debug.LogWarning($"[CharacterSelectManager] 선택 수 불일치 ({_selectedCodes.Count}/{_maxSelect})");
            return;
        }

        if (_localRoomPlayer == null)
            _localRoomPlayer = FindLocalRoomPlayer();

        if (_localRoomPlayer == null)
        {
            Debug.LogError("[CharacterSelectManager] 로컬 GameRoomPlayer를 찾을 수 없습니다.");
            return;
        }

        var localRoomPlayer = _localRoomPlayer;

        foreach (var code in _selectedCodes)
        {
            Debug.Log($"[CharacterSelectManager] CMDChoiceHero({code})");
            localRoomPlayer.CMDChoiceHero(code);
        }

        localRoomPlayer.CmdConfirmSelection();

        if (confirmButton != null)
            confirmButton.interactable = false;

        Debug.Log("[CharacterSelectManager] 캐릭터 선택 확정 완료");
    }

    // ─────────────────────────────────────────────────
    //  유틸
    // ─────────────────────────────────────────────────

    /// <summary>code → CharacterCard O(1) 조회</summary>
    private CharacterCard GetCard(string code)
    {
        _cardMap.TryGetValue(code, out var card);
        return card;
    }

    /// <summary>code에 해당하는 카드 아이콘 스프라이트를 반환합니다.</summary>
    private Sprite GetCardIcon(string code)
    {
        return GetCard(code)?.CardIcon;
    }

    private static void SetText(TextMeshProUGUI label, string value)
    {
        if (label != null) label.text = value;
    }

    /// <summary>
    /// 이미 등록된 프리팹이면 건너뜁니다.
    /// NetworkClient.RegisterPrefab은 중복 호출 시 예외를 던지므로 반드시 이 메서드를 사용하세요.
    /// </summary>
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
