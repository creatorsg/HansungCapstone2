using UnityEngine;
using System.Collections.Generic;
using Jun;

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
            if (PlayerAccount.LocalInstance == null || PlayerAccount.LocalInstance.currentSelectedCharacter == null) return;

            CharacterUnit myChar = PlayerAccount.LocalInstance.currentSelectedCharacter;
            Debug.Log($"[InventoryUI] 인벤토리 갱신 시작 — 아이템 수: {myChar.myInventory.Count}");

            foreach (Transform child in slotContainer)
                Destroy(child.gameObject);
            slotContainer.DetachChildren();

            foreach (InventoryItem item in myChar.myInventory)
            {
                if (item.amount <= 0) continue;

                // ── ItemManager에서 ItemSO를 itemName 기준으로 조회 ──────────────
                // Mirror SyncList 직렬화 후 ConsumInfo/EquipInfo 내부의 Sprite 등이
                // 손실될 수 있으므로, 표시용 데이터는 항상 ItemSO에서 재구성합니다.
                // (equippedWeaponId → 이름으로 조회하는 방식과 동일한 원리)
                ItemSO so = ItemManager.Instance != null
                    ? ItemManager.Instance.GetItemSO(item.itemName)
                    : null;

                // 아이콘: SyncList 내 데이터 → ItemSO → ItemData 순으로 fallback
                Sprite icon = item.ConsumInfo?.icon ?? item.EquipInfo?.icon ?? so?.icon;
                if (icon == null && ItemManager.Instance != null)
                    icon = ItemManager.Instance.GetIcon(item.itemName);

                // ConsumInfo/EquipInfo: SyncList 내 데이터가 있으면 그대로, 없으면 ItemSO에서 재구성
                ConsumableInfo consumInfo = item.ConsumInfo
                    ?? (item.Type == ItemType.Consumable ? so?.ToConsumableInfo() : null);
                EqpInfo equipInfo = item.EquipInfo
                    ?? (item.Type != ItemType.Consumable ? so?.ToEqpInfo() : null);

                Debug.Log($"[InventoryUI] '{item.itemName}' amount={item.amount} | " +
                          $"SO={so != null} | icon={icon != null} | " +
                          $"consumInfo={consumInfo != null} | equipInfo={equipInfo != null}");

                GameObject newSlot = Instantiate(inventorySlotPrefab, slotContainer);
                InventorySlotUI slotScript = newSlot.GetComponent<InventorySlotUI>();
                if (slotScript == null) continue;

                if (consumInfo != null)
                {
                    slotScript.Setup(consumInfo, item.amount, icon, () =>
                        myChar.CmdEquipConsumableToSlot(item.itemName));
                }
                else if (equipInfo != null)
                {
                    slotScript.Setup(equipInfo, item.amount, icon, () =>
                        myChar.CmdEquipConsumableToSlot(item.itemName));
                }
                else
                {
                    // 최후 fallback: 레거시 ItemData 방식
                    ItemData foundData = ItemManager.Instance != null
                        ? ItemManager.Instance.GetItemData(item.itemName)
                        : allItemDatabase.Find(x => x.itemName == item.itemName);

                    if (foundData == null)
                    {
                        Debug.LogWarning($"[InventoryUI] '{item.itemName}' — ItemSO·ItemData 모두 없어 슬롯 삭제");
                        Destroy(newSlot);
                        continue;
                    }
                    slotScript.Setup(foundData, item.amount, () =>
                    {
                        CharacterShop shop = myChar.GetComponent<CharacterShop>();
                        shop?.CmdEquipItem(foundData.itemName);
                    });
                }

                bool isEquipped = (item.Type == ItemType.Weapon && myChar.equipmentSlot.equippedWeaponId == item.itemName)
                               || (item.Type == ItemType.Armor  && myChar.equipmentSlot.equippedArmorId == item.itemName);
                slotScript.ShowEquipOutline(isEquipped);
            }
        }

    }
}

