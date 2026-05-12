using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 맵 위 영지 영역(RectTransform)에 부착.
/// 호버 시 Image 색을 바꾸고, 클릭 시 정적 이벤트 발행.
/// 같은 GameObject에 alpha 0짜리 투명 Image(Raycast Target On)를 두면
/// 시각적 표시 없이 마우스 입력만 받을 수 있다.
/// </summary>
[RequireComponent(typeof(Image))]
public class DistrictHover : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] private DistrictType districtType;

    [Header("호버 색상 (alpha 포함)")]
    [SerializeField] private Color hoverColor = new Color(1f, 1f, 1f, 0.3f);

    public static event Action<DistrictType> OnDistrictClicked;
    public static event Action<DistrictType> OnDistrictHoverEnter;
    public static event Action OnDistrictHoverExit;

    private Image _image;
    private Color _baseColor;

    void Awake()
    {
        _image = GetComponent<Image>();
        _baseColor = _image.color;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _image.color = hoverColor;
        OnDistrictHoverEnter?.Invoke(districtType);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _image.color = _baseColor;
        OnDistrictHoverExit?.Invoke();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OnDistrictClicked?.Invoke(districtType);
    }
}
