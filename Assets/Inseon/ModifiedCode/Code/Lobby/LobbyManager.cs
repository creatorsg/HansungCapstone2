using Edgegap;
using inseon.Core;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace inseon.Lobby.Manager
{
    public class LobbyManager : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _playerNickname;
        [SerializeField] private GameObject _createRoomWindow;
        [SerializeField] private GameObject _findRoomWindow;
        [SerializeField] private GameObject _settingWindow;
        [SerializeField] private PasswordInputWindow _passwordInputWindow;

        [Header("Proto Test")]
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
            RefreshRoomList();
        }

        // ─── 창 열기/닫기 ────────────────────────────────────────────

        public void OpenCreateRoomWindow() => _createRoomWindow.SetActive(true);
        public void CloseCreateRoomWindow() => _createRoomWindow.SetActive(false);

        public void OpenFindRoomWindow() => _findRoomWindow.SetActive(true);
        public void CloseFindRoomWindow() => _findRoomWindow.SetActive(false);

        public void OpenSettingWindow() => _settingWindow.SetActive(true);
        public void CloseSettingWindow() => _settingWindow.SetActive(false);

        // ─── 방 목록 ─────────────────────────────────────────────────

        public void RefreshRoomList()
        {
            if (!ButtonGuard.TryLock()) return;

            PlayfabCommand.GetRooms(json =>
            {
                var rooms = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Collections.Generic.List<RoomInfo>>(json);
                RoomListUI.Instance.UpdateList(rooms);
                ButtonGuard.Unlock();
            });
        }

        // ─── 방 입장 Public API ───────────────────────────────────────

        /// <summary>
        /// 방 목록/코드 검색에서 입장 버튼을 눌렀을 때 호출됩니다.
        /// 비번방이면 PasswordInputWindow를 열고, 일반방이면 바로 입장합니다.
        /// </summary>
        public void JoinRoom(RoomInfo room)
        {
            if (room.isPrivate)
            {
                if (_passwordInputWindow == null)
                {
                    Debug.LogError("[LobbyManager] PasswordInputWindow가 Inspector에 연결되지 않았습니다!");
                    return;
                }
                _passwordInputWindow.Open(room);
            }
            else
            {
                JoinRoomInternal(room, null, null);
            }
        }

        /// <summary>
        /// PasswordInputWindow에서 비밀번호 확인 후 호출됩니다.
        /// 비밀번호가 틀리면 onError 콜백으로 에러 메시지를 전달합니다.
        /// </summary>
        public void JoinRoomWithPassword(RoomInfo room, string password, System.Action<string> onError = null)
        {
            JoinRoomInternal(room, password, onError);
        }

        // ─── 실제 접속 로직 ───────────────────────────────────────────

        private void JoinRoomInternal(RoomInfo room, string password, System.Action<string> onError)
        {
            if (!ButtonGuard.TryLock()) return;

            var manager = Mirror.NetworkManager.singleton as Jun.GameRoomManager;
            if (manager == null)
            {
                const string err = "GameRoomManager를 찾을 수 없습니다. Scene에 GameRoomManager가 있는지 확인하세요.";
                Debug.LogError(err);
                onError?.Invoke(err);
                ButtonGuard.Unlock();
                return;
            }

            PlayfabCommand.JoinRoom(room.roomId, password, async joinedRoom =>
            {
                try
                {
                    // ── 접속 성공 시 비번 창 닫기 ──
                    if (_passwordInputWindow != null && _passwordInputWindow.gameObject.activeSelf)
                        _passwordInputWindow.gameObject.SetActive(false);

                    Debug.Log($"[JoinRoom] PlayFab(JoinRoom CS)에서 받은 방 정보:\n" +
                              $"  roomId      = {joinedRoom.roomId}\n" +
                              $"  ip          = {joinedRoom.ip}\n" +
                              $"  port        = {joinedRoom.port}\n" +
                              $"  sessionId   = {joinedRoom.sessionId}\n" +
                              $"  sessionToken= {joinedRoom.sessionToken}\n" +
                              $"  hostPublicIp= {joinedRoom.hostPublicIp}");

                    if (joinedRoom.sessionToken == 0)
                    {
                        Debug.LogError("[JoinRoom] sessionToken이 0입니다! 연결을 중단하고 playerCount를 롤백합니다.");
                        PlayfabCommand.LeaveRoom(joinedRoom.roomId);
                        ButtonGuard.Unlock();
                        return;
                    }

                    string clientIp = await PlayfabRoomCommand.GetPublicIPAsync();
                    Debug.Log($"[JoinRoom] 클라이언트 공인 IP: {clientIp}");

                    bool isSameIp = !string.IsNullOrEmpty(joinedRoom.hostPublicIp) &&
                                    joinedRoom.hostPublicIp == clientIp;

                    if (isSameIp && !_allowSameIpTest)
                    {
                        Debug.LogError("[JoinRoom] 호스트와 같은 공인 IP. 테스트 시 Inspector에서 'Allow Same Ip Test'를 ON으로 설정하세요.");
                        PlayfabCommand.LeaveRoom(joinedRoom.roomId);
                        ButtonGuard.Unlock();
                        return;
                    }

                    int tokenIndex = joinedRoom.playerCount - 1;

                    if (joinedRoom.userTokens == null || joinedRoom.userTokens.Length <= tokenIndex)
                    {
                        Debug.LogError($"[JoinRoom] userTokens 배열이 없거나 슬롯 부족. " +
                                       $"playerCount={joinedRoom.playerCount}, tokenIndex={tokenIndex}, " +
                                       $"userTokens길이={joinedRoom.userTokens?.Length ?? 0}");
                        PlayfabCommand.LeaveRoom(joinedRoom.roomId);
                        ButtonGuard.Unlock();
                        return;
                    }

                    uint userToken = joinedRoom.userTokens[tokenIndex];
                    Debug.Log($"[JoinRoom] userToken 슬롯[{tokenIndex}] = {userToken}");

                    if (userToken == 0)
                    {
                        Debug.LogError($"[JoinRoom] 슬롯[{tokenIndex}]의 userToken이 0.");
                        PlayfabCommand.LeaveRoom(joinedRoom.roomId);
                        ButtonGuard.Unlock();
                        return;
                    }

                    var transport = manager.GetComponent<EdgegapKcpTransport>();
                    if (transport == null)
                    {
                        Debug.LogError("EdgegapKcpTransport not found");
                        PlayfabCommand.LeaveRoom(joinedRoom.roomId);
                        ButtonGuard.Unlock();
                        return;
                    }

                    transport.relayAddress = joinedRoom.ip;
                    transport.relayGameClientPort = (ushort)joinedRoom.port;
                    transport.sessionId = joinedRoom.sessionToken;
                    transport.userId = userToken;

                    Debug.Log($"[JoinRoom] Transport 설정 완료:\n" +
                              $"  relayAddress       = {transport.relayAddress}\n" +
                              $"  relayGameClientPort= {transport.relayGameClientPort}\n" +
                              $"  sessionId          = {transport.sessionId}\n" +
                              $"  userId             = {transport.userId}");

                    if (NetworkClient.active)
                    {
                        Debug.LogWarning("[JoinRoom] 기존 클라이언트 연결 감지 → 정리 후 재접속");
                        manager.StopClient();
                    }

                    manager.RoomId = joinedRoom.roomId;
                    manager.RoomName = joinedRoom.roomName;
                    manager.networkAddress = joinedRoom.ip;
                    manager.StartClient();
                    // StartClient 성공 후 씬 전환되므로 Unlock 불필요
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[JoinRoom] 예외 발생: {e.Message}");
                    ButtonGuard.Unlock();
                }

            }, err =>
            {
                onError?.Invoke(err);
                ButtonGuard.Unlock();   // PlayFab 에러 시 잠금 해제
            });
        }
    }
}