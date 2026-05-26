using System.Collections.Generic;
using UnityEngine;

namespace Lsy
{
    public class ItemManager : MonoBehaviour
    {
        public static ItemManager Instance { get; private set; }

        public List<Consum> allItems = new List<Consum>();
        public List<Equipment> allEqps = new List<Equipment>();

        private Dictionary<string, Consum> _itemDict = new Dictionary<string, Consum>();
        private Dictionary<string, Equipment> _eqpDict = new Dictionary<string, Equipment>();

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

        private void InitializeDictionary()
        {
            //  Consum 딕셔너리
            _itemDict.Clear();
            foreach (var item in allItems)
            {
                if (item == null || item.ConsumItem == null) continue;
                if (!_itemDict.ContainsKey(item.ConsumItem.Name))
                    _itemDict.Add(item.ConsumItem.Name, item);
                else
                    Debug.LogWarning($"[ItemManager] Consum 이름 중복: '{item.ConsumItem.Name}'");
            }

            //  Equipment 딕셔너리 (기존에 완전히 빠져있던 부분)
            _eqpDict.Clear();
            foreach (var eqp in allEqps)
            {
                if (eqp == null || eqp.EqpItem == null) continue;
                if (!_eqpDict.ContainsKey(eqp.EqpItem.Name))
                    _eqpDict.Add(eqp.EqpItem.Name, eqp);
                else
                    Debug.LogWarning($"[ItemManager] Equipment 이름 중복: '{eqp.EqpItem.Name}'");
            }

            Debug.Log($"[ItemManager] 초기화 완료  Consum: {_itemDict.Count}개 / Equipment: {_eqpDict.Count}개");
        }

        public Consum GetItemData(string itemName)
        {
            if (string.IsNullOrEmpty(itemName)) return null;
            if (_itemDict.TryGetValue(itemName, out Consum data)) return data;
            Debug.LogWarning($"[ItemManager] Consum '{itemName}' 을 찾을 수 없습니다.");
            return null;
        }

        //  주석 해제 + 올바른 딕셔너리(_eqpDict) 사용
        public Equipment GetEqpData(string eqpName)
        {
            if (string.IsNullOrEmpty(eqpName)) return null;
            if (_eqpDict.TryGetValue(eqpName, out Equipment data)) return data;
            Debug.LogWarning($"[ItemManager] Equipment '{eqpName}' 을 찾을 수 없습니다.");
            return null;
        }
    }
}