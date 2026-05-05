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
///   2. 각 캐릭터 버튼에 CharacterCard 스크립트를 붙이고 CharacterCode를 입력합니다.
///   3. characterCards 리스트에 씬의 CharacterCard들을 순서대로 등록합니다.
///   4. previewImage : 카드 호버 시 캐릭터 이미지가 표시될 큰 Image
///   5. 우측 스탯 패널 TMP 텍스트들을 연결합니다.
///   6. confirmButton의 OnClick → OnClickConfirm() 을 연결합니다.
/// </summary>
public class CharacterSelectManager : MonoBehaviour
{
    public static CharacterSelectManager Instance { get; private set; }

    // ── 카드 목록 ────────────────────────────────────
    [Header("캐릭터 카드 (씬에 배치된 순서대로 등록)")]
    [SerializeField] private List<CharacterCard> characterCards;

    // ── 아이콘 / 이미지 ──────────────────────────────
    [Header("캐릭터 아이콘 & 프리뷰")]
    [Tooltip("CharacterCode(C001, C002...) 와 동일한 순서로 스프라이트를 등록하세요.")]
    [SerializeField] private List<Sprite> characterIcons;

    [Tooltip("카드에 마우스를 올리면 이 Image에 캐릭터 이미지가 표시됩니다.")]
    [SerializeField] private Image previewImage;             // 가운데 기다란 흰 박스

    [Tooltip("previewImage에 아무것도 없을 때 표시할 기본 스프라이트 (선택사항)")]
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

    private readonly List<string> _selectedCodes = new List<string>();
    private int _maxSelect = 1;

    // ─────────────────────────────────────────────────

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (!CharacterDatabase.IsLoaded)
        {
            Debug.LogError("[CharacterSelectManager] CharacterDatabase가 로드되지 않았습니다.");
            return;
        }

        var localRoomPlayer = NetworkClient.localPlayer?.GetComponent<GameRoomPlayer>();
        if (localRoomPlayer != null)
            _maxSelect = localRoomPlayer.CharCount;

        // Inspector 수동 연결 없이도 동작하도록 코드에서 직접 등록
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(OnClickConfirm);
        }

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

        foreach (var card in characterCards)
        {
            if (card == null) continue;

            string code = card.CharacterCode;
            if (string.IsNullOrEmpty(code))
            {
                Debug.LogWarning($"[CharacterSelectManager] {card.gameObject.name} 의 CharacterCode가 비어있습니다.");
                continue;
            }

            // CharacterDatabase(Catalog ItemId 기준)와 _ownedCharacters(PlayFab UserData 이름 기준)가
            // 다른 키를 사용하므로, DB에서 characterName을 꺼내 소유권 조회에 사용합니다.
            var charInfo     = CharacterDatabase.Get(code);
            string ownerKey  = (charInfo.HasValue && !string.IsNullOrEmpty(charInfo.Value.characterName))
                               ? charInfo.Value.characterName
                               : code;
            bool owned = player != null && player.OwnsCharacter(ownerKey);

            Sprite icon = GetIcon(code);

            string captured = code;
            card.Setup(
                owned,
                icon,
                onClick:       () => OnCardClicked(captured),
                onHoverEnter:  () => OnCardHoverEnter(captured),
                onHoverExit:   () => OnCardHoverExit()
            );
        }
    }

    // ─────────────────────────────────────────────────
    //  호버
    // ─────────────────────────────────────────────────

    private void OnCardHoverEnter(string characterCode)
    {
        // 스탯 패널 갱신
        ShowStatPanel(characterCode);

        // 기다란 프리뷰 이미지 갱신
        if (previewImage != null)
        {
            Sprite icon = GetIcon(characterCode);
            previewImage.sprite  = (icon != null) ? icon : previewDefaultSprite;
            previewImage.enabled = true;
        }
    }

    private void OnCardHoverExit()
    {
        // 마우스가 카드에서 벗어나면 초기화
        ClearStatPanel();

        if (previewImage != null)
        {
            previewImage.sprite  = previewDefaultSprite;
            previewImage.enabled = (previewDefaultSprite != null);
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
            FindCard(characterCode)?.SetSelected(false);
        }
        else
        {
            if (_selectedCodes.Count >= _maxSelect)
            {
                string oldest = _selectedCodes[0];
                _selectedCodes.RemoveAt(0);
                FindCard(oldest)?.SetSelected(false);
            }

            _selectedCodes.Add(characterCode);
            FindCard(characterCode)?.SetSelected(true);
        }

        RefreshMySlots();
        RefreshConfirmButton();
    }

    private CharacterCard FindCard(string code)
    {
        if (characterCards == null) return null;
        foreach (var card in characterCards)
            if (card != null && card.CharacterCode == code) return card;
        return null;
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
        SetText(nameText, dash);  SetText(levelText, dash);    SetText(hpText, dash);
        SetText(attackText, dash); SetText(defenseText, dash); SetText(accuracyText, dash);
        SetText(evasionText, dash); SetText(speedText, dash);  SetText(criticalText, dash);
        SetText(effectResText, dash); SetText(stunResText, dash); SetText(stressText, dash);
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
                Sprite icon = GetIcon(_selectedCodes[i]);
                mySelectedSlots[i].sprite  = icon;
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

        var localRoomPlayer = NetworkClient.localPlayer?.GetComponent<GameRoomPlayer>();
        if (localRoomPlayer == null)
        {
            Debug.LogError("[CharacterSelectManager] 로컬 GameRoomPlayer를 찾을 수 없습니다.");
            return;
        }

        foreach (var code in _selectedCodes)
        {
            if (CharacterDatabase.Stats.TryGetValue(code, out var charData))
            {
                Debug.Log($"[CharacterSelectManager] CMDChoiceHero({charData.index}) - {code}");
                localRoomPlayer.CMDChoiceHero(charData.index);
            }
        }

        if (confirmButton != null)
            confirmButton.interactable = false;

        Debug.Log("[CharacterSelectManager] 캐릭터 선택 확정 완료");
    }

    // ─────────────────────────────────────────────────
    //  유틸
    // ─────────────────────────────────────────────────

    private Sprite GetIcon(string characterCode)
    {
        if (characterIcons == null) return null;
        if (!CharacterDatabase.Stats.TryGetValue(characterCode, out var c)) return null;
        if (c.index < 0 || c.index >= characterIcons.Count) return null;
        return characterIcons[c.index];
    }

    private static void SetText(TextMeshProUGUI label, string value)
    {
        if (label != null) label.text = value;
    }
}
