using System.Collections.Generic;
using UnityEngine;

namespace Lsy
{
    public class ItemManager : MonoBehaviour
    {
        public static ItemManager Instance { get; private set; }

        public List<Consum> AllItems = new List<Consum>();
        public List<Equipment> AllEqps = new List<Equipment>();

        private readonly Dictionary<string, Consum> _consumDict = new Dictionary<string, Consum>();
        private readonly Dictionary<string, Equipment> _eqpDict = new Dictionary<string, Equipment>();
        private readonly List<ScriptableObject> _runtimeRegisteredItems = new List<ScriptableObject>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeDictionary();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void RegisterItemSO(ItemSO so)
        {
            if (so == null || string.IsNullOrWhiteSpace(so.itemName)) return;

            switch (so.category)
            {
                case ItemCategory.Weapon:
                case ItemCategory.Armor:
                    RegisterEquipmentSO(so);
                    break;

                case ItemCategory.Consumable:
                    RegisterConsumableSO(so);
                    break;
            }

            Debug.Log($"[ItemManager] ItemSO registered: {so.itemName} ({so.category})");
        }

        private void RegisterEquipmentSO(ItemSO so)
        {
            RegisterEqpInfo(so.ToEqpInfo(), so.category == ItemCategory.Weapon ? ItemType.Weapon : ItemType.Armor, so.price);
        }

        private void RegisterConsumableSO(ItemSO so)
        {
            Consum consum = ScriptableObject.CreateInstance<Consum>();
            consum.name = so.itemName;
            consum.ConsumItem = so.ToConsumableInfo();
            consum.priceLevel = BuildPriceLevels(so.price);

            _consumDict[so.itemName] = consum;
            _runtimeRegisteredItems.Add(consum);
        }

        private static List<int> BuildPriceLevels(int price)
        {
            int safePrice = Mathf.Max(0, price);
            return new List<int> { safePrice, safePrice, safePrice };
        }

        private void InitializeDictionary()
        {
            _consumDict.Clear();
            foreach (var consum in AllItems)
            {
                string key = consum != null && consum.ConsumItem != null ? consum.ConsumItem.Name : "";
                if (!string.IsNullOrEmpty(key) && !_consumDict.ContainsKey(key))
                    _consumDict.Add(key, consum);
            }

            _eqpDict.Clear();
            foreach (var eqp in AllEqps)
            {
                string key = eqp != null && eqp.EqpItem != null ? eqp.EqpItem.Name : "";
                if (!string.IsNullOrEmpty(key) && !_eqpDict.ContainsKey(key))
                    _eqpDict.Add(key, eqp);
            }

            RegisterCharacterEquipment();

            Debug.Log($"[ItemManager] Loaded - Consum:{_consumDict.Count}, Eqp:{_eqpDict.Count}");
        }

        private void RegisterCharacterEquipment()
        {
            foreach (var entry in CharacterRegistry.All.Values)
            {
                RegisterEqpInfo(entry.Weapon, ItemType.Weapon, 0);
                RegisterEqpInfo(entry.Armor, ItemType.Armor, 0);
            }
        }

        private void RegisterEqpInfo(Jun.EqpInfo info, ItemType itemType, int price)
        {
            if (info == null || string.IsNullOrWhiteSpace(info.Name)) return;

            Equipment equipment = ScriptableObject.CreateInstance<Equipment>();
            equipment.name = info.Name;
            equipment.itemType = itemType;
            equipment.EqpItem = info;
            equipment.priceLevel = BuildPriceLevels(price);

            _eqpDict[info.Name] = equipment;
            _runtimeRegisteredItems.Add(equipment);
        }

        public Consum GetConsumData(string itemName)
        {
            if (string.IsNullOrEmpty(itemName)) return null;
            _consumDict.TryGetValue(itemName, out Consum data);
            return data;
        }

        public Equipment GetEquipmentData(string itemName)
        {
            if (string.IsNullOrEmpty(itemName)) return null;
            _eqpDict.TryGetValue(itemName, out Equipment data);
            return data;
        }

        public Jun.EqpInfo GetEqpData(string itemName)
        {
            Equipment equipmentData = GetEquipmentData(itemName);
            return equipmentData != null ? equipmentData.EqpItem : null;
        }

        public Sprite GetIcon(string itemName)
        {
            if (string.IsNullOrEmpty(itemName)) return null;
            if (_consumDict.TryGetValue(itemName, out Consum consum) && consum.ConsumItem?.icon != null)
                return consum.ConsumItem.icon;
            if (_eqpDict.TryGetValue(itemName, out Equipment equipment) && equipment.EqpItem?.icon != null)
                return equipment.EqpItem.icon;

            // ItemManager 초기화 시점(Lobby)에 CharacterRegistry가 비어있을 수 있으므로
            // 런타임에 CharacterRegistry를 직접 조회해 아이콘을 찾는다.
            foreach (var entry in CharacterRegistry.All.Values)
            {
                if (entry.Weapon?.Name == itemName && entry.Weapon.icon != null) return entry.Weapon.icon;
                if (entry.Armor?.Name  == itemName && entry.Armor.icon  != null) return entry.Armor.icon;
                // CharacterCard.Items (ConsumableInfo 직접 할당) 소모품도 체크
                if (entry.Items != null)
                    foreach (var c in entry.Items)
                        if (c?.Name == itemName && c.icon != null) return c.icon;
            }
            return null;
        }

        /// <summary>
        /// CharacterSelectManager.InitCards() 이후 CharacterRegistry가 채워진 시점에 호출해
        /// 캐릭터 소모품 아이콘을 _consumDict에 등록합니다.
        /// </summary>
        public void RefreshFromCharacterRegistry()
        {
            foreach (var entry in CharacterRegistry.All.Values)
            {
                if (entry.Items == null) continue;
                foreach (var consumInfo in entry.Items)
                {
                    if (consumInfo == null || string.IsNullOrEmpty(consumInfo.Name)) continue;
                    if (_consumDict.ContainsKey(consumInfo.Name)) continue; // 이미 있으면 skip

                    Consum consum = ScriptableObject.CreateInstance<Consum>();
                    consum.name = consumInfo.Name;
                    consum.ConsumItem = consumInfo;
                    _consumDict[consumInfo.Name] = consum;
                    _runtimeRegisteredItems.Add(consum);
                }
            }
            Debug.Log($"[ItemManager] RefreshFromCharacterRegistry 완료 — Consum:{_consumDict.Count}, Eqp:{_eqpDict.Count}");
        }
    }
}
