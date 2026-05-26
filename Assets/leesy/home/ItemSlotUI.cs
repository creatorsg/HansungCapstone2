using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Lsy
{
    public class ItemSlotUI : MonoBehaviour
    {

        [SerializeField] private Image _itemIcon;
        [SerializeField] private TextMeshProUGUI _itemNametxt;
        [SerializeField] private TextMeshProUGUI _itemPricetxt;
        [SerializeField] private Button _buyButton;

        public void Setup(Consum itemData, int currentPrice, Action onBuyClicked)
        {
            // 1. 아이템 데이터 시각화
            if (_itemIcon != null && itemData.ConsumItem.icon != null)
            {
                _itemIcon.sprite = itemData.ConsumItem.icon;
            }

            if (_itemNametxt != null)
            {
                _itemNametxt.text = itemData.ConsumItem.Name;
            }

            if (_itemPricetxt != null)
            {
                // 올려주신 스크린샷처럼 "50G", "1000G" 형태로 표기
                _itemPricetxt.text = $"{currentPrice}G";
            }

            // 2. 구매 버튼 이벤트 연결
            if (_buyButton != null)
            {
                // 프리팹을 파괴/생성하지 않고 오브젝트 풀링으로 재사용할 경우를 대비해 기존 리스너 초기화
                _buyButton.onClick.RemoveAllListeners();

                // 버튼 클릭 시 매개변수로 받아온 콜백(NPCPopupUI의 OnBuyItemClicked)을 실행
                _buyButton.onClick.AddListener(() =>
                {
                    onBuyClicked?.Invoke();
                });
            }
        }
    }
}