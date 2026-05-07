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

        private Image _buttonImage;
        private bool _isReady = false;

        private bool IsHost => NetworkServer.active;

        private void Awake()
        {
            _buttonImage = button.GetComponent<Image>();
            button.transition = Selectable.Transition.None;
        }

        private void Start()
        {
            Debug.Log($"<color=cyan>[ReadyOrStartButton] Start - IsHost:{IsHost}</color>");

            if (IsHost)
            {
                buttonText.text = hostReadyText;
                SetButtonInteractable(false);
                if (ReadySystem.Instance != null)
                    SetButtonInteractable(ReadySystem.Instance.AllReady);
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
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClick);
        }

        private void OnDisable()
        {
            ReadySystem.OnAllReadyChanged -= OnAllReadyChanged;
            ReadySystem.OnPlayerReadyChanged -= OnPlayerReadyChanged;
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
            ReadySystem.Instance.CmdToggleReady(myNetId);
        }

        private void OnClickStartGame()
        {
            Debug.Log("[ReadyOrStartButton] 게임 시작!");
        }

        private void OnAllReadyChanged(bool allReady)
        {
            Debug.Log($"<color=magenta>[ReadyOrStartButton] OnAllReadyChanged - allReady:{allReady}, IsHost:{IsHost}</color>");
            if (!IsHost) return;
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
            buttonText.text = _isReady ? clientReadyText : clientNotReadyText;
            if (_buttonImage != null)
                _buttonImage.color = _isReady ? clientReadyColor : activeColor;
        }

        private void SetButtonInteractable(bool interactable)
        {
            button.interactable = interactable;
            if (_buttonImage != null)
                _buttonImage.color = interactable ? activeColor : lockedColor;
            Debug.Log($"<color=cyan>[ReadyOrStartButton] 버튼 상태: {(interactable ? "활성화" : "비활성화")}</color>");
        }
    }
}
