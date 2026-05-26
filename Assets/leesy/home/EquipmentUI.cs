using UnityEngine;
using System.Collections.Generic;

namespace Lsy
{
    public class EquipmentUI : MonoBehaviour
    {
        public static EquipmentUI Instance;

        [Header("Equipment slots")]
        public InventorySlotUI weaponSlot;
        public InventorySlotUI armorSlot;
        public InventorySlotUI[] consumableSlots;
        public List<Equipment> allEqpDatabase = new List<Equipment>();

        private void Awake()
        {
            if (Instance == null) Instance = this;
        }

        private void OnEnable()
        {
            CharacterUnit.OnLocalInventoryChanged += RefreshAllUI;
            RefreshAllUI();
        }

        private void OnDisable()
        {
            CharacterUnit.OnLocalInventoryChanged -= RefreshAllUI;
        }

        public void RefreshAllUI()
        {
            if (PlayerAccount.LocalInstance == null || PlayerAccount.LocalInstance.currentSelectedCharacter == null) return;

            CharacterUnit myChar = PlayerAccount.LocalInstance.currentSelectedCharacter;
            if (myChar.equipmentSlot == null) return;

            for (int i = 0; i < consumableSlots.Length; i++)
            {
                if (i < myChar.equipmentSlot.equippedConsumables.Count)
                {
                    InventoryItem item = myChar.equipmentSlot.equippedConsumables[i];

                    if (item.ConsumInfo == null)
                    {
                        Debug.LogWarning($"[EquipmentUI] ConsumInfo is null. slot:{i}, item:{item.itemName}");
                        consumableSlots[i].gameObject.SetActive(false);
                        continue;
                    }

                    consumableSlots[i].gameObject.SetActive(true);
                    string capturedName = item.itemName;
                    Jun.ConsumableInfo capturedInfo = item.ConsumInfo;

                    consumableSlots[i].Setup(capturedInfo, item.amount, capturedInfo.icon, () =>
                    {
                        myChar.CmdUnequipConsumableFromSlot(capturedName);
                    });
                }
                else
                {
                    consumableSlots[i].gameObject.SetActive(false);
                }
            }

            RefreshWeaponSlot(myChar);
            RefreshArmorSlot(myChar);
        }

        private void RefreshWeaponSlot(CharacterUnit myChar)
        {
            if (weaponSlot == null) return;

            if (string.IsNullOrEmpty(myChar.equipmentSlot.equippedWeaponId))
            {
                weaponSlot.gameObject.SetActive(false);
                return;
            }

            weaponSlot.gameObject.SetActive(true);
            InventoryItem item = myChar.equipmentSlot.equippedWeapon;
            Jun.EqpInfo info = item.EquipInfo ?? FindEquipmentInfo(myChar.equipmentSlot.equippedWeaponId);

            if (info != null)
                weaponSlot.Setup(info, 1, info.icon, () => myChar.CmdUnequipWeapon());
            else
                Debug.LogWarning($"[EquipmentUI] Weapon data not found: {myChar.equipmentSlot.equippedWeaponId}");
        }

        private void RefreshArmorSlot(CharacterUnit myChar)
        {
            if (armorSlot == null) return;

            if (string.IsNullOrEmpty(myChar.equipmentSlot.equippedArmorId))
            {
                armorSlot.gameObject.SetActive(false);
                return;
            }

            armorSlot.gameObject.SetActive(true);
            InventoryItem item = myChar.equipmentSlot.equippedArmor;
            Jun.EqpInfo info = item.EquipInfo ?? FindEquipmentInfo(myChar.equipmentSlot.equippedArmorId);

            if (info != null)
                armorSlot.Setup(info, 1, info.icon, () => myChar.CmdUnequipArmor());
            else
                Debug.LogWarning($"[EquipmentUI] Armor data not found: {myChar.equipmentSlot.equippedArmorId}");
        }

        private Jun.EqpInfo FindEquipmentInfo(string equipmentName)
        {
            Equipment data = ItemManager.Instance != null
                ? ItemManager.Instance.GetEqpData(equipmentName)
                : allEqpDatabase.Find(x => x != null && x.EqpItem != null && x.EqpItem.Name == equipmentName);

            return data != null ? data.EqpItem : null;
        }
    }
}
