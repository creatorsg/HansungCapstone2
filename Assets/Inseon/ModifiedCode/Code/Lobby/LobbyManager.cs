using System;
using Edgegap;
using Mirror;
using UnityEngine;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviour
{
    [SerializeField] private Text _playerNickname;
    [SerializeField] private GameObject _createRoomWindow;

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
            Debug.LogError("Player not found.");
            return;
        }

        _playerNickname.text = _player.Nickname;

        // 로비 진입 시 자동으로 방 목록 갱신
        RefreshRoomList();
    }

    public void OpenCreateRoomWindow() => _createRoomWindow.SetActive(true);
    public void CloseCreateRoomWindow() => _createRoomWindow.SetActive(false);

    public void RefreshRoomList()
    {
        PlayfabCommand.GetRooms(json =>
        {
            var rooms = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Collections.Generic.List<RoomInfo>>(json);
            RoomListUI.Instance.UpdateList(rooms);
        }); 
    }

    public async void JoinRoom(RoomInfo room)
    {
        var manager = Mirror.NetworkManager.singleton as Jun.GameRoomManager;

        if (manager == null)
        {
            Debug.LogError("GameRoomManager를 찾을 수 없습니다. Scene에 GameRoomManager가 있는지 확인하세요.");
            return;
        }

        PlayfabCommand.JoinRoom(room.roomId, null, async joinedRoom =>
        {
            // 2. 클라이언트용 유저 토큰 발급
            string clientIp = await PlayfabRoomCommand.GetPublicIPAsync();
            uint userToken = await EdgegapRelayManager.GetUserToken(room.sessionId, clientIp);
            // 3. Transport에 릴레이 정보 설정
            var transport = manager.GetComponent<EdgegapKcpTransport>();
            if (transport == null)
            {
                Debug.LogError("EdgegapKcpTransport not found");
                return;
            }

            transport.relayAddress = room.ip;
            transport.relayGameClientPort = (ushort)room.port;
            transport.sessionId = room.sessionToken;
            transport.userId = userToken;

            // 이미 연결 중이면 먼저 정리
            if (NetworkClient.active)
            {
                Debug.LogWarning("[JoinRoom] 기존 클라이언트 연결 감지 → 정리 후 재접속");
                manager.StopClient();
            }

            // 4. 클라이언트 접속
            manager.networkAddress = room.ip;
            manager.StartClient();
        });
    }
}