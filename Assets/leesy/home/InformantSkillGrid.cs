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

            // [수정] 정보상 requiredNpcLevel은 UI에 표시되는 NPC Lv.와 같은 1-based 값입니다.
            int requiredNpcLevel = GetRequiredNpcLevel(nodeData);
            if (npcLevel < requiredNpcLevel) return false;

            // [수정] skillId만 쓰면 다른 정보상 그리드의 같은 ID 노드까지 같이 구매 처리되므로, 그리드/노드 기준의 고유 구매 키를 사용합니다.
            if (unit != null && IsSkillPurchased(GetPurchaseKey(nodeIndex, nodeData), unit))
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
            string purchaseKey = GetPurchaseKey(nodeIndex, nodeData);
            int requiredNpcLevel = GetRequiredNpcLevel(nodeData);

            // 서버로 보낸다: 구매/강화할 스킬 인덱스(skillIndex)
            shop.CmdUpgradeSkillWithLevel(purchaseKey, skillIndex, nodeData.price, npcLevel, requiredNpcLevel);
        }

        private int GetRequiredNpcLevel(SkillUpgradeNode nodeData)
        {
            // [수정] 잘못 저장된 0 이하 값이 모든 레벨에서 열리지 않도록 최소 NPC Lv.1로 고정합니다.
            return nodeData != null ? Mathf.Max(1, nodeData.requiredNpcLevel) : 1;
        }

        private string GetPurchaseKey(int nodeIndex, SkillUpgradeNode nodeData)
        {
            string group = skillData != null && !string.IsNullOrEmpty(skillData.groupName) ? skillData.groupName : gameObject.name;
            string skillId = nodeData != null ? nodeData.skillId : "";
            return $"{group}:{nodeIndex}:{skillId}";
        }

        private bool IsSkillPurchased(string purchaseKey, CharacterUnit unit)
        {
            foreach (var skill in unit.mySkills)
                if (skill.skillName == purchaseKey) return true;
            return false;
        }
    }
}


