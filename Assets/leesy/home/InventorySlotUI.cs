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

        public GameObject itemMark;
        public Button interactButton;

        public void Setup(ItemData data, int amount, Action onClickAction)
        {
            // 1. 아이템 데이터 연동
            if (data != null)
            {
                itemNameText.text = data.itemName;

                // ItemData에 스프라이트 변수가 있다면 연결
                itemIcon.sprite = data.itemIcon; 
                itemIcon.gameObject.SetActive(true);
            }
            else
            {
                itemNameText.text = "알 수 없음";
                itemIcon.gameObject.SetActive(false);
            }

            // 2. 소지 개수 텍스트 업데이트
            itemAmountText.text = $"x{amount}";

            // 3. 기존 버튼 이벤트 초기화 후 새로운 이벤트(장착/사용) 연결
            interactButton.onClick.RemoveAllListeners();
            if (onClickAction != null)
            {
                interactButton.onClick.AddListener(() => onClickAction.Invoke());
            }

            ShowItemMark(false);
        }
        public void ShowItemMark(bool isEquipped)
        {
            if (itemMark != null)
            {
                itemMark.SetActive(isEquipped);
            }
        }
    }
}