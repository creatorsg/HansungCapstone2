using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lsy
{
    public class UpgradeNode : MonoBehaviour
    {
        public Button nodeButton;

        [Header("가격 텍스트 (선택사항 - 없으면 무시됨)")]
        public TextMeshProUGUI priceText;

        [Header("해금됐을 때 색상")]
        public Color unlockedColor = Color.white;

        [Header("잠겼을 때 색상 (어둡게)")]
        public Color lockedColor = new Color(0.3f, 0.3f, 0.3f, 1f);

        // 버튼 배경 이미지 (자동으로 찾음)
        private Image _buttonImage;

        private void Awake()
        {
            // 버튼 자체의 Image 컴포넌트를 가져옴
            _buttonImage = nodeButton.GetComponent<Image>();

            // [중요] 버튼 Transition을 None으로 설정
            // Color Tint 모드면 interactable=false 시 자동으로 반투명해지므로
            // 직접 색상을 제어하기 위해 None으로 변경
            nodeButton.transition = Selectable.Transition.None;
        }

        public void SetNodeState(bool canUpgrade, int price = 0)
        {
            nodeButton.interactable = canUpgrade;

            // 직접 색상 제어
            if (_buttonImage != null)
                _buttonImage.color = canUpgrade ? unlockedColor : lockedColor;

            if (priceText != null)
                priceText.text = price > 0 ? $"{price}G" : "";
        }
    }
}