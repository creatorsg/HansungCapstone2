using UnityEngine;
using Mirror;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

namespace Jun
{
    public class LobbyManager : NetworkBehaviour
    {
        public static LobbyManager Instance;

        // ── 캐릭터 선택 슬롯 (기존) ──
        public List<GameObject> Go;
        [SerializeField] private List<Button> _heroBTN;
        [SerializeField] private Button _startBTN;
        [SerializeField] private Button _readyBTN;
        [SerializeField] private TextMeshProUGUI _playerNumText;

        // ── 플레이어 입장 슬롯 (최대 4칸) ──
        // Inspector에서 4개 할당. 각 슬롯 GameObject 안에 Image + TMP_Text(닉네임) 구성
        [Header("Player Slots (최대 4)")]
        [SerializeField] private List<GameObject>        _playerSlotObjects;  // 슬롯 루트 오브젝트
        [SerializeField] private List<TextMeshProUGUI>   _playerSlotNames;    // 닉네임 텍스트
        [SerializeField] private List<Image>             _playerSlotImages;   // 배경 이미지 (반투명 처리용)

        // ── 채팅 ──
        [Header("Chat")]
        [SerializeField] private ScrollRect      _chatScrollRect;
        [SerializeField] private Transform       _chatContent;       // ScrollRect > Content
        [SerializeField] private GameObject      _chatMessagePrefab; // Text 하나짜리 프리팹
        [SerializeField] private TMP_InputField  _chatInput;
        [SerializeField] private Button          _sendButton;

