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
            // ── 진단 로그 ──
            Debug.Log($"[JoinRoom] PlayFab에서 받은 방 정보:\n" +
                      $"  ip          = {room.ip}\n" +
                      $"  port        = {room.port}\n" +
                      $"  sessionId   = {room.sessionId}\n" +
                      $"  sessionToken= {room.sessionToken}");

            // sessionToken이 0이면 CloudScript가 아직 업데이트 안 된 것
            if (room.sessionToken == 0)
                Debug.LogWarning("[JoinRoom] sessionToken이 0입니다! PlayFab CloudScript가 업데이트됐는지 확인하세요.");

            // 2. 클라이언트용 유저 토큰 발급
            string clientIp = await PlayfabRoomCommand.GetPublicIPAsync();
            Debug.Log($"[JoinRoom] 클라이언트 공인 IP: {clientIp}");

            uint userToken = await EdgegapRelayManager.GetUserToken(room.sessionId, clientIp);
            Debug.Log($"[JoinRoom] GetUserToken 결과: {userToken}");

            if (userToken == 0)
                Debug.LogWarning("[JoinRoom] userToken이 0입니다! Edgegap GetUserToken API 호출이 실패했을 수 있습니다.");

            // 3. Transport에 릴레이 정보 설정
            var transport = manager.GetComponent<EdgegapKcpTransport>();
            if (transport == null)
            {
                Debug.LogError("EdgegapKcpTransport not found");
                return;
            }

            transport.relayAddress       = room.ip;
            transport.relayGameClientPort = (ushort)room.port;
            transport.sessionId          = room.sessionToken;
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

            // 4. 클라이언트 접속
            manager.networkAddress = room.ip;
            manager.StartClient();
        });
    }
}