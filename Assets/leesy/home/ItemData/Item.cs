using System.Collections.Generic;
using UnityEngine;

namespace Lsy
{
    [CreateAssetMenu(fileName = "Item", menuName = "Item")]
    public class ItemData : ScriptableObject
    {
        public string itemName = "test";
        public Sprite itemIcon = null;
        public List<int> priceLevel = new List<int> { 300, 200, 100 };
    }
}