using System;
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

    public string CharacterCode => characterCode;

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
            // Inspector 수동 연결 없이도 클릭이 동작하도록 코드에서 직접 등록
            btn.onClick.RemoveAllListeners();
            if (owned) btn.onClick.AddListener(OnClick);
        }
    }

    public void SetSelected(bool selected)
    {
        _isSelected = selected;
        if (selectedOutline != null)
            selectedOutline.SetActive(selected);
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
