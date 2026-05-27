using System.Collections.Generic;
using UnityEngine;

namespace Lsy
{
    public class ItemManager : MonoBehaviour
    {
        public static ItemManager Instance { get; private set; }

        // [수정] NPC 팝업 판매 데이터 원본입니다. 바텐더는 AllItems, 대장장이는 AllEqps를 사용합니다.
        public List<Consum> AllItems = new List<Consum>();
        public List<Equipment> AllEqps = new List<Equipment>();

        private Dictionary<string, Consum>    _consumDict = new Dictionary<string, Consum>();
        private Dictionary<string, Equipment> _eqpDict    = new Dictionary<string, Equipment>();

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

        /// <summary>Inseon 코드 호환용. ItemManager는 더 이상 ItemSO를 관리하지 않습니다.</summary>
        public void RegisterItemSO(ItemSO so)
        {
            // [수정] CharacterSelectManager가 아직 호출하므로 메서드는 남기되, ItemSO 등록은 하지 않습니다.
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

            Debug.Log($"[ItemManager] 로드 완료 — Consum:{_consumDict.Count}, Eqp:{_eqpDict.Count}");
        }

        // [수정] 바텐더 NPC 팝업 판매용 소모품 데이터를 이름으로 조회합니다.
        public Consum GetConsumData(string itemName)
        {
            if (string.IsNullOrEmpty(itemName)) return null;
            _consumDict.TryGetValue(itemName, out Consum data);
            return data;
        }

        // [수정] 대장장이 NPC 팝업 판매용 장비 데이터를 이름으로 조회합니다.
        public Equipment GetEquipmentData(string itemName)
        {
            if (string.IsNullOrEmpty(itemName)) return null;
            _eqpDict.TryGetValue(itemName, out Equipment data);
            return data;
        }

        // [수정] EquipmentUI와 NPC 구매 로직이 장비 전투 데이터를 이름으로 조회할 수 있게 병합 후 누락된 API를 복구합니다.
        public Jun.EqpInfo GetEqpData(string itemName)
        {
            Equipment equipmentData = GetEquipmentData(itemName);
            if (equipmentData != null)
                return equipmentData.EqpItem;

            return null;
        }

        /// <summary>
        /// 아이템 이름으로 NPC 판매 데이터의 아이콘을 조회합니다.
        /// </summary>
        public Sprite GetIcon(string itemName)
        {
            if (string.IsNullOrEmpty(itemName)) return null;
            if (_consumDict.TryGetValue(itemName, out Consum consum) && consum.ConsumItem != null)
                return consum.ConsumItem.icon;
            if (_eqpDict.TryGetValue(itemName, out Equipment equipment) && equipment.EqpItem != null)
                return equipment.EqpItem.icon;
            return null;
        }
    }
}
