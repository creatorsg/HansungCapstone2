using System.Collections.Generic;
using UnityEngine;

namespace Lsy
{
    [CreateAssetMenu(fileName = "NPCData", menuName = "NPCData")]
    public class NPCData : ScriptableObject
    {
        public string npcName;
        public List<Consum> sellingItems;
        public List<Equipment> sellingEquipments;
        public List<int> upgradeTargetGold;
        public int maxLevel = 3;
    }
}
