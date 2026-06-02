using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lsy
{
    public class ReadyOrStartButton : MonoBehaviour
    {
        [Header("버튼 컴포넌트")]
        public Button button;
        public TextMeshProUGUI buttonText;

        [Header("호스트 텍스트")]
        public string hostReadyText = "게임 시작";

        [Header("클라이언트 텍스트")]
        public string clientNotReadyText = "준비";
        public string clientReadyText = "준비 취소";

        [Header("색상")]
        public Color activeColor = Color.white;
        public Color lockedColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        public Color clientReadyColor = Color.green;

        [Header("디버그(테스트용 - 빌드 전 반드시 OFF)")]
        [Tooltip("켜면 호스트가 클라이언트 준비 여부와 상관없이 시작 버튼을 누를 수 있음")]
        [SerializeField] private bool debugForceHostInteractable = false;

        private Image _buttonImage;
        private bool _isReady;
        private uint _cachedLocalNetId;

        private bool IsHost => NetworkServer.active && NetworkClient.active;

        private void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();
            if (buttonText == null)
                buttonText = GetComponentInChildren<TextMeshProUGUI>(true);

            if (button != null)
            {
                _buttonImage = button.GetComponent<Image>();
                button.transition = Selectable.Transition.None;
                button.onClick = new Button.ButtonClickedEvent();
            }
        }

        private void Start()
        {
            if (button == null || buttonText == null)
            {
                Debug.LogWarning("[ReadyOrStartButton] button 또는 buttonText가 연결되지 않아 초기화를 중단합니다.");
                return;
            }

            if (IsHost)
            {
                buttonText.text = hostReadyText;
                if (debugForceHostInteractable)
                {
                    SetButtonInteractable(true);
                }
                else
                {
                    if (QuestVoteSystem.Instance != null && !QuestVoteSystem.Instance.CanUseReadyOrStartButton)
                    {
                        SetButtonInteractable(false);
                    }
                    else
                    {
                        SetButtonInteractable(false);
                        SetButtonInteractable(IsSoloHost() || (ReadySystem.Instance != null && ReadySystem.Instance.AllReady));
                    }
                }
            }
            else
            {
                SyncClientReadyVisual(false);
                bool canUseButton = QuestVoteSystem.Instance == null || QuestVoteSystem.Instance.CanUseReadyOrStartButton;
                SetButtonInteractable(canUseButton);
            }
        }

        private void OnEnable()
        {
            ReadySystem.OnAllReadyChanged += OnAllReadyChanged;
            ReadySystem.OnPlayerReadyChanged += OnPlayerReadyChanged;
            QuestVoteSystem.OnVotePhaseChanged += OnVotePhaseChanged;
            if (button != null)
            {
                button.onClick.AddListener(OnClick);
            }
        }

        private void OnDisable()
        {
            ReadySystem.OnAllReadyChanged -= OnAllReadyChanged;
            ReadySystem.OnPlayerReadyChanged -= OnPlayerReadyChanged;
            QuestVoteSystem.OnVotePhaseChanged -= OnVotePhaseChanged;
            button?.onClick.RemoveListener(OnClick);
        }

        private void OnClick()
        {
            if (IsHost)
                OnClickStartGame();
            else
                OnClickReady();
        }

        private void OnClickReady()
        {
            if (ReadySystem.Instance == null)
            {
                Debug.LogWarning("[ReadyOrStartButton] ReadySystem.Instance가 NULL!");
                return;
            }

            CacheLocalNetIdIfPossible();
            // [수정] UI는 서버 동기화 이벤트에서만 바꾸고, 서버에는 목표 상태를 명시적으로 요청합니다.
            ReadySystem.Instance.CmdSetReady(!_isReady);
        }

        private void OnClickStartGame()
        {
            if (QuestVoteSystem.Instance != null && !QuestVoteSystem.Instance.CanUseReadyOrStartButton)
            {
                Debug.LogWarning("[ReadyOrStartButton] 투표가 확정되지 않아 시작할 수 없습니다.");
                return;
            }

            // [수정] UI가 잘못 활성화되어도 모든 클라이언트가 준비하지 않았다면 전투 시작을 차단합니다.
            if (!IsSoloHost() && (ReadySystem.Instance == null || !ReadySystem.Instance.AllReady))
            {
                Debug.LogWarning("[ReadyOrStartButton] 모든 클라이언트가 준비되지 않아 시작할 수 없습니다.");
                return;
            }

            var rm = NetworkManager.singleton as Jun.GameRoomManager;
            string targetScene = rm?.GameplayScene;

            if (string.IsNullOrEmpty(targetScene))
            {
                Debug.LogWarning("[ReadyOrStartButton] GameplayScene이 비어있습니다. 지도에서 영지를 먼저 클릭하세요.");
                return;
            }

            if (NetworkManager.singleton != null)
            {
                PlayerAccount.SyncAllAccountsHideoutDataToBattleData();
                NetworkManager.singleton.ServerChangeScene(targetScene);
            }
        }

        private void OnAllReadyChanged(bool allReady)
        {
            if (!IsHost) return;
            if (debugForceHostInteractable) return;
            if (QuestVoteSystem.Instance != null && !QuestVoteSystem.Instance.CanUseReadyOrStartButton)
            {
                SetButtonInteractable(false);
                return;
            }
            SetButtonInteractable(IsSoloHost() || allReady);
        }

        private void OnVotePhaseChanged(bool isVoteRunning, bool isVoteFinished)
        {
            if (button == null) return;
            if (debugForceHostInteractable) return;

            if (!isVoteFinished)
            {
                SetButtonInteractable(false);
                return;
            }

            if (IsHost)
                SetButtonInteractable(IsSoloHost() || (ReadySystem.Instance != null && ReadySystem.Instance.AllReady));
            else
                SetButtonInteractable(true);
        }

        private bool IsSoloHost()
        {
            if (!NetworkServer.active) return false;

            foreach (var conn in NetworkServer.connections.Values)
            {
                if (conn == null) continue;
                if (conn.connectionId == 0) continue;
                return false;
            }

            return true;
        }

        private void OnPlayerReadyChanged(uint playerNetId, bool isReady)
        {
            if (IsHost) return;
            CacheLocalNetIdIfPossible();
            if (_cachedLocalNetId == 0 || playerNetId != _cachedLocalNetId) return;

            SyncClientReadyVisual(isReady);
        }

        private void CacheLocalNetIdIfPossible()
        {
            if (_cachedLocalNetId != 0) return;
            if (CharacterSlotManager.TryGetLocalNetId(out uint myNetId))
                _cachedLocalNetId = myNetId;
        }

        private void SyncClientReadyVisual(bool isReady)
        {
            _isReady = isReady;
            if (buttonText != null)
                buttonText.text = _isReady ? clientReadyText : clientNotReadyText;
            if (_buttonImage != null)
                _buttonImage.color = _isReady ? clientReadyColor : activeColor;
        }

        private void SetButtonInteractable(bool interactable)
        {
            if (button == null) return;
            button.interactable = interactable;
            if (_buttonImage != null)
                _buttonImage.color = interactable ? activeColor : lockedColor;
        }
    }
}