        private void Awake()
        {
            Instance = this;

            if (_sendButton != null)
                _sendButton.onClick.AddListener(OnSendButtonClicked);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ── 기존 기능 ──

        public void ActiveBTN(bool IsServer)
        {
            if (IsServer) _startBTN.gameObject.SetActive(true);
            else          _readyBTN.gameObject.SetActive(true);
        }

        public void OnClickedHero(int index)
        {
            var player = NetworkClient.localPlayer.GetComponent<GameRoomPlayer>();

            // CMDChoiceHero는 이제 CharacterCode(string)를 받습니다.
            // Jun 직행 경로는 CharacterRegistry가 없으므로 CharacterDatabase에서 역조회합니다.
            string code = null;
            foreach (var kv in CharacterDatabase.Stats)
            {
                if (kv.Value.index == index) { code = kv.Key; break; }
            }

            if (string.IsNullOrEmpty(code))
            {
                Debug.LogWarning($"[LobbyManager] index={index} 에 해당하는 CharacterCode를 찾지 못했습니다.");
                return;
            }

            player.CMDChoiceHero(code);
        }

        /// <summary>
        /// 플레이어 수 변경 시 호출됩니다.
        /// 카운터 대신 roomSlots를 직접 읽어 정확한 인원을 표시합니다.
        /// entering 파라미터는 호환성을 위해 유지하지만 내부에서 사용하지 않습니다.
        /// </summary>
        public void UpdatePlayerNum(bool entering)
        {
            RefreshPlayerSlots();
        }

        public void OnClickedReady()
        {
            var localPlayer = NetworkClient.localPlayer.GetComponent<GameRoomPlayer>();
            if (localPlayer.readyToBegin)
            {
                localPlayer.CmdChangeReadyState(false);
                _readyBTN.GetComponent<Image>().color = Color.white;
                foreach (var hero in _heroBTN) hero.interactable = true;
                return;
            }
            if (localPlayer.CharaterNum.Count == 0) return;
            localPlayer.CmdChangeReadyState(true);
            _readyBTN.GetComponent<Image>().color = Color.gray;
            foreach (var hero in _heroBTN) hero.interactable = false;
        }

        public void OnClickedStart()
        {
            var localPlayer = NetworkClient.localPlayer.GetComponent<GameRoomPlayer>();
            var manager     = NetworkManager.singleton as GameRoomManager;

            bool allReady = true;
            foreach (var player in manager.roomSlots)
            {
                if (player == localPlayer) continue;
                if (!player.readyToBegin) allReady = false;
            }

            if (allReady) manager.ServerChangeScene(manager.GameplayScene);
            else          Debug.Log("모든 플레이어가 준비되지 않았습니다.");
        }

        // ── 플레이어 슬롯 ──

        /// <summary>
        /// 현재 방의 roomSlots 상태에 맞게 슬롯 UI와 인원 수 텍스트를 갱신합니다.
        /// GameRoomPlayer.OnStartClient / OnDestroy / OnNicknameChanged 에서 호출됩니다.
        /// </summary>
        public void RefreshPlayerSlots()
        {
            if (_playerSlotObjects == null || _playerSlotObjects.Count == 0) return;

            var manager = NetworkManager.singleton as GameRoomManager;
            int maxPlayers = (manager != null) ? manager.maxConnections : _playerSlotObjects.Count;

            // roomSlots를 한 번만 List로 변환해서 재사용
            var slotList = (manager != null)
                ? new System.Collections.Generic.List<NetworkRoomPlayer>(manager.roomSlots)
                : new System.Collections.Generic.List<NetworkRoomPlayer>();

            // 실제 인원 수를 roomSlots.Count에서 직접 읽어 텍스트 갱신
            if (_playerNumText != null)
                _playerNumText.text = slotList.Count.ToString();

            for (int i = 0; i < _playerSlotObjects.Count; i++)
            {
                bool withinMax = i < maxPlayers;

                // maxPlayers를 초과하는 슬롯은 반투명
                if (_playerSlotImages != null && i < _playerSlotImages.Count && _playerSlotImages[i] != null)
                {
                    Color c = _playerSlotImages[i].color;
                    c.a = withinMax ? 1f : 0.3f;
                    _playerSlotImages[i].color = c;
                }

                // 닉네임 표시
                if (_playerSlotNames != null && i < _playerSlotNames.Count && _playerSlotNames[i] != null)
                {
                    if (i < slotList.Count && slotList[i] != null)
                    {
                        var roomPlayer = slotList[i] as GameRoomPlayer;
                        string nick = (roomPlayer != null && !string.IsNullOrEmpty(roomPlayer.PlayerNickname))
                            ? roomPlayer.PlayerNickname
                            : "...";
                        _playerSlotNames[i].text = nick;
                    }
                    else
                    {
                        _playerSlotNames[i].text = withinMax ? "대기 중..." : "-";
                    }
                }
            }
        }

        // ── 채팅 ──

        /// <summary>
        /// 채팅창에 메시지를 추가합니다. GameRoomPlayer.RpcReceiveChat 에서 호출됩니다.
        /// </summary>
        public void AddChatMessage(string message)
        {
            if (_chatContent == null || _chatMessagePrefab == null) return;

            var obj = Instantiate(_chatMessagePrefab, _chatContent);
            var text = obj.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null) text.text = message;

            // 스크롤을 항상 최신 메시지로
            Canvas.ForceUpdateCanvases();
            if (_chatScrollRect != null)
                _chatScrollRect.verticalNormalizedPosition = 0f;
        }

        /// <summary>
        /// Send 버튼 클릭 또는 Enter 키 시 호출.
        /// </summary>
        public void OnSendButtonClicked()
        {
            if (_chatInput == null) return;
            string msg = _chatInput.text.Trim();
            if (string.IsNullOrEmpty(msg)) return;

            var localPlayer = NetworkClient.localPlayer?.GetComponent<GameRoomPlayer>();
            localPlayer?.CmdSendChat(msg);

            _chatInput.text = "";
            _chatInput.ActivateInputField();
        }

        private void Update()
        {
            // Enter 키로도 채팅 전송
            if (_chatInput != null && _chatInput.isFocused && Input.GetKeyDown(KeyCode.Return))
                OnSendButtonClicked();
        }
    }
}
