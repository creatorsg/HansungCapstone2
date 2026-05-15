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
        public Vector2 outlineEffectDistance = new Vector2(5f, -5f);

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
            PlayerAccount.OnLocalAccountReady += OnAccountReady;
            PlayerAccount.OnCharacterSwitched += RefreshState;

            // RemoveAllListeners 대신 중복 방지 후 추가 — CharacterPortraitUI 등 다른
            // 리스너를 지우지 않도록 한다.
            button.onClick.RemoveListener(OnClickSwitch);
            button.onClick.AddListener(OnClickSwitch);

            if (PlayerAccount.LocalInstance != null)
                RefreshState();
        }

        private void OnDisable()
        {
            PlayerAccount.OnLocalAccountReady -= OnAccountReady;
            PlayerAccount.OnCharacterSwitched -= RefreshState;
            button.onClick.RemoveListener(OnClickSwitch);
        }

        private void OnAccountReady(PlayerAccount account)
        {
            RefreshState();
        }

        private void OnClickSwitch()
        {
            var account = PlayerAccount.LocalInstance;
            if (account == null) return;

            // characterIndex는 슬롯 인덱스(heroPos). SelectCharacter는 프로필 인덱스를 요구하므로
            // myHeroPositions에서 해당 슬롯의 프로필 인덱스를 조회한다.
            int profileIndex = account.myHeroPositions.IndexOf(characterIndex);
            if (profileIndex < 0) return; // 내 캐릭터가 아니면 무시

            account.SelectCharacter(profileIndex);
        }

        private void RefreshState()
        {
            var account = PlayerAccount.LocalInstance;
            if (account == null) return;

            // myHeroPositions 기반 소유 판정 — CharacterPortraitUI와 동일한 기준 사용
            bool isMySlot = account.myHeroPositions.Contains(characterIndex);

            // 버튼 인터랙션: 내 슬롯이 아니면 비활성
            // (CharacterPortraitUI.RefreshOwnership과 함께 동작하므로 interactable은 여기선 건드리지 않음)

            if (_buttonImage != null)
                _buttonImage.color = isMySlot ? _originalColor : takenColor;

            // 현재 활성 캐릭터 아웃라인 표시
            // currentActiveIndex는 프로필 인덱스, myHeroPositions[profileIdx] = slotIndex
            bool isActive = false;
            if (isMySlot && account.currentActiveIndex >= 0
                         && account.currentActiveIndex < account.myHeroPositions.Count)
            {
                isActive = account.myHeroPositions[account.currentActiveIndex] == characterIndex;
            }

            if (_outline != null)
                _outline.enabled = isActive;
        }
    }
}


