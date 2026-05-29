using UnityEngine;

namespace Lsy
{
    /// <summary>
    /// 정보상 스킬 강화 그리드. 이 그리드 하나가 한 스킬(skillIndex)의 Lv1~Lv4 노드를 담당한다.
    /// nodeIndex 0~3은 각각 targetLevel 1~4로 해석한다. Lv1은 base라 Registry에 없어 구매 불가로 표시된다.
    /// </summary>
    public class InformantSkillGrid : BaseUpgradeRow
    {
        [Tooltip("이 그리드가 담당하는 스킬 번호 (0~3). CharacterCard.skills / Info.Skills 순서와 1:1")]
        public int skillIndex;

        private int TargetLevelOf(int nodeIndex) => nodeIndex + 1;

        private CharacterUnit ActiveUnit =>
            PlayerAccount.LocalInstance != null
                ? PlayerAccount.LocalInstance.currentSelectedCharacter
                : null;

        protected override int GetNodePrice(int nodeIndex)
        {
            CharacterUnit unit = ActiveUnit;
            if (unit == null) return 0;

            if (SkillUpgradeRegistry.TryGet(unit.heroCode, skillIndex, TargetLevelOf(nodeIndex), out var node))
                return node.price;

            return 0;
        }

        protected override bool CanPurchaseNode(int nodeIndex, int npcLevel, CharacterUnit unit)
        {
            if (unit == null) return false;

            int targetLevel = TargetLevelOf(nodeIndex);
            if (!SkillUpgradeRegistry.TryGet(unit.heroCode, skillIndex, targetLevel, out var node))
                return false;

            if (npcLevel < Mathf.Max(1, node.requiredNpcLevel)) return false;

            return GetCurrentLevel(skillIndex, unit) == targetLevel - 1;
        }

        protected override void OnNodeClicked(int nodeIndex)
        {
            // [구매 분리] 노드를 눌러도 구매하지 않고, 해당 레벨 노드의 SO 설명만 표시한다.
            // 실제 강화는 InformantUI의 "강화" 버튼 → TryPurchase 로 수행한다.
            CharacterUnit unit = ActiveUnit;
            if (unit == null) return;

            int targetLevel = TargetLevelOf(nodeIndex);
            if (!SkillUpgradeRegistry.TryGet(unit.heroCode, skillIndex, targetLevel, out _))
                return; // Lv1 등 SO 노드가 없는 레벨은 보여줄 내용 없음

            InformantUI ui = GetComponentInParent<InformantUI>();
            if (ui != null)
                ui.SelectNode(this, nodeIndex);
        }

        /// <summary>이 그리드가 담당하는 스킬 번호.</summary>
        public int SkillIndex => skillIndex;

        /// <summary>노드 인덱스(0~3) → 목표 레벨(1~4).</summary>
        public int TargetLevelOfNode(int nodeIndex) => TargetLevelOf(nodeIndex);

        /// <summary>InformantUI 강화 버튼이 현재 선택 노드의 구매 가능 여부를 묻는 용도.</summary>
        public bool CanPurchase(int nodeIndex)
        {
            CharacterUnit unit = ActiveUnit;
            if (unit == null) return false;

            NPCState npcState = GetNpcState();
            int npcLevel = npcState != null ? npcState.currentLevel : 1;
            return CanPurchaseNode(nodeIndex, npcLevel, unit);
        }

        /// <summary>InformantUI 강화 버튼이 실제 구매를 요청하는 용도. 구매 가능할 때만 서버로 전송.</summary>
        public bool TryPurchase(int nodeIndex)
        {
            CharacterUnit unit = ActiveUnit;
            if (unit == null) return false;

            NPCState npcState = GetNpcState();
            int npcLevel = npcState != null ? npcState.currentLevel : 1;
            if (!CanPurchaseNode(nodeIndex, npcLevel, unit)) return false;

            CharacterShop shop = GetLocalShop();
            if (shop == null) return false;

            shop.CmdUpgradeSkillWithLevel(skillIndex, TargetLevelOf(nodeIndex), npcLevel);
            return true;
        }

        protected override void OnRefreshCompleted(int npcLevel, CharacterUnit unit)
        {
            Sprite icon = ResolveSkillIcon(unit);
            foreach (var node in upgradeNodes)
            {
                if (node == null) continue;
                node.SetIcon(icon);

                // [구매 분리] 잠긴 레벨도 설명을 보려면 클릭은 가능해야 한다.
                // 구매 가능 여부는 색(잠김=회색)으로만 구분하고, 버튼 자체는 항상 누를 수 있게 둔다.
                if (node.nodeButton != null)
                    node.nodeButton.interactable = true;
            }
        }

        private Sprite ResolveSkillIcon(CharacterUnit unit)
        {
            if (unit == null || string.IsNullOrEmpty(unit.heroCode)) return null;
            if (!CharacterRegistry.TryGet(unit.heroCode, out var entry)) return null;
            if (entry.Skills == null) return null;
            if (skillIndex < 0 || skillIndex >= entry.Skills.Count) return null;

            return entry.Skills[skillIndex]?.icon;
        }

        private int GetCurrentLevel(int skillIndex, CharacterUnit unit)
        {
            if (unit == null) return 1;

            foreach (var skill in unit.mySkills)
                if (skill.skillIndex == skillIndex)
                    return skill.currentLevel;

            return 1;
        }
    }
}
