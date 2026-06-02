using UnityEngine;
using Jun;

namespace Lsy
{
    public class InventoryUI : MonoBehaviour
    {
        public static InventoryUI Instance;

        public GameObject inventoryWindow;
        public Transform slotContainer;
        public GameObject inventorySlotPrefab;

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

                // [수정] ItemSO/ItemData fallback 제거. InventoryItem과 ItemManager의 NPC 판매 데이터만 사용합니다.
                ConsumableInfo consumInfo = item.ConsumInfo;
                EqpInfo equipInfo = item.EquipInfo;

                if (consumInfo == null && item.Type == ItemType.Consumable && ItemManager.Instance != null)
                {
                    Consum consumData = ItemManager.Instance.GetConsumData(item.itemName);
                    consumInfo = consumData != null ? consumData.ConsumItem : null;
                }
                
                if (consumInfo != null && consumInfo.icon == null && ItemManager.Instance != null)
                {
                    Consum consumData = ItemManager.Instance.GetConsumData(item.itemName);
                    if (consumData?.ConsumItem != null)
                        consumInfo.icon = consumData.ConsumItem.icon;
                }
                if (equipInfo == null && item.Type != ItemType.Consumable && ItemManager.Instance != null)
                    equipInfo = ItemManager.Instance.GetEqpData(item.itemName);

                // 클라이언트에서 네트워크 역직렬화 시 icon은 항상 null → ItemManager로 복원
                if (equipInfo != null && equipInfo.icon == null && ItemManager.Instance != null)
                {
                    Sprite restored = ItemManager.Instance.GetIcon(item.itemName);
                    if (restored != null) equipInfo.icon = restored;
                }

                Sprite icon = consumInfo?.icon ?? equipInfo?.icon;
                if (icon == null && ItemManager.Instance != null)
                    icon = ItemManager.Instance.GetIcon(item.itemName);

                Debug.Log($"[InventoryUI] '{item.itemName}' amount={item.amount} | " +
                          $"icon={icon != null} | consumInfo={consumInfo != null} | equipInfo={equipInfo != null}");

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
                    Debug.LogWarning($"[InventoryUI] '{item.itemName}' — Consum/Equipment 데이터가 없어 슬롯 삭제");
                    Destroy(newSlot);
                    continue;
                }

                bool isEquipped = (item.Type == ItemType.Weapon && myChar.equipmentSlot.equippedWeaponId == item.itemName)
                               || (item.Type == ItemType.Armor  && myChar.equipmentSlot.equippedArmorId == item.itemName);
                slotScript.ShowEquipOutline(isEquipped);
            }
        }

    }
}

