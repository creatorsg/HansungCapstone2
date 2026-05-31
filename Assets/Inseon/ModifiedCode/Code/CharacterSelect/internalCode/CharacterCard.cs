using System;
using System.Collections.Generic;
using Jun;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 캐릭터 선택 그리드에 배치되는 개별 카드 컴포넌트.
///
/// ★ 사용법 (Unity Editor)
///   1. 캐릭터 버튼 오브젝트에 이 스크립트를 추가합니다.
///   2. Inspector에서 CharacterCode 를 "C001", "C002" 등으로 입력합니다.
///   3. Button 컴포넌트의 OnClick → 이 스크립트의 OnClick() 을 연결합니다.
///   4. 나머지 UI 참조를 연결합니다.
///   5. CharacterSelectManager가 Start()에서 자동으로 Setup()을 호출합니다.
/// </summary>
public class CharacterCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("캐릭터 식별")]
    [Tooltip("PlayFab Catalog의 ItemId와 동일하게 입력하세요. 예: C001")]
    [SerializeField] private string characterCode;

    [Header("스프라이트")]
    [Tooltip("선택 화면 카드 아이콘 및 프리뷰에 표시될 스프라이트")]
    [SerializeField] private Sprite cardIcon;
    [SerializeField] private Sprite previewSprite;

    [Tooltip("배틀 씬에서 유닛에 적용할 스프라이트 (cardIcon과 다른 이미지를 쓰고 싶을 때만 채우세요. 비워두면 cardIcon을 사용합니다.)")]
    [SerializeField] private Sprite battleSprite;

    [Header("네트워크 프리팹")]
    [Tooltip("PlayerData NetworkBehaviour 컴포넌트가 붙은 프리팹을 연결하세요.")]
    [SerializeField] private GameObject playerDataPrefab;

    [Tooltip("배틀 씬의 GamePlayerController 유닛 프리팹을 연결하세요.")]
    [SerializeField] private GameObject battleUnitPrefab;

    [Header("스킬 & 아이템")]
    [SerializeField] private List<SkillInfo> skills = new List<SkillInfo>();
    [SerializeField] private List<ConsumableInfo>  items  = new List<ConsumableInfo>();

    [Header("장비 (ItemSO 기반 — 아이콘 자동 연결)")]
    [Tooltip("무기 ItemSO를 연결하면 ItemManager가 씬을 넘어도 아이콘을 찾을 수 있습니다.")]
    [SerializeField] private ItemSO weaponSO;
    [Tooltip("방어구 ItemSO를 연결하면 ItemManager가 씬을 넘어도 아이콘을 찾을 수 있습니다.")]
    [SerializeField] private ItemSO armorSO;

    [Header("고유 특성")]
    [Tooltip("이 캐릭터 고유의 강화 가능한 특성. 기본적으로 1단계 스탯이 항상 적용됩니다.")]
    [SerializeField] private UniqueTraitSO uniqueTrait;

    [Header("UI 참조")]
    [SerializeField] private Image           characterImage;
    [SerializeField] private TextMeshProUGUI codeLabel;
    [SerializeField] private GameObject      lockOverlay;
    [SerializeField] private GameObject      selectedOutline;
    [SerializeField] private Sprite          defaultSprite;

    // ─────────────────────────────────────────────
    private bool   _owned;
    private bool   _isSelected;
    private Action _onClick;
    private Action _onHoverEnter;   // 마우스 올렸을 때
    private Action _onHoverExit;    // 마우스 뗐을 때

    public string        CharacterCode    => characterCode;
    public Sprite        CardIcon         => cardIcon;
    public Sprite        PreviewSprite    => previewSprite;
    /// <summary>배틀 씬 전용 스프라이트. 비어있으면 CardIcon을 대신 사용합니다.</summary>
    public Sprite        BattleSprite     => battleSprite != null ? battleSprite : cardIcon;
    public GameObject    PlayerDataPrefab => playerDataPrefab;
    public GameObject    BattleUnitPrefab => battleUnitPrefab;
    public List<SkillInfo> Skills         => skills;
    public List<ConsumableInfo>  Items    => items;

    /// <summary>고유 특성 SO</summary>
    public UniqueTraitSO UniqueTrait => uniqueTrait;

    /// <summary>무기 ItemSO (ItemManager 아이콘 조회용)</summary>
    public ItemSO WeaponSO => weaponSO;
    /// <summary>방어구 ItemSO (ItemManager 아이콘 조회용)</summary>
    public ItemSO ArmorSO  => armorSO;

    /// <summary>무기 EqpInfo — ItemSO가 있으면 ToEqpInfo(), 없으면 null 반환합니다.</summary>
    public EqpInfo Weapon => weaponSO != null ? weaponSO.ToEqpInfo() : null;
    /// <summary>방어구 EqpInfo — ItemSO가 있으면 ToEqpInfo(), 없으면 null 반환합니다.</summary>
    public EqpInfo Armor  => armorSO  != null ? armorSO.ToEqpInfo()  : null;

    // ─────────────────────────────────────────────

    /// <summary>
    /// CharacterSelectManager.Start()에서 자동 호출됩니다.
    /// </summary>
    public void Setup(bool owned, Sprite icon, Action onClick, Action onHoverEnter, Action onHoverExit = null)
    {
        _owned        = owned;
        _onClick      = onClick;
        _onHoverEnter = onHoverEnter;
        _onHoverExit  = onHoverExit;
        _isSelected   = false;

        if (characterImage != null)
            characterImage.sprite = (icon != null) ? icon : defaultSprite;

        if (codeLabel != null)
            codeLabel.text = characterCode;

        if (lockOverlay != null)
            lockOverlay.SetActive(!owned);

        if (selectedOutline != null)
            selectedOutline.SetActive(false);

        var btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.interactable = owned;
            btn.onClick.RemoveAllListeners();
            if (owned) btn.onClick.AddListener(OnClick);
        }
    }

    public bool IsOwned => _owned;

    public void SetSelected(bool selected)
    {
        _isSelected = selected;
        if (selectedOutline != null)
            selectedOutline.SetActive(selected);
    }

    /// <summary>
    /// 다른 플레이어가 이미 선택한 카드임을 표시합니다.
    /// taken=true면 버튼 비활성화 (내 소유 여부와 무관).
    /// </summary>
    public void SetTaken(bool taken)
    {
        var btn = GetComponent<Button>();
        if (btn != null) btn.interactable = _owned && !taken;
    }

    // ── 클릭 ────────────────────────────────────
    public void OnClick()
    {
        if (!_owned) return;
        _onClick?.Invoke();
    }

    // ── 호버 ────────────────────────────────────
    public void OnPointerEnter(PointerEventData eventData)
    {
        _onHoverEnter?.Invoke();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _onHoverExit?.Invoke();
    }
}
