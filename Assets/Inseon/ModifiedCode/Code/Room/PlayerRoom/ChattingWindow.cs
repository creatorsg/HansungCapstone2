using UnityEngine;
using Mirror;
using UnityEngine.UI;

public class ChattingWindow : NetworkBehaviour
{
    [SerializeField] private GameObject chatUI;
    private InputField inputField;
    private Text chatDisplay;

    public override void OnStartLocalPlayer()
    {
        chatUI.SetActive(true);
    }

    public void SendMessage(string message)
    {
        CmdSendMessage(message);
    }

    [Command]
    void CmdSendMessage(string message)
    {
        RpcReceiveMessage(message); 
    }

    [ClientRpc]
    void RpcReceiveMessage(string message)
    {
    }
}

