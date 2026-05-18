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

        // 씬 이름은 DistrictHover 클릭 시 GameRoomManager.GameplayScene에 자동 등록됩니다.
        // 이 버튼은 별도 씬 이름 없이 GameplayScene을 그대로 사용합니다.

        [Header("디버그(테스트용 - 빌드 전 반드시 OFF)")]
        [Tooltip("켜면 호스트가 클라이언트 준비 여부와 상관없이 시작 버튼을 누를 수 있음")]
        [SerializeField] private bool debugForceHostInteractable = false;

        private Image _buttonImage;
        private bool _isReady = false;

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
                // 씬에 남아있는 영구 OnClick(GameStartBtn 등) 영향을 끊고 이 스크립트 분기만 사용한다.
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

            Debug.Log($"<color=cyan>[ReadyOrStartButton] Start - IsHost:{IsHost}</color>");

            if (IsHost)
            {
                buttonText.text = hostReadyText;

                if (ReadySystem.Instance != null && NetworkServer.active)
                    ReadySystem.Instance.ServerRefreshReadyState();

                if (debugForceHostInteractable)
                {
                    SetButtonInteractable(true);
                }
                else
                {
                    SetButtonInteractable(false);
                    if (ReadySystem.Instance != null)
                        SetButtonInteractable(ReadySystem.Instance.AllReady);
                }
            }
            else
            {
                SyncClientReadyVisual(false);
                SetButtonInteractable(true);
            }
        }

        private void OnEnable()
        {
            ReadySystem.OnAllReadyChanged += OnAllReadyChanged;
            ReadySystem.OnPlayerReadyChanged += OnPlayerReadyChanged;
            if (button != null)
            {
                button.onClick.AddListener(OnClick);
            }
        }

        private void OnDisable()
        {
            ReadySystem.OnAllReadyChanged -= OnAllReadyChanged;
            ReadySystem.OnPlayerReadyChanged -= OnPlayerReadyChanged;
            // Bug Fix: OnEnable에서 추가한 리스너를 반드시 제거해야 중복 등록 방지
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

            if (!CharacterSlotManager.TryGetLocalNetId(out uint myNetId))
            {
                Debug.LogWarning("[ReadyOrStartButton] local netId를 찾지 못함");
                return;
            }

            Debug.Log($"<color=yellow>[ReadyOrStartButton] 준비 토글 - netId:{myNetId}</color>");
            // 서버로 보낸다: 준비 토글 요청(netId 기준)
            ReadySystem.Instance.CmdToggleReady(myNetId);
        }

        private void OnClickStartGame()
        {
            Debug.Log("[ReadyOrStartButton] 게임 시작!");

            if (!debugForceHostInteractable && ReadySystem.Instance != null && !ReadySystem.Instance.AllReady)
            {
                Debug.LogWarning("[ReadyOrStartButton] 아직 준비하지 않은 클라이언트가 있어 시작할 수 없습니다.");
                return;
            }

            var rm = NetworkManager.singleton as Jun.GameRoomManager;

            // 씬 이름은 DistrictHover.OnPointerClick에서 GameplayScene에 등록됩니다.
            string targetScene = rm?.GameplayScene;

            if (string.IsNullOrEmpty(targetScene))
            {
                Debug.LogWarning("[ReadyOrStartButton] GameplayScene이 비어있습니다. " +
                                 "지도에서 영지를 먼저 클릭하세요.");
                return;
            }

            if (NetworkManager.singleton != null)
            {
                NetworkManager.singleton.ServerChangeScene(targetScene);
            }
            else
            {
                Debug.LogWarning("[ReadyOrStartButton] NetworkManager.singleton이 NULL - 씬 전환 실패");
            }
        }

        private void OnAllReadyChanged(bool allReady)
        {
            if (!IsHost) return;
            if (debugForceHostInteractable) return;
            SetButtonInteractable(allReady);
        }

        private void OnPlayerReadyChanged(uint playerNetId, bool isReady)
        {
            if (IsHost) return;
            if (!CharacterSlotManager.TryGetLocalNetId(out uint myNetId)) return;
            if (playerNetId != myNetId) return;

            SyncClientReadyVisual(isReady);
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
