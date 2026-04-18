using UnityEngine;
using UnityEngine.UI;

public class PlayerSlot : MonoBehaviour
{
    [SerializeField] private Text playerDisplayName;
    [SerializeField] private Button controlButton;
    [SerializeField] private Text playerState;

    /// <summary>
    /// 플레이어 이름과 역할(Host/추방버튼)을 한번에 설정합니다.
    /// </summary>
    public void SetPlayer(string name, bool isHost)
    {
        if (playerDisplayName != null) playerDisplayName.text = name;
        if (playerState != null)       playerState.text = isHost ? "Host" : "추방";
        if (controlButton != null)     controlButton.interactable = !isHost;
    }

    /// <summary>
    /// 비어있는 슬롯으로 초기화합니다.
    /// </summary>
    public void SetEmpty()
    {
        if (playerDisplayName != null) playerDisplayName.text = "대기 중...";
        if (playerState != null)       playerState.text = "";
        if (controlButton != null)     controlButton.interactable = false;
    }

    // 하위 호환용 메서드
    public void SetName(string name)
    {
        if (playerDisplayName != null) playerDisplayName.text = name;
    }

    public void SetState(bool isHost)
    {
        if (playerState != null)   playerState.text = isHost ? "Host" : "추방";
        if (controlButton != null) controlButton.interactable = !isHost;
    }

    public void SetSlotVaild(bool slotVaild)
    {
        if (!slotVaild) SetEmpty();
    }
}
