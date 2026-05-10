using System.Collections.Generic;
using UnityEngine;

namespace Lsy
{
    [CreateAssetMenu(fileName = "WeaponUpgradeData", menuName = "Upgrade/WeaponUpgradeData")]
    public class WeaponUpgradeData : ScriptableObject
    {
        public string weaponId;
        public string weaponName;

        public List<WeaponUpgradeNode> nodes = new List<WeaponUpgradeNode>();
    }

    [System.Serializable]
    public class WeaponUpgradeNode
    {
        public string nodeName;

        public int price;

        public int requiredNpcLevel;
    }
}