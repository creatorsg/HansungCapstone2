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

                    // [수정] 네트워크로 받은 소모품 아이콘은 null이므로 클라이언트 로컬 데이터를 우선 사용합니다.
                    Jun.ConsumableInfo info =
                        ItemManager.Instance?.GetConsumData(item.itemName)?.ConsumItem
                        ?? item.ConsumInfo;

                    if (info == null)
                    {
                        Debug.LogWarning($"[EquipmentUI] Consumable data not found. slot:{i}, item:{item.itemName}");
                        consumableSlots[i].gameObject.SetActive(false);
                        continue;
                    }

                    Sprite icon = ItemManager.Instance?.GetIcon(item.itemName) ?? info.icon;

                    consumableSlots[i].gameObject.SetActive(true);
                    string capturedName = item.itemName;

                    consumableSlots[i].Setup(info, item.amount, icon, () =>
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
            // [수정] 장착 장비 아이콘은 클라이언트 로컬 ItemManager에서 조회합니다.
            Jun.EqpInfo info = FindEquipmentInfo(myChar.equipmentSlot.equippedWeaponId);

            if (info != null)
            {
                Sprite icon = ItemManager.Instance?.GetIcon(myChar.equipmentSlot.equippedWeaponId) ?? info.icon;
                weaponSlot.Setup(info, 1, icon, () => myChar.CmdUnequipWeapon());
            }
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
            // [수정] 장착 방어구 아이콘은 클라이언트 로컬 ItemManager에서 조회합니다.
            Jun.EqpInfo info = FindEquipmentInfo(myChar.equipmentSlot.equippedArmorId);

            if (info != null)
            {
                Sprite icon = ItemManager.Instance?.GetIcon(myChar.equipmentSlot.equippedArmorId) ?? info.icon;
                armorSlot.Setup(info, 1, icon, () => myChar.CmdUnequipArmor());
            }
            else
                Debug.LogWarning($"[EquipmentUI] Armor data not found: {myChar.equipmentSlot.equippedArmorId}");
        }

        private Jun.EqpInfo FindEquipmentInfo(string equipmentName)
        {
            // [수정] ItemManager.GetEqpData는 EqpInfo를 반환하므로 로컬 Equipment DB fallback과 타입을 분리합니다.
            Jun.EqpInfo data = ItemManager.Instance != null
                ? ItemManager.Instance.GetEqpData(equipmentName)
                : null;

            if (data != null) return data;

            Equipment fallback = allEqpDatabase.Find(x => x != null && x.EqpItem != null && x.EqpItem.Name == equipmentName);
            return fallback != null ? fallback.EqpItem : null;
        }
    }
}
