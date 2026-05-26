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
                        if (ReadySystem.Instance != null)
                            SetButtonInteractable(ReadySystem.Instance.AllReady);
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

            // 클릭 즉시 UI를 토글해 첫 클릭 반응 지연/누락 체감을 없앤다.
            // 서버 동기화 이벤트가 도착하면 최종 상태로 다시 맞춰진다.
            SyncClientReadyVisual(!_isReady);

            CacheLocalNetIdIfPossible();
            // CmdToggleReady는 서버에서 sender.identity.netId를 권한 기준으로 사용한다.
            // local netId 조회 타이밍 실패로 클릭이 드롭되지 않도록 파라미터 의존성을 제거한다.
            ReadySystem.Instance.CmdToggleReady(0);
        }

        private void OnClickStartGame()
        {
            if (QuestVoteSystem.Instance != null && !QuestVoteSystem.Instance.CanUseReadyOrStartButton)
            {
                Debug.LogWarning("[ReadyOrStartButton] 투표가 확정되지 않아 시작할 수 없습니다.");
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
            SetButtonInteractable(allReady);
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
                SetButtonInteractable(ReadySystem.Instance != null && ReadySystem.Instance.AllReady);
            else
                SetButtonInteractable(true);
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
