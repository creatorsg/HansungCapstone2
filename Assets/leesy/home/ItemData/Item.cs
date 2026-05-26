using System.Collections.Generic;
using UnityEngine;

namespace Lsy
{
    [CreateAssetMenu(fileName = "Item", menuName = "Item")]
    public class ItemData : ScriptableObject
    {
        public string itemName; // 이 줄이 없거나 public이 아니면 에러가 납니다.
        public Sprite itemIcon; // 이 줄이 없거나 public이 아니면 에러가 납니다.
        public List<int> priceLevel = new List<int> { 300, 200, 100 };
    }
}