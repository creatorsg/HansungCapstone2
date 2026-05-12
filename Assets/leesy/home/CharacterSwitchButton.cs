using Mirror;
using UnityEngine;
using UnityEngine.UI;

namespace Lsy
{
    public class CharacterSwitchButton : MonoBehaviour
    {
        [Header("전환할 캐릭터 인덱스 (직접 입력)")]
        public int characterIndex;

        [Header("UI 컴포넌트")]
        public Button button;

        [Header("다른 사람이 선점했을 때 색상 (어둡게)")]
        public Color takenColor = new Color(0.3f, 0.3f, 0.3f, 1f);

        [Header("현재 사용 중인 캐릭터 아웃라인 색상")]
        public Color activeOutlineColor = Color.white;

        [Header("아웃라인 두께")]
        public Vector2 outlineEffectDistance = new Vector2(3f, -3f);

        private Image _buttonImage;
        private Color _originalColor;
        private Outline _outline;

        private void Awake()
        {
            _buttonImage = button.GetComponent<Image>();
            button.transition = Selectable.Transition.None;

            if (_buttonImage != null)
                _originalColor = _buttonImage.color;

            _outline = GetComponent<Outline>();
            if (_outline == null)
                Debug.LogWarning($"[CharacterSwitchButton] {gameObject.name}에 Outline 컴포넌트가 없습니다!");
            else
            {
                _outline.enabled = false;
                _outline.effectColor = activeOutlineColor;
                _outline.effectDistance = outlineEffectDistance;
            }
        }

        private void OnEnable()
        {
            CharacterSlotManager.OnSlotChanged += RefreshState;
            PlayerAccount.OnCharacterSwitched += RefreshState;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClickSwitch);
            RefreshState();
        }

        private void OnDisable()
        {
            CharacterSlotManager.OnSlotChanged -= RefreshState;
            PlayerAccount.OnCharacterSwitched -= RefreshState;
        }

        private void OnClickSwitch()
        {
            if (PlayerAccount.LocalInstance == null) return;
            if (CharacterSlotManager.Instance == null) return;

            bool isMySlot = CharacterSlotManager.Instance.IsMySlot(characterIndex);
            if (!isMySlot) return;

            PlayerAccount.LocalInstance.SelectCharacter(characterIndex);
        }

        private void RefreshState()
        {
            if (CharacterSlotManager.Instance == null) return;
            if (PlayerAccount.LocalInstance == null) return;

            bool isMySlot = CharacterSlotManager.Instance.IsMySlot(characterIndex);
            bool isTaken = !isMySlot;

            button.interactable = isMySlot;
            if (_buttonImage != null)
                _buttonImage.color = isTaken ? takenColor : _originalColor;

            bool isActive = PlayerAccount.LocalInstance.currentActiveIndex == characterIndex
                            && characterIndex >= 0;

            if (_outline != null)
                _outline.enabled = isActive;
        }
    }
}
