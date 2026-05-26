using UnityEngine;
using System.Collections.Generic;

namespace Lsy
{
    public class EquipmentUI : MonoBehaviour
    {
        public static EquipmentUI Instance;

        [Header("미리 만들어둔 장착 슬롯들")]
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

            // 소모품 슬롯
            for (int i = 0; i < consumableSlots.Length; i++)
            {
                if (i < myChar.equipmentSlot.equippedConsumables.Count)
                {
                    InventoryItem item = myChar.equipmentSlot.equippedConsumables[i];

                    // ConsumInfo null 체크
                    if (item.ConsumInfo == null)
                    {
                        Debug.LogWarning($"[EquipmentUI] 소모품 슬롯 {i}  ConsumInfo가 null입니다. ({item.itemName})");
                        consumableSlots[i].gameObject.SetActive(false);
                        continue;
                    }

                    consumableSlots[i].gameObject.SetActive(true);

                    // 클로저 캡처 버그 방지: 루프 변수 로컬 복사
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

            // 무기 슬롯
            if (string.IsNullOrEmpty(myChar.equipmentSlot.equippedWeaponId))
            {
                weaponSlot.gameObject.SetActive(false);
            }
            else
            {
                weaponSlot.gameObject.SetActive(true);
                InventoryItem w = myChar.equipmentSlot.equippedWeapon;

                if (w.EquipInfo != null)
                {
                    weaponSlot.Setup(w.EquipInfo, 1, w.EquipInfo.icon, () => myChar.CmdUnequipWeapon());
                }
                else
                {
                    // null 가드 추가
                    ItemData fallback = allItemDatabase.Find(x => x != null && x.itemName == myChar.equipmentSlot.equippedWeaponId);
                    if (fallback != null)
                        weaponSlot.Setup(fallback, 1, () => myChar.CmdUnequipWeapon());
                    else
                        Debug.LogWarning($"[EquipmentUI] 무기 '{myChar.equipmentSlot.equippedWeaponId}'  EquipInfo도 없고 ItemData도 없습니다.");
                }
            }

            // 방어구 슬롯 
            if (string.IsNullOrEmpty(myChar.equipmentSlot.equippedArmorId))
            {
                armorSlot.gameObject.SetActive(false);
            }
            else
            {
                armorSlot.gameObject.SetActive(true);
                InventoryItem a = myChar.equipmentSlot.equippedArmor;

                if (a.EquipInfo != null)
                {
                    armorSlot.Setup(a.EquipInfo, 1, a.EquipInfo.icon, () => myChar.CmdUnequipArmor());
                }
                else
                {
                    // null 가드 추가
                    ItemData fallback = allItemDatabase.Find(x => x != null && x.itemName == myChar.equipmentSlot.equippedArmorId);
                    if (fallback != null)
                        armorSlot.Setup(fallback, 1, () => myChar.CmdUnequipArmor());
                    else
                        Debug.LogWarning($"[EquipmentUI] 방어구 '{myChar.equipmentSlot.equippedArmorId}'  EquipInfo도 없고 ItemData도 없습니다.");
                }
            }
        }
    }
}