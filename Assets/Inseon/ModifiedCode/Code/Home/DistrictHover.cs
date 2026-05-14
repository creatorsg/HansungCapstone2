using System;
using Jun;
using Mirror;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 맵 위 영지 영역(RectTransform)에 부착.
/// 호버 시 Image 색을 바꾸고, 클릭 시 정적 이벤트 발행.
/// 클릭 시 GameRoomManager.GameplayScene을 battleSceneName으로 자동 갱신하므로
/// ReadyOrStartButton은 별도 씬 이름 설정 없이 항상 올바른 씬으로 전환됩니다.
/// </summary>
[RequireComponent(typeof(Image))]
public class DistrictHover : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] private DistrictType districtType;

    [Header("이 영지에 대응하는 전투 씬 이름 (Build Settings의 씬 이름과 동일하게)")]
    [SerializeField] private string battleSceneName;

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
        // GameRoomManager.GameplayScene을 이 영지의 씬으로 즉시 갱신.
        // 이후 ReadyOrStartButton(호스트) 또는 QuestInfoPanel의 Start 버튼이
        // GameplayScene을 참조해 올바른 씬으로 전환합니다.
        if (!string.IsNullOrEmpty(battleSceneName) && NetworkServer.active)
        {
            var rm = NetworkManager.singleton as GameRoomManager;
            if (rm != null)
                rm.GameplayScene = battleSceneName;
        }

        OnDistrictClicked?.Invoke(districtType);
    }
}
