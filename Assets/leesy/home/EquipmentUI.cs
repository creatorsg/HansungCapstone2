using UnityEngine;
using System.Collections.Generic;

namespace Lsy
{
    public class EquipmentUI : MonoBehaviour
    {
        public static EquipmentUI Instance;

        [Header("¹Ì¸® ¸¸µé¾îµÐ ÀåÂø ½½·Ôµé")]
        public InventorySlotUI weaponSlot;
        public InventorySlotUI armorSlot;
        public InventorySlotUI[] consumableSlots;

        public List<ItemData> allItemDatabase = new List<ItemData>();

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

            // ¼Ò¸ðÇ° ½½·Ô
            for (int i = 0; i < consumableSlots.Length; i++)
            {
                if (i < myChar.equipmentSlot.equippedConsumables.Count)
                {
                    InventoryItem item = myChar.equipmentSlot.equippedConsumables[i];
                    consumableSlots[i].gameObject.SetActive(true);

                    if (item.ConsumInfo != null)
                    {
                        consumableSlots[i].Setup(item.ConsumInfo, item.amount, item.ConsumInfo.icon, () =>
                        {
                            myChar.CmdUnequipConsumableFromSlot(item.itemName);
                        });
                    }
                }
                else
                {
                    consumableSlots[i].gameObject.SetActive(false);
                }
            }

            // ¹«±â ½½·Ô
            if (string.IsNullOrEmpty(myChar.equipmentSlot.equippedWeaponId))
            {
                weaponSlot.gameObject.SetActive(false);
            }
            else
            {
                weaponSlot.gameObject.SetActive(true);
                InventoryItem w = myChar.equipmentSlot.equippedWeapon;
                if (w.EquipInfo != null)
                    weaponSlot.Setup(w.EquipInfo, 1, w.EquipInfo.icon, () => myChar.CmdUnequipWeapon());
                else
                {
                    ItemData fallback = allItemDatabase.Find(x => x.itemName == myChar.equipmentSlot.equippedWeaponId);
                    if (fallback != null)
                        weaponSlot.Setup(fallback, 1, () => myChar.CmdUnequipWeapon());
                }
            }

            // ¹æ¾î±¸ ½½·Ô
            if (string.IsNullOrEmpty(myChar.equipmentSlot.equippedArmorId))
            {
                armorSlot.gameObject.SetActive(false);
            }
            else
            {
                armorSlot.gameObject.SetActive(true);
                InventoryItem a = myChar.equipmentSlot.equippedArmor;
                if (a.EquipInfo != null)
                    armorSlot.Setup(a.EquipInfo, 1, a.EquipInfo.icon, () => myChar.CmdUnequipArmor());
                else
                {
                    ItemData fallback = allItemDatabase.Find(x => x.itemName == myChar.equipmentSlot.equippedArmorId);
                    if (fallback != null)
                        armorSlot.Setup(fallback, 1, () => myChar.CmdUnequipArmor());
                }
            }
        }
    }
}