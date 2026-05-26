using System.Collections.Generic;
using UnityEngine;

namespace Lsy
{
    [CreateAssetMenu(fileName = "Consum", menuName = "Scriptable Objects/Consum")]
    public class Consum : ScriptableObject
    {
        public Jun.ConsumableInfo ConsumItem;
        public List<int> priceLevel = new List<int> { 300, 200, 100 };
    }
}
