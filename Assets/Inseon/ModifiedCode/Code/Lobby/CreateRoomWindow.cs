using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace inseon.Lobby
{
    /// <summary>
    /// 방 생성 UI를 담당합니다.
    /// 실제 방 생성 로직은 HostRoomService에 위임합니다.
    /// </summary>
    public class CreateRoomWindow : MonoBehaviour
    {
        [SerializeField] private TMP_InputField      _roomName;
        [SerializeField] private TMP_InputField      _password;
        [SerializeField] private Toggle              _privateRoomSetting;

        [SerializeField] private TextMeshProUGUI     _currentSettingRoomNumber;
        [SerializeField] private Button              _roomNumberUpButton;
        [SerializeField] private Button              _roomNumberDownButton;

        [SerializeField] private Button              _roomCreateButton;
        [SerializeField] private Button              _closeButton;

        private int _currentRoomNumber;
        private const int MIN_ROOM = 1;
        private const int MAX_ROOM = 4;

        private void Awake()
        {
            SetRoomNumber(1);
        }

        // ─── 인원 수 조절 ─────────────────────────────────────────────

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

        // ─── 방 생성 ──────────────────────────────────────────────────

        public async void CreateRoom()
        {
            if (!ButtonGuard.TryLock()) return;

            await HostRoomService.TryCreateAndHostRoom(
                roomName:   _roomName.text,
                password:   _password.text,
                isPrivate:  _privateRoomSetting.isOn,
                maxPlayers: _currentRoomNumber,
                onFail:     _ => ButtonGuard.Unlock()  // 실패 시만 해제, 성공은 씬 전환
            );
        }
    }
}
