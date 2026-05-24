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
            RefreshInventory();
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
            Debug.Log($"[InventoryUI] 인벤토리 갱신 시작 — 아이템 수: {myChar.myInventory.Count}");

            foreach (Transform child in slotContainer)
            {
                Destroy(child.gameObject);
            }

            slotContainer.DetachChildren();

            foreach (InventoryItem item in myChar.myInventory)
            {
                Debug.Log($"[InventoryUI] 아이템: {item.itemName} / amount: {item.amount} / ConsumInfo: {(item.ConsumInfo != null ? item.ConsumInfo.Name : "null")}");

                if (item.amount > 0)
                {
                    GameObject newSlot = Instantiate(inventorySlotPrefab, slotContainer);

                    InventorySlotUI slotScript = newSlot.GetComponent<InventorySlotUI>();

                    if (slotScript == null) continue;

                    // ① ConsumableInfo가 있으면 그걸로 바로 표시 (ItemData 조회 불필요)
                    if (item.ConsumInfo != null)
                    {
                        // 아이콘: ConsumInfo.icon 우선, null이면 ItemData에서 폴백
                        Sprite icon = item.ConsumInfo.icon;
                        if (icon == null)
                        {
                            ItemData fallback = ItemManager.Instance != null
                                ? ItemManager.Instance.GetItemData(item.itemName)
                                : allItemDatabase.Find(x => x.itemName == item.itemName);
                            icon = fallback?.itemIcon;
                        }

                        slotScript.Setup(item.ConsumInfo, item.amount, icon, () =>
                        {
                            CharacterShop shop = myChar.GetComponent<CharacterShop>();
                            if (shop == null) return;
                            shop.CmdEquipItem(item.itemName);
                        });
                    }
                    // ② ConsumableInfo 없는 경우 (장비 등) — 기존 ItemData 방식
                    else
                    {
                        ItemData foundData = ItemManager.Instance != null
                            ? ItemManager.Instance.GetItemData(item.itemName)
                            : allItemDatabase.Find(x => x.itemName == item.itemName);

                        if (foundData == null)
                        {
                            Debug.LogWarning($"[InventoryUI] '{item.itemName}' — ConsumInfo도 없고 ItemData도 없습니다.");
                            Destroy(newSlot);
                            continue;
                        }

                        bool isEquipped = myChar.selectedWeaponId == foundData.itemName;
                        slotScript.Setup(foundData, item.amount, () =>
                        {
                            CharacterShop shop = myChar.GetComponent<CharacterShop>();
                            if (shop == null) return;
                            shop.CmdEquipItem(foundData.itemName);
                        });
                        slotScript.ShowEquipOutline(isEquipped);
                    }
                }
            }
        }
    }
}

