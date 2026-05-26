using System.Collections.Generic;
using UnityEngine;

namespace Lsy
{
    public class ItemManager : MonoBehaviour
    {
        public static ItemManager Instance { get; private set; }

        public List<ItemData> allItems   = new List<ItemData>();
        public List<ItemSO>   allItemSOs = new List<ItemSO>();   // ItemSO 에셋 목록

        private Dictionary<string, ItemData> _itemDict = new Dictionary<string, ItemData>();
        private Dictionary<string, ItemSO>   _soDict   = new Dictionary<string, ItemSO>();

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

        /// <summary>씬 전환 등으로 새 ItemSO가 메모리에 올라왔을 때 수동으로 재갱신합니다.</summary>
        public void RefreshItemSOs()
        {
            InitializeDictionary();
        }

        private void InitializeDictionary()
        {
            _itemDict.Clear();
            foreach (var item in allItems)
            {
                if (item != null && !_itemDict.ContainsKey(item.itemName))
                    _itemDict.Add(item.itemName, item);
            }

            _soDict.Clear();

            // ① Inspector에 수동 등록한 것 먼저
            foreach (var so in allItemSOs)
            {
                if (so != null && !_soDict.ContainsKey(so.itemName))
                    _soDict.Add(so.itemName, so);
            }

            // ② Resources/Items/ 폴더에서 자동 로드
            var loaded = Resources.LoadAll<ItemSO>("Items");
            foreach (var so in loaded)
            {
                if (so != null && !string.IsNullOrEmpty(so.itemName) && !_soDict.ContainsKey(so.itemName))
                    _soDict.Add(so.itemName, so);
            }

            // ③ 현재 메모리에 올라와 있는 모든 ItemSO 탐색 (에디터/빌드 무관하게 동작)
            //    에디터에서는 프로젝트 내 모든 에셋이 대상, 빌드에서는 이미 로드된 것만 대상
            var allInMemory = Resources.FindObjectsOfTypeAll<ItemSO>();
            foreach (var so in allInMemory)
            {
                if (so != null && !string.IsNullOrEmpty(so.itemName) && !_soDict.ContainsKey(so.itemName))
                    _soDict.Add(so.itemName, so);
            }

            Debug.Log($"[ItemManager] ItemSO 로드 완료 — Inspector:{allItemSOs.Count} + Resources:{loaded.Length} + InMemory:{allInMemory.Length} = {_soDict.Count}개");
        }

        public ItemData GetItemData(string itemName)
        {
            if (string.IsNullOrEmpty(itemName)) return null;
            _itemDict.TryGetValue(itemName, out ItemData data);
            return data;
        }

        public ItemSO GetItemSO(string itemName)
        {
            if (string.IsNullOrEmpty(itemName)) return null;
            _soDict.TryGetValue(itemName, out ItemSO so);
            return so;
        }

        /// <summary>
        /// 아이템 이름으로 아이콘을 조회합니다.
        /// ItemSO → ItemData 순서로 찾습니다.
        /// </summary>
        public Sprite GetIcon(string itemName)
        {
            if (string.IsNullOrEmpty(itemName)) return null;
            if (_soDict.TryGetValue(itemName, out ItemSO so) && so.icon != null) return so.icon;
            if (_itemDict.TryGetValue(itemName, out ItemData data)) return data.itemIcon;
            return null;
        }
    }
}
