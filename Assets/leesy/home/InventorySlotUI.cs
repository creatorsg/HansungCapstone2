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

        public Outline itemOutline;
        public Button interactButton;

        public void Setup(ItemData data, int amount, Action onClickAction)
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
                itemAmountText.text = $"x{amount}";

            if (interactButton != null)
            {
                interactButton.onClick.RemoveAllListeners();
                if (onClickAction != null)
                    interactButton.onClick.AddListener(() => onClickAction.Invoke());
            }

            ShowEquipOutline(false);
        }

        public void ShowEquipOutline(bool isEquipped)
        {
            if (itemOutline != null)
                itemOutline.enabled = isEquipped;
        }
    }
}
