using Mirror;
using UnityEngine;

public class MyNetworkManager : NetworkManager
{
    [HideInInspector] public string PlayerName;

    public override void OnClientConnect()
    {
        base.OnClientConnect();
        NetManager.Instance.OnConnected();
    }

    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();
        NetManager.Instance.OnDisconnected();
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        GameObject player = Instantiate(playerPrefab);
        player.GetComponent<Player>().SetPlayerName(PlayerName);
        NetworkServer.AddPlayerForConnection(conn, player);
    }
}
