using System;
using Edgegap;
using Mirror;
using UnityEngine;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviour
{
    [SerializeField] private Text _playerNickname;
    [SerializeField] private GameObject _createRoomWindow;

    [Header("Debug / Test")]
    [Tooltip("ON: 같은 공인 IP끼리도 접속 허용 (테스트용). 실제 배포 시 OFF 권장.")]
    [SerializeField] private bool _allowSameIpTest = false;

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

        // ── [FIX] PlayfabCommand.JoinRoom 콜백은 joinedRoom을 사용해야 함 ──
        // GetRoomList는 보안상 sessionToken을 지워서 반환하므로,
        // JoinRoom CloudScript가 반환하는 joinedRoom에만 sessionToken이 존재함.
        PlayfabCommand.JoinRoom(room.roomId, null, async joinedRoom =>
        {
            // ── 진단 로그 (joinedRoom 사용) ──
            Debug.Log($"[JoinRoom] PlayFab(JoinRoom CS)에서 받은 방 정보:\n" +
                      $"  roomId      = {joinedRoom.roomId}\n" +
                      $"  ip          = {joinedRoom.ip}\n" +
                      $"  port        = {joinedRoom.port}\n" +
                      $"  sessionId   = {joinedRoom.sessionId}\n" +
                      $"  sessionToken= {joinedRoom.sessionToken}\n" +
                      $"  hostPublicIp= {joinedRoom.hostPublicIp}");

            // sessionToken이 0이면 CloudScript 버전 불일치 → 즉시 중단
            if (joinedRoom.sessionToken == 0)
            {
                Debug.LogError("[JoinRoom] sessionToken이 0입니다! 연결을 중단하고 playerCount를 롤백합니다.");
                PlayfabCommand.LeaveRoom(joinedRoom.roomId);
                return;
            }

            // ── 클라이언트 공인 IP (same-IP 테스트 판별용으로만 사용) ──
            string clientIp = await PlayfabRoomCommand.GetPublicIPAsync();
            Debug.Log($"[JoinRoom] 클라이언트 공인 IP: {clientIp}");

            bool isSameIp = !string.IsNullOrEmpty(joinedRoom.hostPublicIp) &&
                            joinedRoom.hostPublicIp == clientIp;

            Debug.Log($"[JoinRoom] 클라이언트 IP={clientIp} / 호스트 IP={joinedRoom.hostPublicIp} / SameIp={isSameIp}");

            if (isSameIp && !_allowSameIpTest)
            {
                Debug.LogError("[JoinRoom] 호스트와 같은 공인 IP. 테스트 시 Inspector에서 'Allow Same Ip Test'를 ON으로 설정하세요.");
                PlayfabCommand.LeaveRoom(joinedRoom.roomId);
                return;
            }

            // ── 선발급 userToken 배열에서 이 클라이언트 슬롯 인덱스 계산 ──
            // JoinRoom CS가 playerCount를 이미 올린 상태로 반환하므로:
            //   playerCount=1 → 호스트(슬롯0, 방 생성 시 처리됨)
            //   playerCount=2 → 첫 번째 클라이언트 → 슬롯1
            //   playerCount=3 → 두 번째 클라이언트 → 슬롯2
            int tokenIndex = joinedRoom.playerCount - 1;

            if (joinedRoom.userTokens == null || joinedRoom.userTokens.Length <= tokenIndex)
            {
                Debug.LogError($"[JoinRoom] userTokens 배열이 없거나 슬롯 부족. " +
                               $"playerCount={joinedRoom.playerCount}, tokenIndex={tokenIndex}, " +
                               $"userTokens길이={joinedRoom.userTokens?.Length ?? 0}");
                PlayfabCommand.LeaveRoom(joinedRoom.roomId);
                return;
            }

            uint userToken = joinedRoom.userTokens[tokenIndex];
            Debug.Log($"[JoinRoom] userToken 슬롯[{tokenIndex}] = {userToken}");

            if (userToken == 0)
            {
                Debug.LogError($"[JoinRoom] 슬롯[{tokenIndex}]의 userToken이 0. 릴레이 세션 생성 시 토큰이 발급되지 않았습니다.");
                PlayfabCommand.LeaveRoom(joinedRoom.roomId);
                return;
            }

            // ── Transport에 릴레이 정보 설정 ──
            var transport = manager.GetComponent<EdgegapKcpTransport>();
            if (transport == null)
            {
                Debug.LogError("EdgegapKcpTransport not found");
                PlayfabCommand.LeaveRoom(joinedRoom.roomId);
                return;
            }

            transport.relayAddress        = joinedRoom.ip;
            transport.relayGameClientPort = (ushort)joinedRoom.port;
            transport.sessionId           = joinedRoom.sessionToken;
            transport.userId              = userToken;

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

            // ── 클라이언트 접속 전, 방 정보를 manager에 저장 ──
            // (Host는 CreateRoomWindow에서 이미 세팅 완료)
            manager.RoomId   = joinedRoom.roomId;
            manager.RoomName = joinedRoom.roomName;

            manager.networkAddress = joinedRoom.ip;
            manager.StartClient();
        });
    }
}