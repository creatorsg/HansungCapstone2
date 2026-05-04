using Jun;
using Mirror;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Image = UnityEngine.UI.Image;

/// <summary>
/// Room.unity 씬의 UI를 통합 관리합니다.
/// Jun.LobbyManager 없이 이 클래스 하나로 동작합니다.
///
/// GameRoomPlayer가 호출하는 기능:
///   - UpdatePlayerNum(bool)    → 슬롯 갱신
///   - ActiveBTN(bool isServer) → Host/Client에 맞는 버튼 표시
///   - RefreshPlayerSlots()     → 슬롯 UI 갱신
///   - RefreshStartButton()     → Ready 상태 변화 시 Start 버튼 갱신
/// </summary>
public class PlayerRoomManager : MonoBehaviour
{
    public static PlayerRoomManager Instance;

    [Header("방 정보")]
    [SerializeField] private TextMeshProUGUI roomName;
    [SerializeField] private TextMeshProUGUI roomID;
    [SerializeField] private Image           roomTypeImage;
    [SerializeField] private Sprite          privateSprite;
    [SerializeField] private Sprite          publicSprite;

    [Header("플레이어 슬롯 (최대 4)")]
    [SerializeField] private List<PlayerSlot> playerSlots;

    [Header("Start / Ready 공용 버튼")]
    [Tooltip("Host면 Start, Client면 Ready로 동작하는 단일 버튼")]
    [SerializeField] private Button          actionButton;
    [SerializeField] private TextMeshProUGUI actionButtonText;

    private GameRoomManager _gameRoomManager;
    private bool _isHost = false;

    // ────────────────────────────────────────────────

    private void Awake()
    {
        Instance = this;
        _gameRoomManager = FindAnyObjectByType<GameRoomManager>();

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
    /// </summary>
    public void ActiveBTN(bool isServer)
    {
        _isHost = isServer;

        if (actionButtonText != null)
            actionButtonText.text = isServer ? "START" : "READY";

        if (actionButton != null)
        {
            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(OnClickAction);
        }

        // Host는 모든 플레이어 Ready 후 활성화, Client는 언제든 활성
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
    /// Ready 상태가 바뀔 때마다 호출됩니다.
    /// </summary>
    public void RefreshStartButton()
    {
        if (!_isHost || actionButton == null) return;

        EnsureManager();
        if (_gameRoomManager == null) return;

        var slots = _gameRoomManager.roomSlots;

        // 혼자일 때(호스트만 있을 때)는 바로 활성화
        if (slots.Count <= 1)
        {
            actionButton.interactable = true;
            return;
        }

        // 다른 플레이어가 있을 때는 모두 Ready여야 활성화
        bool allReady = true;
        foreach (var slot in slots)
        {
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
    /// CharCount 올리기/내리기 버튼 포함.
    /// </summary>
    public void RefreshPlayerSlots()
    {
        EnsureManager();
        if (_gameRoomManager == null || playerSlots == null || playerSlots.Count == 0) return;

        var slots = new List<NetworkRoomPlayer>(_gameRoomManager.roomSlots);

        bool localIsHost = NetworkServer.active;

        for (int i = 0; i < playerSlots.Count; i++)
        {
            if (playerSlots[i] == null) continue;

            if (i < slots.Count && slots[i] != null)
            {
                var roomPlayer = slots[i] as GameRoomPlayer;
                string nick = (roomPlayer != null && !string.IsNullOrEmpty(roomPlayer.PlayerNickname))
                              ? roomPlayer.PlayerNickname
                              : "...";
                bool isHostSlot = (i == 0);
                int  charCount  = roomPlayer != null ? roomPlayer.CharCount : 1;

                // 올리기: 대상 외 다른 플레이어 중 CharCount > 1 인 사람이 있어야 함
                bool canUp = localIsHost && slots.Count > 1 &&
                             slots.Exists(s => {
                                 var p = s as GameRoomPlayer;
                                 return p != null && p != roomPlayer && p.CharCount > 1;
                             });

                // 내리기: 본인이 2개 이상이고, 받을 다른 플레이어가 있어야 함
                bool canDown = localIsHost && charCount > 1 && slots.Count > 1;

                // 추방 콜백
                System.Action kickAction = null;
                if (!isHostSlot && localIsHost && roomPlayer != null)
                {
                    uint targetNetId = roomPlayer.netId;
                    kickAction = () => KickPlayer(targetNetId);
                }

                // 올리기 / 내리기 콜백
                System.Action upAction = null, downAction = null;
                if (localIsHost && roomPlayer != null)
                {
                    uint targetNetId = roomPlayer.netId;
                    upAction   = () => AdjustCharCount(targetNetId,  1);
                    downAction = () => AdjustCharCount(targetNetId, -1);
                }

                playerSlots[i].gameObject.SetActive(true);
                playerSlots[i].SetPlayer(nick, isHostSlot, localIsHost,
                                         charCount, canUp, canDown,
                                         kickAction, upAction, downAction);
            }
            else
            {
                playerSlots[i].SetEmpty();
            }
        }
    }

    /// <summary>
    /// 호스트가 특정 플레이어의 CharCount를 delta만큼 조정 요청합니다.
    /// </summary>
    private void AdjustCharCount(uint targetNetId, int delta)
    {
        var localPlayer = NetworkClient.localPlayer?.GetComponent<GameRoomPlayer>();
        if (localPlayer == null) return;
        localPlayer.CmdAdjustCharCount(targetNetId, delta);
    }

    /// <summary>
    /// 호스트가 특정 플레이어를 추방합니다.
    /// </summary>
    private void KickPlayer(uint targetNetId)
    {
        var localPlayer = NetworkClient.localPlayer?.GetComponent<GameRoomPlayer>();
        if (localPlayer == null)
        {
            Debug.LogWarning("[PlayerRoomManager] KickPlayer: 로컬 플레이어를 찾을 수 없습니다.");
            return;
        }
        Debug.Log($"[PlayerRoomManager] 추방 요청 → netId={targetNetId}");
        localPlayer.CmdKickPlayer(targetNetId);
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
    //  방 나가기
    // ────────────────────────────────────────────────

    /// <summary>
    /// 방 나가기 버튼 OnClick에 연결합니다.
    /// Host면 StopHost()로 방 전체를 닫고, Client면 StopClient()로 접속만 끊습니다.
    /// </summary>
    public void OnClickLeaveRoom()
    {
        EnsureManager();
        if (_gameRoomManager == null) return;

        if (_isHost)
            _gameRoomManager.StopHost();    // OnStopHost() → PlayfabCommand.RemoveRoom() 자동 호출
        else
            _gameRoomManager.StopClient();  // OnServerDisconnect() → PlayfabCommand.LeaveRoom() 자동 호출
    }

    // ────────────────────────────────────────────────
    //  버튼 OnClick (Host/Client 공용)
    // ────────────────────────────────────────────────

    /// <summary>
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
