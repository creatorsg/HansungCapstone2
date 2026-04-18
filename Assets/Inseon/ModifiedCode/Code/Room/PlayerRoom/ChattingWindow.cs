using System.Collections;
using Mirror;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// 채팅 UI 관리 클래스.
///
/// [씬 구조]
///   ChattingUI
///     ├─ Scroll View
///     │    └─ Viewport
///     │         └─ Content          ← chatContent 연결
///     └─ InputField (Legacy)        ← inputField 연결
///
/// [전송] Enter 키
/// [스크롤] 마우스 휠 (ScrollRect 기본 동작)
///          새 메시지 수신 시 자동으로 맨 아래로 이동
///
/// [메시지 흐름]
///   InputField에서 Enter
///     → GameRoomPlayer.CmdSendChat()   (Command: 클라이언트 → 서버)
///     → GameRoomPlayer.RpcReceiveChat() (ClientRpc: 서버 → 전체)
///     → ChattingWindow.DisplayMessage()
/// </summary>
public class ChattingWindow : MonoBehaviour
{
    public static ChattingWindow Instance;

    [Header("UI 연결")]
    [SerializeField] private ScrollRect  scrollRect;   // Scroll View 컴포넌트
    [SerializeField] private Transform   chatContent;  // Scroll View > Viewport > Content
    [SerializeField] private GameObject  messagePrefab; // Text 하나짜리 프리팹
    [SerializeField] private InputField  inputField;   // InputField (Legacy)

    // ────────────────────────────────────────────────

    private void Awake()
    {
        Instance = this;
        Debug.Log("[ChattingWindow] Awake 실행됨 - Instance 설정 완료");
    }

    private void OnEnable()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("[ChattingWindow] OnEnable에서 Instance 재설정");
        }
    }

    private void Update()
    {
        if (inputField == null || !inputField.isFocused) return;

        var kb = Keyboard.current;
        if (kb == null) return;

        // Enter(메인) 또는 Enter(숫자패드) 로 전송
        if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
            Send();
    }

    // ────────────────────────────────────────────────
    //  Public API
    // ────────────────────────────────────────────────

    /// <summary>
    /// GameRoomPlayer.RpcReceiveChat 에서 호출됩니다.
    /// 메시지 오브젝트를 Content에 추가하고 스크롤을 맨 아래로 내립니다.
    /// </summary>
    public void DisplayMessage(string message)
    {
        if (chatContent == null || messagePrefab == null) return;

        var obj = Instantiate(messagePrefab, chatContent);
        var text = obj.GetComponentInChildren<Text>();
        if (text != null)
            text.text = message;

        StartCoroutine(ScrollToBottomNextFrame());
    }

    private IEnumerator ScrollToBottomNextFrame()
    {
        // Layout 재계산이 완전히 끝난 다음 프레임에 스크롤
        yield return new WaitForEndOfFrame();

        LayoutRebuilder.ForceRebuildLayoutImmediate(chatContent as RectTransform);

        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 0f;
    }

    // ────────────────────────────────────────────────
    //  Private
    // ────────────────────────────────────────────────

    private void Send()
    {
        if (inputField == null) return;

        string msg = inputField.text.Trim();
        if (string.IsNullOrEmpty(msg)) return;

        var localPlayer = NetworkClient.localPlayer?.GetComponent<Jun.GameRoomPlayer>();
        if (localPlayer == null)
        {
            Debug.LogWarning("[ChattingWindow] localPlayer를 찾을 수 없습니다.");
            return;
        }

        localPlayer.CmdSendChat(msg);

        // 입력창 초기화 후 즉시 포커스 유지
        inputField.text = "";
        inputField.ActivateInputField();
    }
}
