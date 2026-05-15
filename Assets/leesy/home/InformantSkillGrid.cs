using UnityEngine;

namespace Lsy
{
    public class InformantSkillGrid : BaseUpgradeRow
    {
        public SkillUpgradeData skillData;

        protected override int GetNodePrice(int nodeIndex)
        {
            if (skillData == null || nodeIndex >= skillData.nodes.Count) return 0;
            return skillData.nodes[nodeIndex].price;
        }

        protected override bool CanPurchaseNode(int nodeIndex, int npcLevel, CharacterUnit unit)
        {
            if (skillData == null || nodeIndex >= skillData.nodes.Count) return false;

            SkillUpgradeNode nodeData = skillData.nodes[nodeIndex];

            if (npcLevel < nodeData.requiredNpcLevel) return false;

            if (unit != null && IsSkillPurchased(nodeData.skillId, unit))
                return false;

            return true;
        }

        protected override void OnNodeClicked(int nodeIndex)
        {
            if (skillData == null || nodeIndex >= skillData.nodes.Count) return;

            CharacterShop shop = GetLocalShop();
            if (shop == null) return;

            NPCState npcState = GetNpcState();
            int npcLevel = npcState != null ? npcState.currentLevel : 1;

            SkillUpgradeNode nodeData = skillData.nodes[nodeIndex];
            int skillIndex = nodeIndex;

            // 서버로 보낸다: 구매/강화할 스킬 인덱스(skillIndex)
            shop.CmdUpgradeSkillWithLevel(nodeData.skillId, skillIndex, nodeData.price, npcLevel, nodeData.requiredNpcLevel);
        }

        private bool IsSkillPurchased(string skillId, CharacterUnit unit)
        {
            foreach (var skill in unit.mySkills)
                if (skill.skillName == skillId) return true;
            return false;
        }
    }
}
