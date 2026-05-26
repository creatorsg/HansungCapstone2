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
        public List<Consum> allItemDatabase = new List<Consum>();
        public List<Equipment> allEqpDatabase = new List<Equipment>();
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

            for (int i = 0; i < myChar.myInventory.Count; i++)
            {
                InventoryItem item = myChar.myInventory[i];
                // ① 역조회 — Type으로 먼저 구분
                Jun.ConsumableInfo consumInfo = item.ConsumInfo;
                Jun.EqpInfo equipInfo = item.EquipInfo;

                if (consumInfo == null && equipInfo == null)
                {
                    // item.Type으로 어느 딕셔너리를 볼지 결정
                    if (item.Type == ItemType.Weapon || item.Type == ItemType.Armor)
                    {
                        Equipment eqpData = ItemManager.Instance != null
                            ? ItemManager.Instance.GetEqpData(item.itemName)
                            : allEqpDatabase.Find(x => x != null && x.EqpItem != null && x.EqpItem.Name == item.itemName);

                        if (eqpData != null)
                            equipInfo = eqpData.EqpItem;
                        else
                            Debug.LogWarning($"[InventoryUI] Equipment '{item.itemName}' DB에 없습니다.");
                    }
                    else  // Consumable
                    {
                        Consum consumData = ItemManager.Instance != null
                            ? ItemManager.Instance.GetItemData(item.itemName)
                            : allItemDatabase.Find(x => x != null && x.ConsumItem != null && x.ConsumItem.Name == item.itemName);

                        if (consumData != null)
                            consumInfo = consumData.ConsumItem;
                        else
                            Debug.LogWarning($"[InventoryUI] Consum '{item.itemName}'  DB에 없습니다.");
                    }
                }

                Debug.Log($"[InventoryUI] 아이템: {item.itemName} / amount: {item.amount} / ConsumInfo: {(consumInfo != null ? consumInfo.Name : "null")} / EquipInfo: {(equipInfo != null ? equipInfo.Name : "null")}");

                // ② amount가 0 이하면 스킵
                if (item.amount <= 0) continue;

                // ③ ConsumInfo / EquipInfo 둘 다 없으면 스킵
                if (consumInfo == null && equipInfo == null)
                {
                    Debug.LogWarning($"[InventoryUI] '{item.itemName}' — ConsumInfo도 없고 EquipInfo도 없습니다. 슬롯 생성 스킵.");
                    continue;
                }

                GameObject newSlot = Instantiate(inventorySlotPrefab, slotContainer);
                InventorySlotUI slotScript = newSlot.GetComponent<InventorySlotUI>();

                if (slotScript == null)
                {
                    Destroy(newSlot);
                    continue;
                }

                // ④ 아이콘 결정 — ConsumInfo / EquipInfo 분리하여 각각 폴백 처리
                Sprite icon = null;

                if (consumInfo != null)
                {
                    icon = consumInfo.icon;
                    if (icon == null)
                    {
                        Consum fallback = ItemManager.Instance != null
                            ? ItemManager.Instance.GetItemData(consumInfo.Name)
                            : allItemDatabase.Find(x => x != null && x.ConsumItem != null && x.ConsumItem.Name == consumInfo.Name);
                        icon = fallback?.ConsumItem.icon;
                    }
                }
                else if (equipInfo != null)
                {
                    icon = equipInfo.icon;
                    if (icon == null)
                    {
                        Equipment fallback = ItemManager.Instance != null
                            ? ItemManager.Instance.GetEqpData(equipInfo.Name)
                            : allEqpDatabase.Find(x => x != null && x.EqpItem != null && x.EqpItem.Name == equipInfo.Name);
                        icon = fallback?.EqpItem.icon;
                    }
                }

                if (icon == null)
                    Debug.LogWarning($"[InventoryUI] '{item.itemName}' — 아이콘을 찾을 수 없습니다.");

                // ⑤ 슬롯 셋업
                if (consumInfo != null)
                {
                    slotScript.Setup(consumInfo, item.amount, icon, () =>
                    {
                        myChar.CmdEquipConsumableToSlot(item.itemName);
                    });
                }
                else
                {
                    slotScript.Setup(equipInfo, item.amount, icon, () =>
                    {
                        myChar.CmdEquipConsumableToSlot(item.itemName);
                    });
                }

                // ⑥ 장착 여부 아웃라인
                bool isEquipped = (item.Type == ItemType.Weapon && myChar.equipmentSlot.equippedWeaponId == item.itemName)
                               || (item.Type == ItemType.Armor && myChar.equipmentSlot.equippedArmorId == item.itemName);
                slotScript.ShowEquipOutline(isEquipped);
            }
        }

    }
}

