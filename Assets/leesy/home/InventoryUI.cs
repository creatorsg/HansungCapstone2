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
        private readonly HashSet<string> _equipmentIds = new HashSet<string>();
        private readonly Dictionary<string, string> _equipKeyToWeaponId = new Dictionary<string, string>();
        private CharacterUnit _lastCharacter;
        private string _visualEquippedItemKey = null;

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
            // 서버에서 받아온다: 현재 캐릭터 인벤토리 상태(myInventory)
            if (PlayerAccount.LocalInstance == null || PlayerAccount.LocalInstance.currentSelectedCharacter == null) return;

            CharacterUnit myChar = PlayerAccount.LocalInstance.currentSelectedCharacter;
            if (_lastCharacter != myChar)
            {
                _lastCharacter = myChar;
                _visualEquippedItemKey = null;
            }
            RefreshEquipmentIds();

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
                            bool isEquipment = IsEquipmentItem(item.itemName);
                            bool isEquipped = isEquipment && _visualEquippedItemKey == item.itemName;

                            if (isEquipment)
                            {
                                slotScript.Setup(foundData, item.amount, true, isEquipped, () =>
                                {
                                    CharacterShop shop = myChar.GetComponent<CharacterShop>();
                                    if (shop == null)
                                    {
                                        Debug.LogWarning("[InventoryUI] CharacterShop 컴포넌트를 찾지 못했습니다.");
                                        return;
                                    }

                                    if (_visualEquippedItemKey == item.itemName) _visualEquippedItemKey = null;
                                    else _visualEquippedItemKey = item.itemName;

                                    shop.CmdEquipItem(item.itemName);
                                    RefreshInventory();
                                });
                            }
                            else
                            {
                                slotScript.Setup(foundData, item.amount, false, false, null);
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"[InventoryUI] '{item.itemName}' 아이템을 찾지 못했습니다. ItemManager.allItems를 확인해주세요.");
                        }
                    }
                }
            }
        }

        private void RefreshEquipmentIds()
        {
            _equipmentIds.Clear();
            _equipKeyToWeaponId.Clear();

            var rows = FindObjectsByType<BlacksmithWeaponRow>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var row in rows)
            {
                if (row == null || row.weaponData == null) continue;
                if (string.IsNullOrWhiteSpace(row.weaponData.weaponId)) continue;
                AddEquipmentKey(row.weaponData.weaponId, row.weaponData.weaponId);
                if (!string.IsNullOrWhiteSpace(row.weaponData.weaponName))
                    AddEquipmentKey(row.weaponData.weaponName, row.weaponData.weaponId);
                if (!string.IsNullOrWhiteSpace(row.weaponData.inventoryItemName))
                    AddEquipmentKey(row.weaponData.inventoryItemName, row.weaponData.weaponId);
            }
        }

        private void AddEquipmentKey(string key, string weaponId)
        {
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(weaponId)) return;
            _equipmentIds.Add(key);
            _equipKeyToWeaponId[key] = weaponId;
        }

        private bool IsEquipmentItem(string itemName)
        {
            if (string.IsNullOrWhiteSpace(itemName)) return false;
            return _equipmentIds.Contains(itemName);
        }

    }
}

