using UnityEngine;
using UnityEngine.UI;

namespace Lsy
{
    public class CharacterProfileOutline : MonoBehaviour
    {
        [Header("이 프로필의 캐릭터 인덱스 (직접 입력)")]
        public int characterIndex;

        [Header("아웃라인 색상")]
        public Color outlineColor = Color.green;

        [Header("아웃라인 두께")]
        public Vector2 outlineEffectDistance = new Vector2(3f, -3f);

        private Outline _outline;

        private void Awake()
        {
            _outline = GetComponent<Outline>();
            if (_outline == null)
                Debug.LogWarning($"[CharacterProfileOutline] {gameObject.name}에 Outline 컴포넌트가 없습니다!");

            if (_outline != null)
            {
                _outline.enabled = false;
                _outline.effectColor = outlineColor;
                _outline.effectDistance = outlineEffectDistance;
            }
        }

        private void OnEnable()
        {
            CharacterSlotManager.OnSlotChanged += RefreshOutline;
            ReadySystem.OnPlayerReadyChanged += OnPlayerReadyChanged;
            RefreshOutline();
        }

        private void OnDisable()
        {
            CharacterSlotManager.OnSlotChanged -= RefreshOutline;
            ReadySystem.OnPlayerReadyChanged -= OnPlayerReadyChanged;
        }

        private void OnPlayerReadyChanged(uint playerNetId, bool isReady)
        {
            RefreshOutline();
        }

        private void RefreshOutline()
        {
            if (_outline == null)
                return;

            if (CharacterSlotManager.Instance == null || ReadySystem.Instance == null)
            {
                SetOutline(false);
                return;
            }

            if (!CharacterSlotManager.Instance.TryGetSlotOwner(characterIndex, out uint ownerNetId))
            {
                SetOutline(false);
                return;
            }

            bool ownerReady = ReadySystem.Instance.IsPlayerReady(ownerNetId);
            SetOutline(ownerReady);
        }

        private void SetOutline(bool show)
        {
            _outline.enabled = show;
        }
    }
}
