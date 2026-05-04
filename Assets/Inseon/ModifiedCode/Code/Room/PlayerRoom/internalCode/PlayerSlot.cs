using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerSlot : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI playerDisplayName;
    [SerializeField] private Button          controlButton;      // 추방 버튼
    [SerializeField] private TextMeshProUGUI playerState;

    [Header("캐릭터 수 조정")]
    [SerializeField] private TextMeshProUGUI charCountText;  // 현재 숫자
    [SerializeField] private Button          upButton;       // 올리기
    [SerializeField] private Button          downButton;     // 내리기

    /// <summary>
    /// 플레이어 이름, 역할, CharCount, 버튼 콜백을 한번에 설정합니다.
    /// </summary>
    /// <param name="name">표시할 닉네임</param>
    /// <param name="isHostSlot">이 슬롯이 호스트 슬롯인지 (index 0)</param>
    /// <param name="localIsHost">현재 로컬 플레이어가 호스트인지</param>
    /// <param name="charCount">조종할 캐릭터 수</param>
    /// <param name="canUp">올리기 버튼 활성 여부 (가져올 플레이어 없으면 false)</param>
    /// <param name="canDown">내리기 버튼 활성 여부 (CharCount == 1 이면 false)</param>
    /// <param name="onKick">추방 버튼 콜백</param>
    /// <param name="onUp">올리기 버튼 콜백</param>
    /// <param name="onDown">내리기 버튼 콜백</param>
    public void SetPlayer(string name, bool isHostSlot, bool localIsHost,
                          int charCount, bool canUp, bool canDown,
                          Action onKick = null, Action onUp = null, Action onDown = null)
    {
        if (playerDisplayName != null) playerDisplayName.text = name;
        if (playerState != null)       playerState.text = isHostSlot ? "Host" : "추방";

        // 추방 버튼
        if (controlButton != null)
        {
            controlButton.interactable = !isHostSlot && localIsHost;
            controlButton.onClick.RemoveAllListeners();
            if (onKick != null)
                controlButton.onClick.AddListener(() => onKick());
        }

        // 캐릭터 수 숫자 표시
        if (charCountText != null)
            charCountText.text = charCount.ToString();

        // 올리기 / 내리기 버튼 (호스트만 표시)
        if (upButton != null)
        {
            upButton.gameObject.SetActive(localIsHost);
            upButton.interactable = localIsHost && canUp;
            upButton.onClick.RemoveAllListeners();
            if (onUp != null)
                upButton.onClick.AddListener(() => onUp());
        }

        if (downButton != null)
        {
            downButton.gameObject.SetActive(localIsHost);
            downButton.interactable = localIsHost && canDown;
            downButton.onClick.RemoveAllListeners();
            if (onDown != null)
                downButton.onClick.AddListener(() => onDown());
        }
    }

    /// <summary>
    /// 슬롯을 빈 대기 상태로 초기화합니다.
    /// </summary>
    public void SetEmpty()
    {
        if (playerDisplayName != null) playerDisplayName.text = "대기 중...";
        if (playerState != null)       playerState.text = "";
        if (charCountText != null)     charCountText.text = "-";

        if (controlButton != null)
        {
            controlButton.interactable = false;
            controlButton.onClick.RemoveAllListeners();
        }

        if (upButton   != null) { upButton.gameObject.SetActive(false);   upButton.onClick.RemoveAllListeners(); }
        if (downButton != null) { downButton.gameObject.SetActive(false); downButton.onClick.RemoveAllListeners(); }
    }
}
