using UnityEngine;

namespace Lsy
{
    public class InformantSkillGrid : BaseUpgradeRow
    {
        [SerializeField, Range(0, 3)] private int skillIndex;

        [Tooltip("Use this when one grid has 16 buttons. Every 4 buttons advance to the next skill.")]
        [SerializeField] private bool advanceSkillEveryFourNodes;

        protected override int GetNodePrice(int nodeIndex, CharacterUnit unit)
        {
            SkillTreeNodeSO node = ResolveNode(nodeIndex, unit);
            return node != null ? node.unlockCost : 0;
        }

        protected override bool CanPurchaseNode(int nodeIndex, int npcLevel, CharacterUnit unit)
        {
            SkillTreeNodeSO node = ResolveNode(nodeIndex, unit);
            if (node == null || unit == null) return false;

            return unit.CanUnlockSkillNode(node.nodeId, npcLevel, out _);
        }

        protected override void OnNodeClicked(int nodeIndex)
        {
            CharacterUnit unit = PlayerAccount.LocalInstance != null
                ? PlayerAccount.LocalInstance.currentSelectedCharacter
                : null;

            SkillTreeNodeSO node = ResolveNode(nodeIndex, unit);
            if (node == null) return;

            CharacterShop shop = GetLocalShop();
            if (shop == null) return;

            NPCState npcState = GetNpcState();
            int npcLevel = npcState != null ? npcState.currentLevel : 1;

            shop.CmdUpgradeSkillWithLevel(node.nodeId, npcLevel);
        }

        private SkillTreeNodeSO ResolveNode(int nodeIndex, CharacterUnit unit)
        {
            if (unit == null) return null;

            CharacterSkillTreeSO tree = SkillTreeRegistry.GetTree(unit.SkillTreeCharacterCode);
            if (tree == null) return null;

            ResolveNodePosition(nodeIndex, out int resolvedSkillIndex, out int level, out int branchIndex);
            return tree.FindByPosition(resolvedSkillIndex, level, branchIndex);
        }

        private void ResolveNodePosition(int nodeIndex, out int resolvedSkillIndex, out int level, out int branchIndex)
        {
            int slotIndex = nodeIndex;
            resolvedSkillIndex = skillIndex;

            if (advanceSkillEveryFourNodes)
            {
                resolvedSkillIndex = nodeIndex / 4;
                slotIndex = nodeIndex % 4;
            }

            level = slotIndex < 2 ? 2 : 3;
            branchIndex = slotIndex % 2;
        }
    }
}
