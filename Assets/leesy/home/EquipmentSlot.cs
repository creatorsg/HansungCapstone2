using System;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace Lsy
{
    public class EquipmentSlot : NetworkBehaviour
    {
        /// <summary>장비 ID / 소모품 슬롯이 변경될 때 클라이언트에서 발화 (UI 갱신용)</summary>
        public static event Action OnEquipmentChanged;

        [Header("장착 장비")]
        [SyncVar(hook = nameof(OnWeaponIdChanged))]
        public string equippedWeaponId = "";

        [SyncVar(hook = nameof(OnArmorIdChanged))]
        public string equippedArmorId = "";

        // 서버 전용 상세 데이터 (SyncVar 불가 — 클라이언트는 ID로 ItemManager 조회)
        public InventoryItem equippedWeapon;
        public InventoryItem equippedArmor;

        [Header("소모품 슬롯 (최대 6)")]
        public readonly SyncList<InventoryItem> equippedConsumables = new SyncList<InventoryItem>();
        private const int MAX_CONSUMABLE_SLOTS = 6;

        // ── SyncVar 훅 ────────────────────────────────────────────
        private void OnWeaponIdChanged(string oldVal, string newVal) => OnEquipmentChanged?.Invoke();
        private void OnArmorIdChanged (string oldVal, string newVal) => OnEquipmentChanged?.Invoke();

        public override void OnStartClient()
        {
            base.OnStartClient();
            // 소모품 슬롯 변경도 UI 갱신 트리거
            equippedConsumables.Callback -= OnConsumablesChanged;
            equippedConsumables.Callback += OnConsumablesChanged;
        }

        public override void OnStopClient()
        {
            base.OnStopClient();
            equippedConsumables.Callback -= OnConsumablesChanged;
        }

        private void OnConsumablesChanged(SyncList<InventoryItem>.Operation op, int index,
                                          InventoryItem oldItem, InventoryItem newItem)
        {
            OnEquipmentChanged?.Invoke();
        }

        // ── 서버 전용 메서드 ─────────────────────────────────────
        [Server]
        public bool EquipWeapon(InventoryItem item)
        {
            equippedWeaponId = item.itemName;
            equippedWeapon   = item;
            return true;
        }

        [Server]
        public bool EquipArmor(InventoryItem item)
        {
            equippedArmorId = item.itemName;
            equippedArmor   = item;
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
                Debug.LogWarning("소모품 슬롯 최대 초과. (최대 6)");
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
            equippedWeapon   = default;
            return true;
        }

        [Server]
        public bool UnequipArmor()
        {
            if (string.IsNullOrEmpty(equippedArmorId)) return false;
            equippedArmorId = "";
            equippedArmor   = default;
            return true;
        }
    }
}
