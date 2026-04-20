using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 비밀번호 입력 창.
/// 비번방에 입장하려 할 때 LobbyManager가 Open(room)으로 열어줍니다.
///
/// [인스펙터 연결 방법]
/// - _titleText       : "방 이름" 표시 텍스트 (선택)
/// - _passwordInput   : 비밀번호 InputField (contentType = Password 권장)
/// - _confirmButton   : 확인 버튼
/// - _cancelButton    : 취소 버튼
/// - _statusText      : 오류 메시지 텍스트 (선택)
/// </summary>
public class PasswordInputWindow : MonoBehaviour
{
    public static PasswordInputWindow Instance;

    [Tooltip("창 상단에 표시할 방 이름 텍스트 (없으면 생략 가능)")]
    [SerializeField] private TextMeshProUGUI _titleText;

    [SerializeField] private InputField _passwordInput;
    [SerializeField] private Button     _confirmButton;
    [SerializeField] private Button     _cancelButton;

    [Tooltip("오류/상태 메시지를 표시할 텍스트 (없으면 생략 가능)")]
    [SerializeField] private TextMeshProUGUI _statusText;

    private RoomInfo _pendingRoom;

    // ────────────────────────────────────────────────

    private void Awake()
    {
        Instance = this;

        _confirmButton.onClick.AddListener(OnClickConfirm);
        _cancelButton.onClick.AddListener(OnClickCancel);

        // 입력 필드가 비어 있으면 확인 버튼 비활성화
        _passwordInput.onValueChanged.AddListener(val =>
            _confirmButton.interactable = !string.IsNullOrEmpty(val));

        _confirmButton.interactable = false;
    }

    // ────────────────────────────────────────────────
    //  Public API
    // ────────────────────────────────────────────────

    /// <summary>
    /// 비밀번호 입력 창을 엽니다.
    /// </summary>
    public void Open(RoomInfo room)
    {
        _pendingRoom = room;

        _passwordInput.text = "";
        _confirmButton.interactable = false;
        SetStatus("", false);

        if (_titleText != null)
            _titleText.text = $"[{room.roomName}] 비밀번호 입력";

        gameObject.SetActive(true);
        _passwordInput.Select();
        _passwordInput.ActivateInputField();
    }

    // ────────────────────────────────────────────────

    private void OnClickConfirm()
    {
        if (_pendingRoom == null) return;

        string password = _passwordInput.text;
        if (string.IsNullOrEmpty(password))
        {
            SetStatus("비밀번호를 입력해주세요.", true);
            return;
        }

        SetStatus("입장 중...", false);
        _confirmButton.interactable = false;
        _cancelButton.interactable  = false;

        LobbyManager.Instance.JoinRoomWithPassword(_pendingRoom, password,
            onError: errMsg =>
            {
                // 비밀번호 틀림 or 기타 에러
                if (errMsg != null && errMsg.Contains("Wrong password"))
                    SetStatus("비밀번호가 틀렸습니다.", true);
                else if (errMsg != null && errMsg.Contains("Room is full"))
                    SetStatus("방이 가득 찼습니다.", true);
                else
                    SetStatus("오류가 발생했습니다. 다시 시도해주세요.", true);

                _confirmButton.interactable = true;
                _cancelButton.interactable  = true;
            });
    }

    private void OnClickCancel()
    {
        _pendingRoom = null;
        gameObject.SetActive(false);
    }

    private void SetStatus(string msg, bool isError)
    {
        if (_statusText == null) return;
        _statusText.text  = msg;
        _statusText.color = isError ? Color.red : Color.gray;
    }
}
