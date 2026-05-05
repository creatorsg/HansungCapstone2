using UnityEngine;
using System.Collections.Generic;
using System; // Action을 사용하기 위해 필요

namespace Lsy
{
    public class InventoryUI : MonoBehaviour
    {
        public static InventoryUI Instance;

        public GameObject inventoryWindow;
        public Transform slotContainer;
        public GameObject inventorySlotPrefab;

        // ★ 새로 추가: 게임에 존재하는 모든 ItemData를 모아둘 리스트 (번역기 역할)
        [Header("모든 아이템 데이터베이스")]
        public List<ItemData> allItemDatabase = new List<ItemData>();

        private void Awake()
        {
            if (Instance == null) Instance = this;
        }

        public void RefreshInventory()
        {
            if (PlayerAccount.LocalInstance == null || PlayerAccount.LocalInstance.currentSelectedCharacter == null) return;

            CharacterUnit myChar = PlayerAccount.LocalInstance.currentSelectedCharacter;

            // 1. 기존 슬롯 싹 다 지우기
            foreach (Transform child in slotContainer)
            {
                Destroy(child.gameObject);
            }

            slotContainer.DetachChildren();

            foreach (InventoryItem item in myChar.myInventory)
            {
                if (item.amount > 0)
                {
                    GameObject newSlot = Instantiate(inventorySlotPrefab, slotContainer);

                    InventorySlotUI slotScript = newSlot.GetComponent<InventorySlotUI>();

                    if (slotScript != null)
                    {
                        ItemData foundData = allItemDatabase.Find(x => x.itemName == item.itemName);

                        if (foundData != null)
                        {
                            slotScript.Setup(foundData, item.amount, () =>
                            {
                                Debug.Log($"<color=yellow>{foundData.itemName} 아이템 구매완료!</color>");
                                //서버에 구매한 아이템 전달
                            });
                        }
                        else
                        {
                            Debug.LogWarning($"데이터베이스에 '{item.itemName}' 아이템이 없습니다! 인스펙터를 확인해주세요.");
                        }
                    }
                }
            }
        }
    }
}