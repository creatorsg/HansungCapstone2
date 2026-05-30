using Jun;
using System.Collections.Generic;
using UnityEngine;

namespace Lsy
{
    [CreateAssetMenu(fileName = "Equipment", menuName = "Scriptable Objects/Equipment")]
    public class Equipment : ScriptableObject
    {
        public ItemType itemType = ItemType.Weapon;
        public EqpInfo EqpItem;
        public List<int> priceLevel = new List<int> { 300, 200, 100 };

        public InventoryItem ToInventoryItem(int amount = 1)
        {
            return new InventoryItem
            {
                itemName = EqpItem.Name,
                amount = amount,
                Type = itemType,
                EquipInfo = EqpItem
            };
        }
    }
}
