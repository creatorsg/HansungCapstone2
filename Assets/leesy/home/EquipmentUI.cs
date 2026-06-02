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

                    Sprite consumIcon = capturedInfo.icon;
                    if (consumIcon == null && ItemManager.Instance != null)
                        consumIcon = ItemManager.Instance.GetIcon(capturedName);
                    consumableSlots[i].Setup(capturedInfo, item.amount, consumIcon, () =>
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
            // equippedWeapon은 서버 전용 필드라 클라이언트에서 항상 default → ID로 조회
            Jun.EqpInfo info = FindEquipmentInfo(myChar.equipmentSlot.equippedWeaponId);

            if (info != null)
            {
                Sprite weaponIcon = info.icon ?? ItemManager.Instance?.GetIcon(myChar.equipmentSlot.equippedWeaponId);
                weaponSlot.Setup(info, 1, weaponIcon, () => myChar.CmdUnequipWeapon());
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
            // equippedArmor는 서버 전용 필드라 클라이언트에서 항상 default → ID로 조회
            Jun.EqpInfo info = FindEquipmentInfo(myChar.equipmentSlot.equippedArmorId);

            if (info != null)
            {
                Sprite armorIcon = info.icon ?? ItemManager.Instance?.GetIcon(myChar.equipmentSlot.equippedArmorId);
                armorSlot.Setup(info, 1, armorIcon, () => myChar.CmdUnequipArmor());
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
