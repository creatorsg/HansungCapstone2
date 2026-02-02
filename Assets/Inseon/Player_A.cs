using Mirror;
using UnityEngine;

public class Player : NetworkBehaviour
{
    [SyncVar] private string playerName;

    public void SetPlayerName(string name)
    {
        playerName = name;
    }
}
