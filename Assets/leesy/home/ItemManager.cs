using System.Collections.Generic;
using UnityEngine;

namespace Lsy
{

    public class ItemManager : MonoBehaviour
    {
        public static ItemManager Instance { get; private set; }

        public List<ItemData> allItems = new List<ItemData>();

        private Dictionary<string, ItemData> _itemDict = new Dictionary<string, ItemData>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject); // 씬이 넘어가도 파괴되지 않음
                InitializeDictionary();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        // 게임 시작 시 리스트에 있는 데이터를 딕셔너리에 정리
        private void InitializeDictionary()
        {
            _itemDict.Clear();
            foreach (var item in allItems)
            {
                if (item != null)
                {

                    if (!_itemDict.ContainsKey(item.itemName))
                    {
                        _itemDict.Add(item.itemName, item);
                    }
                    else
                    {
                        Debug.LogWarning($"[ItemManager] 아이템 이름 중복 '{item.itemName}'이(가) 여러 개 있습니다.");
                    }
                }
            }
        }

        public ItemData GetItemData(string itemName)
        {
            if (string.IsNullOrEmpty(itemName)) return null;

            if (_itemDict.TryGetValue(itemName, out ItemData data))
            {
                return data;
            }

            Debug.LogWarning($"[ItemManager] '{itemName}'(을)를 찾을 수 없습니다. 리스트에 등록되었는지 확인하세요.");
            return null;
        }
    }
}