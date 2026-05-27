using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace Lsy
{
    public class EquipmentSlot : NetworkBehaviour
    {
        [Header("���� ���")]
        [SyncVar] public string equippedWeaponId = "";
        [SyncVar] public string equippedArmorId = "";
        public InventoryItem equippedWeapon;
        public InventoryItem equippedArmor;

        [Header("���� �Ҹ�ǰ (�ִ� 6����)")]
        public readonly SyncList<InventoryItem> equippedConsumables = new SyncList<InventoryItem>();
        private const int MAX_CONSUMABLE_SLOTS = 6;

        [Server]
        public bool EquipWeapon(InventoryItem item)
        {
            equippedWeaponId = item.itemName;
            equippedWeapon = item;
            return true;
        }

        [Server]
        public bool EquipArmor(InventoryItem item)
        {
            equippedArmorId = item.itemName;
            equippedArmor = item;
            return true;
        }

        [Server]
        public bool EquipConsumable(InventoryItem item)
        {
            for (int i = 0; i < equippedConsumables.Count; i++)
            {
                if (equippedConsumables[i].itemName == item.itemName)
                {
                    InventoryItem temp = equippedConsumables[i];
                    temp.amount += item.amount;
                    equippedConsumables[i] = temp;
                    return true;
                }
            }
            if (equippedConsumables.Count >= MAX_CONSUMABLE_SLOTS)
            {
                Debug.LogWarning("�Ҹ�ǰ ���� ������ ���� á���ϴ�. (�ִ� 6��)");
                return false;
            }
            equippedConsumables.Add(item);
            return true;
        }

        [Server]
        public bool UnequipConsumable(string itemName, int amount = 1)
        {
            for (int i = 0; i < equippedConsumables.Count; i++)
            {
                if (equippedConsumables[i].itemName == itemName)
                {
                    InventoryItem temp = equippedConsumables[i];
                    if (temp.amount <= amount)
                        equippedConsumables.RemoveAt(i);
                    else
                    {
                        temp.amount -= amount;
                        equippedConsumables[i] = temp;
                    }
                    return true;
                }
            }
            return false;
        }

        [Server]
        public bool UnequipWeapon()
        {
            if (string.IsNullOrEmpty(equippedWeaponId)) return false;
            equippedWeaponId = "";
            equippedWeapon   = default;   // ← 실제 데이터도 초기화
            return true;
        }

        [Server]
        public bool UnequipArmor()
        {
            if (string.IsNullOrEmpty(equippedArmorId)) return false;
            equippedArmorId = "";
            equippedArmor   = default;    // ← 실제 데이터도 초기화
            return true;
        }


    }
}