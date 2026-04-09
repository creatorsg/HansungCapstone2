using Edgegap;
using Mirror;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace inseon.Lobby.Server.Room.CreateWindow
{
    public class CreateRoomWindow : MonoBehaviour
    {
        [SerializeField] private InputField _roomName;

        [SerializeField] private InputField _password;
        [SerializeField] private Toggle _privateRoomSetting;

        [SerializeField] private Text _currentSettingRoomNumber;
        [SerializeField] private Button _roomNumberUpButton;
        [SerializeField] private Button _roomNumberDownButton;

        [SerializeField] private Button _roomCreateButton;
        [SerializeField] private Button _closeButton;

        private int _currentRoomNumber;

        private const int MIN_ROOM = 1;
        private const int MAX_ROOM = 4;

        private void Awake()
        {
            SetRoomNumber(1);
        }

        private void SetRoomNumber(int value)
        {
            _currentRoomNumber = value;
            _currentSettingRoomNumber.text = value.ToString();
        }

        public void OnRoomNumberUpButtonClicked()
        {
            if (_currentRoomNumber < MAX_ROOM)
                SetRoomNumber(_currentRoomNumber + 1);
        }

        public void OnRoomNumberDownButtonClicked()
        {
            if (_currentRoomNumber > MIN_ROOM)
                SetRoomNumber(_currentRoomNumber - 1);
        }

        public async void CreateRoom()
        {
            _roomCreateButton.interactable = false;

            try
            {
                var manager = UnityEngine.Object.FindFirstObjectByType<Jun.GameRoomManager>();
                if (manager == null)
                {
                    Debug.LogError("[CreateRoom] GameRoomManager를 찾을 수 없습니다.");
                    _roomCreateButton.interactable = true;
                    return;
                }

                string roomId = CreateRoomID();

                // 공인 IP 가져오기
                string hostIp = await PlayfabRoomCommand.GetPublicIPAsync();
                Debug.Log($"[CreateRoom] 호스트 공인 IP: {hostIp}");

                // 릴레이 세션 생성
                var relay = await EdgegapRelayManager.CreateSession(hostIp);
                if (relay == null)
                {
                    Debug.LogError("[CreateRoom] 릴레이 세션 생성 실패");
                    _roomCreateButton.interactable = true;
                    return;
                }

                Debug.Log($"[CreateRoom] 릴레이 세션 생성 완료:\n" +
                          $"  relayAddress    = {relay.relayAddress}\n" +
                          $"  serverPort      = {relay.serverPort}\n" +
                          $"  clientPort      = {relay.clientPort}\n" +
                          $"  sessionId       = {relay.sessionId}\n" +
                          $"  sessionAuthToken= {relay.sessionAuthToken}\n" +
                          $"  userAuthToken   = {relay.userAuthToken}");

                // Transport 설정
                var transport = manager.GetComponent<EdgegapKcpTransport>();
                transport.relayAddress = relay.relayAddress;
                transport.relayGameServerPort = relay.serverPort;
                transport.relayGameClientPort = relay.clientPort;
                transport.sessionId = relay.sessionAuthToken;
                transport.userId = relay.userAuthToken;

                manager.RoomId = roomId;
                manager.maxConnections = _currentRoomNumber;

                // 이미 연결 중이면 먼저 정리
                if (NetworkServer.active || NetworkClient.active)
                {
                    Debug.LogWarning("[CreateRoom] 기존 연결 감지 → 정리 후 재시작");
                    manager.StopHost();
                }

                // 새 방 생성 = 이전 세이브 데이터 초기화
                PlayfabCommand.ResetSaveData();

                manager.StartHost();

                Debug.Log($"[CreateRoom] StartHost 호출 완료 - " +
                          $"Server.active={NetworkServer.active}, Client.active={NetworkClient.active}");

                PlayfabRoomCommand.CreateRoom(
                    roomId,
                    _roomName.text,
                    _privateRoomSetting.isOn,
                    _password.text,
                    _currentRoomNumber,
                    relay.relayAddress,
                    relay.clientPort,
                    relay.sessionId,
                    relay.sessionAuthToken
                );
            }
            catch (Exception e)
            {
                Debug.LogError($"[CreateRoom] 방 생성 중 오류 발생: {e.Message}\n{e.StackTrace}");
            }

            _roomCreateButton.interactable = true;
        }

        public void GetRoomList()
        {

        }

        private string CreateRoomID()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

            System.Text.StringBuilder roomCode = new System.Text.StringBuilder(6);

            for (int i = 0; i < 6; i++)
            {
                int index = UnityEngine.Random.Range(0, chars.Length);
                roomCode.Append(chars[index]);
            }

            return roomCode.ToString();
        }
    }
}