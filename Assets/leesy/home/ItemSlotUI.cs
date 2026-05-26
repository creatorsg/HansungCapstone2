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
            Sprite icon = itemData != null && itemData.ConsumItem != null ? itemData.ConsumItem.icon : null;
            string itemName = itemData != null && itemData.ConsumItem != null ? itemData.ConsumItem.Name : "Unknown";
            SetupVisual(icon, itemName, currentPrice, onBuyClicked);
        }

        public void Setup(Equipment equipmentData, int currentPrice, Action onBuyClicked)
        {
            Sprite icon = equipmentData != null && equipmentData.EqpItem != null ? equipmentData.EqpItem.icon : null;
            string itemName = equipmentData != null && equipmentData.EqpItem != null ? equipmentData.EqpItem.Name : "Unknown";
            SetupVisual(icon, itemName, currentPrice, onBuyClicked);
        }

        private void SetupVisual(Sprite icon, string itemName, int currentPrice, Action onBuyClicked)
        {
            if (_itemIcon != null)
            {
                _itemIcon.sprite = icon;
                _itemIcon.gameObject.SetActive(icon != null);
            }

            if (_itemNametxt != null)
                _itemNametxt.text = itemName;

            if (_itemPricetxt != null)
                _itemPricetxt.text = $"{currentPrice}G";

            if (_buyButton != null)
            {
                _buyButton.onClick.RemoveAllListeners();
                _buyButton.onClick.AddListener(() => onBuyClicked?.Invoke());
            }
        }
    }
}
