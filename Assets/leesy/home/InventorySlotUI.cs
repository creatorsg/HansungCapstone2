using Jun;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Lsy
{
    public class InventorySlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public Image itemIcon;
        public TextMeshProUGUI itemNameText;
        public TextMeshProUGUI itemAmountText;

        public Outline itemOutline;
        public Button interactButton;

        // 새로운 아이템 슬롯 그리기
        public void Setup(ConsumableInfo data, int amount, Sprite icon, Action onClickAction)
        {
            if (data != null)
            {
                if (itemNameText != null) itemNameText.text = data.Name;
                if (itemIcon != null)
                {
                    itemIcon.sprite = icon;
                    itemIcon.gameObject.SetActive(icon != null);
                }
            }
            else
            {
                if (itemNameText != null) itemNameText.text = "알 수 없음";
                if (itemIcon != null) itemIcon.gameObject.SetActive(false);
            }
            if (itemAmountText != null) itemAmountText.text = $"x{amount}";
            if (interactButton != null)
            {
                interactButton.onClick.RemoveAllListeners();
                if (onClickAction != null)
                    interactButton.onClick.AddListener(() => onClickAction.Invoke());
            }
            ShowEquipOutline(false);
            if (data != null) SetCurrentItem(new InventoryItem { itemName = data.Name, Type = ItemType.Consumable, ConsumInfo = data, amount = amount });
        }

        public void Setup(EqpInfo data, int amount, Sprite icon, Action onClickAction)
        {
            if (data != null)
            {
                if (itemNameText != null) itemNameText.text = data.Name;
                if (itemIcon != null)
                {
                    itemIcon.sprite = icon;
                    itemIcon.gameObject.SetActive(icon != null);
                }
            }
            else
            {
                if (itemNameText != null) itemNameText.text = "알 수 없음";
                if (itemIcon != null) itemIcon.gameObject.SetActive(false);
            }
            if (itemAmountText != null) itemAmountText.text = $"x{amount}";
            if (interactButton != null)
            {
                interactButton.onClick.RemoveAllListeners();
                if (onClickAction != null)
                    interactButton.onClick.AddListener(() => onClickAction.Invoke());
            }
            ShowEquipOutline(false);
            if (data != null) SetCurrentItem(new InventoryItem { itemName = data.Name, Type = ItemType.Weapon, EquipInfo = data, amount = amount });
        }

        public void ShowEquipOutline(bool isEquipped)
        {
            if (itemOutline != null)
                itemOutline.enabled = isEquipped;
        }

        // ─── 툴팁 호버 ──────────────────────────────────────────────
        private InventoryItem _currentItem;

        /// <summary>InventoryItem 전체를 저장해 두어 hover 시 툴팁에 전달합니다.</summary>
        public void SetCurrentItem(InventoryItem item)
        {
            _currentItem = item;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (ItemTooltipUI.Instance != null)
                ItemTooltipUI.Instance.Show(_currentItem);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (ItemTooltipUI.Instance != null)
                ItemTooltipUI.Instance.Hide();
        }
    }
}
