using UnityEngine;
using UnityEngine.UI;

public class PlayerSlot : MonoBehaviour
{
    [SerializeField] private Text playerDisplayName;
    [SerializeField] private Button controlButton;
    [SerializeField] private Text playerState;

    public void SetName(string name)
    {
        playerDisplayName.text = name;
    }

    public void SetState(bool host)
    {
        playerState.text = host ? "host" : "Ãß¹æ";
        controlButton.interactable = host ? false : true;
    }

    public void SetSlotVaild(bool slotVaild)
    {
        playerDisplayName.text = "Invalid slot";
        playerState.text = " ";
    }
}
