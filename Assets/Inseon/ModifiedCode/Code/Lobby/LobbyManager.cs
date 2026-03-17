using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviour
{
    [SerializeField] private Text _playerNickname;
    [SerializeField] private GameObject _createRoomWindow;
    [SerializeField] private InputField _roomCode;

    private Player _player;

    public static LobbyManager Instance;

    private void Awake()
    {
        _player = inseon.Playfab.User.PlayfabUserManage.Player;
        Instance = this;
    }

    private void Start()
    {
        if (_player == null)
        {
            Debug.LogError("Player component not found on LobbyManager.");
            return;
        }

        _playerNickname.text = _player.Nickname;
    }

    public void CreateRoom()
    {
        string roomId = Guid.NewGuid().ToString();

        var manager = Mirror.NetworkManager.singleton as MirrorNetworkManager;
        manager.RoomId = roomId;

        manager.StartHost();

        PlayfabRoomService.CreateRoom(roomId);
    }

    public void OpenCreateRoomWindow()
    {
        _createRoomWindow.SetActive(true);
    }

    public void CloseCreateRoomWindow()
    {
        _createRoomWindow.SetActive(false);
    }

    public void RefreshRoomList()
    {
        PlayfabRoomService.GetRoomList(rooms =>
        {
            RoomListUI.Instance.UpdateList(rooms);
        });
    }
    public void JoinRoom(RoomInfo room)
    {
        var manager = Mirror.NetworkManager.singleton;

        manager.networkAddress = room.ip;

        manager.StartClient();
    }
}
