using Jun;
using Mirror;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Image = UnityEngine.UI.Image;

/// <summary>
/// Room.unity 씬의 UI를 통합 관리합니다.
/// Jun.LobbyManager 없이 이 클래스 하나로 동작합니다.
///
/// GameRoomPlayer가 호출하는 기능:
///   - UpdatePlayerNum(bool)  → 내부 카운트 관리 (슬롯으로 대체 가능하여 생략 가능)
///   - ActiveBTN(bool isServer) → Host/Client에 맞는 버튼 표시
///   - RefreshPlayerSlots()   → 슬롯 UI 갱신
/// </summary>
public class PlayerRoomManager : MonoBehaviour
{
    public static PlayerRoomManager Instance;

    [Header("방 정보")]
    [SerializeField] private Text   roomName;
    [SerializeField] private Text   roomID;
    [SerializeField] private Image  roomTypeImage;
    [SerializeField] private Sprite privateSprite;
    [SerializeField] private Sprite publicSprite;

    [Header("플레이어 슬롯 (최대 4)")]
    [SerializeField] private List<PlayerSlot> playerSlots;

    [Header("Start / Ready 공용 버튼")]
    [Tooltip("Host면 Start, Client면 Ready로 동작하는 단일 버튼")]
    [SerializeField] private Button          actionButton;
    [SerializeField] private Text            actionButtonText;

    private GameRoomManager _gameRoomManager;
    private bool _isHost = false;

    // ────────────────────────────────────────────────

    private void Awake()
    {
        Instance = this;
        _gameRoomManager = FindAnyObjectByType<GameRoomManager>();

        // 시작 전 버튼 비활성화
        if (actionButton != null) actionButton.interactable = false;
    }

    private void Start()
    {
        RefreshAll();
    }

    // ────────────────────────────────────────────────
    //  GameRoomPlayer에서 호출하는 Public API
    // ────────────────────────────────────────────────

    /// <summary>
    /// Host/Client 여부에 따라 버튼 텍스트와 동작을 설정합니다.
    /// GameRoomPlayer.OnStartClient(isServer) 에서 호출됩니다.
    /// </summary>
    public void ActiveBTN(bool isServer)
    {
        _isHost = isServer;

        if (actionButtonText != null)
            actionButtonText.text = isServer ? "START" : "READY";

        // 버튼 OnClick 재연결 (이전 리스너 제거 후 역할에 맞게 등록)
        if (actionButton != null)
        {
            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(OnClickAction);
        }

        // Host는 모든 플레이어 Ready 상태일 때만 활성화되므로 일단 비활성
        // Client는 언제든 누를 수 있으므로 활성
        if (actionButton != null)
            actionButton.interactable = !isServer;

        RefreshStartButton();
    }

    /// <summary>
    /// 플레이어 수 변경 시 호출됩니다.
    /// </summary>
    public void UpdatePlayerNum(bool entering)
    {
        RefreshPlayerSlots();
    }

    /// <summary>
    /// Host의 Start 버튼 활성화 여부를 갱신합니다.
    /// 누군가의 Ready 상태가 바뀔 때마다 호출됩니다.
    /// </summary>
    public void RefreshStartButton()
    {
        if (!_isHost || actionButton == null) return;

        EnsureManager();
        if (_gameRoomManager == null) return;

        var slots = _gameRoomManager.roomSlots;
        if (slots.Count <= 1)          // 혼자면 시작 불가
        {
            actionButton.interactable = false;
            return;
        }

        bool allReady = true;
        foreach (var slot in slots)
        {
            // Host 자신(index 0)은 제외하고 나머지가 모두 ready인지 확인
            if (slot.isLocalPlayer) continue;
            if (!slot.readyToBegin) { allReady = false; break; }
        }

        actionButton.interactable = allReady;
    }

    /// <summary>
    /// 방 정보 + 플레이어 슬롯을 한 번에 갱신합니다.
    /// </summary>
    public void RefreshAll()
    {
        RefreshRoomInfo();
        RefreshPlayerSlots();
    }

    /// <summary>
    /// 방 이름 / 방 코드를 UI에 반영합니다.
    /// </summary>
    public void RefreshRoomInfo()
    {
        EnsureManager();
        if (_gameRoomManager == null) return;

        if (roomID   != null) roomID.text   = _gameRoomManager.RoomId;
        if (roomName != null) roomName.text = _gameRoomManager.RoomName;
    }

    /// <summary>
    /// 현재 roomSlots를 읽어 PlayerSlot UI를 갱신합니다.
    /// </summary>
    public void RefreshPlayerSlots()
    {
        EnsureManager();
        if (_gameRoomManager == null || playerSlots == null || playerSlots.Count == 0) return;

        var slots = new List<NetworkRoomPlayer>(_gameRoomManager.roomSlots);

        for (int i = 0; i < playerSlots.Count; i++)
        {
            if (playerSlots[i] == null) continue;

            if (i < slots.Count && slots[i] != null)
            {
                var roomPlayer = slots[i] as GameRoomPlayer;
                string nick = (roomPlayer != null && !string.IsNullOrEmpty(roomPlayer.PlayerNickname))
                              ? roomPlayer.PlayerNickname
                              : "...";
                bool isHost = (i == 0);

                playerSlots[i].gameObject.SetActive(true);
                playerSlots[i].SetPlayer(nick, isHost);
            }
            else
            {
                playerSlots[i].SetEmpty();
            }
        }
    }

    /// <summary>
    /// 방 코드 복사 버튼 OnClick에 연결합니다.
    /// </summary>
    public void CopyRoomCode()
    {
        if (roomID == null) return;
        GUIUtility.systemCopyBuffer = roomID.text;
        Debug.Log("[PlayerRoomManager] Room Code Copied: " + roomID.text);
    }

    // ────────────────────────────────────────────────
    //  버튼 OnClick (Host/Client 공용)
    // ────────────────────────────────────────────────

    /// <summary>
    /// Inspector의 버튼 OnClick에 연결하거나, ActiveBTN()에서 코드로 등록됩니다.
    /// Host면 게임 시작, Client면 Ready 토글로 동작합니다.
    /// </summary>
    public void OnClickAction()
    {
        if (_isHost) HostStart();
        else         ClientToggleReady();
    }

    private void HostStart()
    {
        EnsureManager();
        if (_gameRoomManager == null) return;
        _gameRoomManager.ServerChangeScene(_gameRoomManager.GameplayScene);
    }

    private void ClientToggleReady()
    {
        var localPlayer = NetworkClient.localPlayer?.GetComponent<GameRoomPlayer>();
        if (localPlayer == null) return;

        bool next = !localPlayer.readyToBegin;
        localPlayer.CmdChangeReadyState(next);

        // 버튼 색으로 준비 상태 피드백 (회색 = 준비 완료)
        if (actionButton != null)
        {
            var colors = actionButton.colors;
            colors.normalColor = next ? Color.gray : Color.white;
            actionButton.colors = colors;
        }
    }

    // ────────────────────────────────────────────────

    private void EnsureManager()
    {
        if (_gameRoomManager == null)
            _gameRoomManager = FindAnyObjectByType<GameRoomManager>();
    }
}
