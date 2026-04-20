using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 방 ID(코드) 직접 입력 후 입장하는 창.
///
/// [인스펙터 연결 방법]
/// - _roomIdInput  : 방 코드를 입력하는 InputField
/// - _joinButton   : 입장 버튼
/// - _closeButton  : 창 닫기 버튼
/// - _statusText   : 오류/상태 메시지 텍스트 (선택)
///
/// LobbyManager와 별개의 GameObject에 붙이고,
/// LobbyManager의 OpenFindRoomWindow() / CloseFindRoomWindow()로 SetActive를 제어합니다.
/// </summary>
public class FindRoomWindow : MonoBehaviour
{
    [SerializeField] private InputField _roomIdInput;
    [SerializeField] private Button     _joinButton;

    [Tooltip("검색/오류 결과를 표시할 텍스트 (없으면 생략 가능)")]
    [SerializeField] private TextMeshProUGUI _statusText;

    private void Awake()
    {
        _joinButton.onClick.AddListener(OnClickJoin);

        // 입력 필드가 비어 있으면 버튼 비활성화
        _roomIdInput.onValueChanged.AddListener(val =>
            _joinButton.interactable = !string.IsNullOrWhiteSpace(val));

        _joinButton.interactable = false;
    }

    private void OnEnable()
    {
        // 창이 열릴 때 초기화
        _roomIdInput.text = "";
        _joinButton.interactable = false;
        SetStatus("", false);
    }

    // ──────────────────────────────────────────────────

    private void OnClickJoin()
    {
        string roomId = _roomIdInput.text.Trim().ToUpper();

        if (string.IsNullOrEmpty(roomId))
        {
            SetStatus("방 코드를 입력해주세요.", true);
            return;
        }

        SetStatus("방을 검색하는 중...", false);
        _joinButton.interactable = false;

        // 1단계: 방 정보 조회 (입장 전 유효성 확인)
        PlayfabCommand.GetRoomById(
            roomId,
            onSuccess: room =>
            {
                // 방이 가득 찼으면 입장 불가
                if (room.playerCount >= room.maxPlayers)
                {
                    SetStatus("방이 가득 찼습니다.", true);
                    _joinButton.interactable = true;
                    return;
                }

                // 2단계: 입장 시도 (private/일반 구분은 LobbyManager가 처리)
                // - 일반방: 바로 접속
                // - 비밀방: LobbyManager가 _passwordInputWindow.Open(room) 호출
                SetStatus("방에 입장하는 중...", false);
                gameObject.SetActive(false);            // 방 코드 창 먼저 닫기
                LobbyManager.Instance.JoinRoom(room);
            },
            onError: errMsg =>
            {
                // CloudScript 에러 메시지 파싱
                if (errMsg != null && errMsg.Contains("Room not found"))
                    SetStatus("존재하지 않는 방 코드입니다.", true);
                else
                    SetStatus("오류가 발생했습니다. 다시 시도해주세요.", true);

                _joinButton.interactable = true;
            }
        );
    }

    private void OnClickClose()
    {
        gameObject.SetActive(false);
    }

    private void SetStatus(string msg, bool isError)
    {
        if (_statusText == null) return;
        _statusText.text  = msg;
        _statusText.color = isError ? Color.red : Color.gray;
    }
}
