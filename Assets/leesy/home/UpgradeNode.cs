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

        [Header("아이콘 이미지 (선택사항 - 비워두면 자식 Image 자동 탐색)")]
        public Image iconImage;

        [Header("해금됐을 때 색상")]
        public Color unlockedColor = Color.white;

        [Header("잠겼을 때 색상 (어둡게)")]
        public Color lockedColor = new Color(0.3f, 0.3f, 0.3f, 1f);

        private Image _buttonImage;

        private void Awake()
        {
            if (nodeButton == null)
                nodeButton = GetComponent<Button>();

            if (nodeButton != null)
            {
                _buttonImage = nodeButton.GetComponent<Image>();
                nodeButton.transition = Selectable.Transition.None;
            }

            if (iconImage == null)
            {
                foreach (var image in GetComponentsInChildren<Image>(true))
                {
                    if (image != _buttonImage)
                    {
                        iconImage = image;
                        break;
                    }
                }
            }

            // 별도 아이콘용 자식 Image가 없으면 버튼 자체 이미지를 아이콘 슬롯으로 사용
            // (스킬 노드 = 스킬 아이콘 버튼. 잠기면 SetNodeState의 lockedColor로 회색 처리됨)
            if (iconImage == null)
                iconImage = _buttonImage;
        }

        public void SetNodeState(bool canUpgrade, int price = 0)
        {
            if (nodeButton != null)
                nodeButton.interactable = canUpgrade;

            if (_buttonImage != null)
                _buttonImage.color = canUpgrade ? unlockedColor : lockedColor;

            if (priceText != null)
                priceText.text = price > 0 ? $"{price}G" : "";
        }

        public void SetIcon(Sprite icon)
        {
            // icon이 null이면 기존 표시 유지(버튼 이미지 폴백 시 버튼이 사라지는 사고 방지)
            if (iconImage == null || icon == null) return;

            iconImage.sprite = icon;
            iconImage.enabled = true;
        }
    }
}
