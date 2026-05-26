using Jun;
using System.Collections.Generic;
using UnityEngine;

namespace Lsy
{
    [CreateAssetMenu(fileName = "Equipment", menuName = "Scriptable Objects/Equipment")]
    public class Equipment : ScriptableObject
    {
        public EqpInfo EqpItem;
        public List<int> priceLevel = new List<int> { 300, 200, 100 };
    }
}
