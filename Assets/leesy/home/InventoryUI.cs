using UnityEngine;
using System.Collections.Generic;

namespace Lsy
{
    public class InventoryUI : MonoBehaviour
    {
        public static InventoryUI Instance;

        public GameObject inventoryWindow;
        public Transform slotContainer;
        public GameObject inventorySlotPrefab;

        // ItemManager로 조회하지 못할 경우를 대비한 폴백 목록
        // ItemManager.allItems에 아이템이 모두 등록되어 있다면 비워도 됩니다
        [Header("아이템 DB 폴백 (ItemManager 미사용 시)")]
        public List<ItemData> allItemDatabase = new List<ItemData>();

        private void Awake()
        {
            if (Instance == null) Instance = this;
        }

        private void OnEnable()
        {
            CharacterUnit.OnLocalInventoryChanged += RefreshInventory;
        }

        private void OnDisable()
        {
            CharacterUnit.OnLocalInventoryChanged -= RefreshInventory;
        }

        public void RefreshInventory()
        {
            // 서버에서 받아온다: 현재 캐릭터 인벤토리/장착 상태(myInventory, selectedWeaponId)
            if (PlayerAccount.LocalInstance == null || PlayerAccount.LocalInstance.currentSelectedCharacter == null) return;

            CharacterUnit myChar = PlayerAccount.LocalInstance.currentSelectedCharacter;

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
                        ItemData foundData = ItemManager.Instance != null
                            ? ItemManager.Instance.GetItemData(item.itemName)
                            : allItemDatabase.Find(x => x.itemName == item.itemName);

                        if (foundData != null)
                        {
                            bool isEquipped = myChar.selectedWeaponId == foundData.itemName;

                            slotScript.Setup(foundData, item.amount, () =>
                            {
                                CharacterShop shop = myChar.GetComponent<CharacterShop>();
                                if (shop == null)
                                {
                                    Debug.LogWarning("[InventoryUI] CharacterShop 컴포넌트를 찾지 못했습니다.");
                                    return;
                                }

                                Debug.Log($"<color=yellow>{foundData.itemName} 장착 요청</color>");
                                // 서버로 보낸다: 장착 요청 아이템(itemName)
                                shop.CmdEquipItem(foundData.itemName);
                            });

                            slotScript.ShowItemMark(isEquipped);
                        }
                        else
                        {
                            Debug.LogWarning($"[InventoryUI] '{item.itemName}' 아이템을 찾지 못했습니다. ItemManager.allItems를 확인해주세요.");
                        }
                    }
                }
            }
        }
    }
}
