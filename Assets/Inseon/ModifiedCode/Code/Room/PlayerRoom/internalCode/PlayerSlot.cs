using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerSlot : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI playerDisplayName;
    [SerializeField] private Button          controlButton;
    [SerializeField] private TextMeshProUGUI playerState;

    /// <summary>
    /// 플레이어 이름과 역할(Host/추방버튼)을 한번에 설정합니다.
    /// </summary>
    /// <param name="name">표시할 닉네임</param>
    /// <param name="isHostSlot">이 슬롯이 호스트 슬롯인지 (index 0)</param>
    /// <param name="localIsHost">현재 로컬 플레이어가 호스트인지</param>
    /// <param name="onKick">추방 버튼 클릭 시 실행할 콜백 (null이면 등록 안 함)</param>
    public void SetPlayer(string name, bool isHostSlot, bool localIsHost, Action onKick = null)
    {
        if (playerDisplayName != null) playerDisplayName.text = name;
        if (playerState != null)       playerState.text = isHostSlot ? "Host" : "추방";

        if (controlButton != null)
        {
            // 버튼 활성화 조건:
            //  - 이 슬롯이 호스트 슬롯이 아님 (호스트는 추방 불가)
            //  - 로컬 플레이어가 호스트임 (호스트만 추방 권한)
            controlButton.interactable = !isHostSlot && localIsHost;

            controlButton.onClick.RemoveAllListeners();
            if (onKick != null)
                controlButton.onClick.AddListener(() => onKick());
        }
    }

    /// <summary>
    /// 비어있는 슬롯으로 초기화합니다.
    /// </summary>
    public void SetEmpty()
    {
        if (playerDisplayName != null) playerDisplayName.text = "대기 중...";
        if (playerState != null)       playerState.text = "";
        if (controlButton != null)
        {
            controlButton.interactable = false;
            controlButton.onClick.RemoveAllListeners();
        }
    }

    // 하위 호환용 메서드
    public void SetName(string name)
    {
        if (playerDisplayName != null) playerDisplayName.text = name;
    }

    public void SetState(bool isHostSlot, bool localIsHost)
    {
        if (playerState != null)   playerState.text = isHostSlot ? "Host" : "추방";
        if (controlButton != null) controlButton.interactable = !isHostSlot && localIsHost;
    }

    public void SetSlotVaild(bool slotVaild)
    {
        if (!slotVaild) SetEmpty();
    }
}
