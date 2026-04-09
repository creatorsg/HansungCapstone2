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

    public void JoinRoom(RoomInfo room)
    {
        var manager = Mirror.NetworkManager.singleton as Jun.GameRoomManager;

        if (manager == null)
        {
            Debug.LogError("GameRoomManager를 찾을 수 없습니다. Scene에 GameRoomManager가 있는지 확인하세요.");
            return;
        }

        PlayfabCommand.JoinRoom(room.roomId, null, joinedRoom =>
        {
            // joinedRoom은 JoinRoom CloudScript가 반환한 데이터 (sessionToken 포함)
            // room (목록에서 가져온 데이터)에는 sessionToken이 보안상 삭제되어 0임
            Debug.Log($"[JoinRoom] PlayFab에서 받은 방 정보:\n" +
                      $"  ip          = {joinedRoom.ip}\n" +
                      $"  port        = {joinedRoom.port}\n" +
                      $"  sessionId   = {joinedRoom.sessionId}\n" +
                      $"  sessionToken= {joinedRoom.sessionToken}");

            if (joinedRoom.sessionToken == 0)
                Debug.LogWarning("[JoinRoom] sessionToken이 0입니다! PlayFab CloudScript가 업데이트됐는지 확인하세요.");

            ConnectToRoom(manager, joinedRoom);
        });
    }

    private async void ConnectToRoom(Jun.GameRoomManager manager, RoomInfo joinedRoom)
    {
        try
        {
            // 1. 클라이언트용 유저 토큰 발급
            string clientIp = await PlayfabRoomCommand.GetPublicIPAsync();
            Debug.Log($"[JoinRoom] 클라이언트 공인 IP: {clientIp}");

            uint userToken = await EdgegapRelayManager.GetUserToken(joinedRoom.sessionId, clientIp);
            Debug.Log($"[JoinRoom] GetUserToken 결과: {userToken}");

            if (userToken == 0)
                Debug.LogWarning("[JoinRoom] userToken이 0입니다! Edgegap GetUserToken API 호출이 실패했을 수 있습니다.");

            // 2. Transport에 릴레이 정보 설정
            var transport = manager.GetComponent<EdgegapKcpTransport>();
            if (transport == null)
            {
                Debug.LogError("[JoinRoom] EdgegapKcpTransport not found");
                return;
            }

            transport.relayAddress       = joinedRoom.ip;
            transport.relayGameClientPort = (ushort)joinedRoom.port;
            transport.sessionId          = joinedRoom.sessionToken;
            transport.userId             = userToken;

            Debug.Log($"[JoinRoom] Transport 설정 완료:\n" +
                      $"  relayAddress       = {transport.relayAddress}\n" +
                      $"  relayGameClientPort= {transport.relayGameClientPort}\n" +
                      $"  sessionId          = {transport.sessionId}\n" +
                      $"  userId             = {transport.userId}");

            // 이미 연결 중이면 먼저 정리
            if (NetworkClient.active)
            {
                Debug.LogWarning("[JoinRoom] 기존 클라이언트 연결 감지 → 정리 후 재접속");
                manager.StopClient();
            }

            // 3. 클라이언트 접속
            manager.networkAddress = joinedRoom.ip;
            manager.StartClient();
            Debug.Log("[JoinRoom] StartClient 호출 완료");
        }
        catch (Exception e)
        {
            Debug.LogError($"[JoinRoom] 방 접속 중 오류 발생: {e.Message}\n{e.StackTrace}");
        }
    }
}