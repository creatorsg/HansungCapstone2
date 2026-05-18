using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace Lsy
{
    public class InventorySlotUI : MonoBehaviour
    {
        public Image itemIcon;
        public TextMeshProUGUI itemNameText;
        public TextMeshProUGUI itemAmountText;

        public Image equippedMarkImage;
        public Button interactButton;

        public void Setup(ItemData data, int amount, bool isEquipment, bool isEquipped, Action onClickAction)
        {
            if (data != null)
            {
                if (itemNameText != null)
                    itemNameText.text = data.itemName;

                if (itemIcon != null)
                {
                    itemIcon.sprite = data.itemIcon;
                    itemIcon.gameObject.SetActive(true);
                }
            }
            else
            {
                if (itemNameText != null)
                    itemNameText.text = "알 수 없음";
                if (itemIcon != null)
                    itemIcon.gameObject.SetActive(false);
            }

            if (itemAmountText != null)
                itemAmountText.text = isEquipment ? "" : $"x{amount}";

            if (interactButton != null)
            {
                interactButton.onClick.RemoveAllListeners();
                interactButton.interactable = onClickAction != null;
                if (onClickAction != null)
                    interactButton.onClick.AddListener(() => onClickAction.Invoke());
            }

            SetEquippedVisual(isEquipped);
        }

        public void SetEquippedVisual(bool isEquipped)
        {
            if (equippedMarkImage != null)
                equippedMarkImage.gameObject.SetActive(isEquipped);
        }

        // Kept for prefab event compatibility.
        public void ShowItemMark(bool show) => SetEquippedVisual(show);
    }
}
