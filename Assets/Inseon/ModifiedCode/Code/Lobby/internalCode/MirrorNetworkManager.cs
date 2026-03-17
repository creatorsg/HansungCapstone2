using Mirror;
using UnityEngine;

public class MirrorNetworkManager : NetworkManager
{
    private Player _player;
    public string RoomId;

    private void Awake()
    {
        _player = inseon.Playfab.User.PlayfabUserManage.Player;
    }

    public override void OnStartHost()
    {
        base.OnStartHost();
        Debug.Log("Host started");
    }

    public override void OnStopHost()
    {
        base.OnStopHost();
        Debug.Log("Host stopped");

        PlayfabRoomService.RemoveRoom(RoomId);
    }
}