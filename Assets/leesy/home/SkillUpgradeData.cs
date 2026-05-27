using System.Collections.Generic;
using UnityEngine;

namespace Lsy
{
    
    //   nodes[0]: skillId="tracking",  skillName="추적술",  price=1000, requiredNpcLevel=1
    //   nodes[1]: skillId="disguise",  skillName="변장술",  price=2000, requiredNpcLevel=1
    //   nodes[2]: skillId="sabotage",  skillName="방해공작", price=3000, requiredNpcLevel=2
    //   nodes[3]: skillId="ambush",    skillName="매복",    price=4000, requiredNpcLevel=2

    [CreateAssetMenu(fileName = "SkillUpgradeData", menuName = "Upgrade/SkillUpgradeData")]
    public class SkillUpgradeData : ScriptableObject
    {
        public string groupName;

        public List<SkillUpgradeNode> nodes = new List<SkillUpgradeNode>();
    }

    [System.Serializable]
    public class SkillUpgradeNode
    {
        public string skillId;
        public string skillName;

        public int price;
        // [수정] NPCPopupUI에 표시되는 NPC Lv.숫자와 같은 1부터 시작하는 값입니다.
        public int requiredNpcLevel;
    }
}
