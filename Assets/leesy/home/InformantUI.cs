using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lsy
{
    /// <summary>
    /// ������ �˾� UI.
    /// �ν����Ϳ��� skillGrids�� InformantSkillGrid�� ���.
    /// ����ó�� 2x2 �׸���� InformantSkillGrid �ϳ��� ��� 4���� �����ϸ� ��.
    /// </summary>
    public class InformantUI : BaseUpgradeUI
    {
        [Header("��ų �׸��� ���")]
        public List<InformantSkillGrid> skillGrids = new List<InformantSkillGrid>();

        [Header("스킬 정보 표시 (선택 - 비워두면 무시됨)")]
        [Tooltip("스킬 노드 버튼을 누르면 채워질 스킬 이름 텍스트")]
        public TextMeshProUGUI skillNameText;
        [Tooltip("스킬 노드 버튼을 누르면 채워질 스킬 설명 텍스트")]
        public TextMeshProUGUI skillDescText;

        [Header("강화 버튼 (선택 - 비워두면 노드 클릭으로 구매 불가, 설명만 표시)")]
        [Tooltip("현재 선택한 노드를 실제로 강화(구매)하는 버튼. 구매 가능할 때만 활성화됨")]
        public Button purchaseButton;

        // 현재 설명을 띄워 둔(=강화 버튼이 가리키는) 노드
        private InformantSkillGrid _selectedGrid;
        private int _selectedNodeIndex = -1;

        private void Awake()
        {
            if (purchaseButton != null)
            {
                purchaseButton.onClick.RemoveAllListeners();
                purchaseButton.onClick.AddListener(OnPurchaseClicked);
                purchaseButton.interactable = false;
            }
        }

        /// <summary>
        /// 스킬 노드 버튼 클릭 시 InformantSkillGrid가 호출한다.
        /// 클릭한 노드를 "선택"으로 기억하고, 그 레벨의 SO 설명을 표시하며, 강화 버튼 상태를 갱신한다.
        /// (구매는 하지 않는다 — 실제 강화는 강화 버튼에서 수행)
        /// </summary>
        public void SelectNode(InformantSkillGrid grid, int nodeIndex)
        {
            if (grid == null) return;

            _selectedGrid = grid;
            _selectedNodeIndex = nodeIndex;

            string heroCode = PlayerAccount.LocalInstance != null
                && PlayerAccount.LocalInstance.currentSelectedCharacter != null
                ? PlayerAccount.LocalInstance.currentSelectedCharacter.heroCode
                : null;

            ShowSkillInfo(heroCode, grid.SkillIndex, grid.TargetLevelOfNode(nodeIndex));
            UpdatePurchaseButton();
        }

        /// <summary>클릭한 레벨 노드(targetLevel)의 SkillUpgradeData(SO) 내용을 공용 텍스트 패널에 표시한다.</summary>
        public void ShowSkillInfo(string heroCode, int skillIndex, int targetLevel)
        {
            if (!SkillUpgradeRegistry.TryGet(heroCode, skillIndex, targetLevel, out var node) || node == null)
                return;

            if (skillNameText != null) skillNameText.text = node.skillName;
            if (skillDescText != null) skillDescText.text = node.skillDescription;
        }

        private void OnPurchaseClicked()
        {
            if (_selectedGrid == null || _selectedNodeIndex < 0) return;
            _selectedGrid.TryPurchase(_selectedNodeIndex);
            // 구매 후 mySkills 변경 → OnLocalUpgradeStateChanged → RefreshAllRows → UpdatePurchaseButton 으로 버튼 상태 자동 갱신
        }

        private void UpdatePurchaseButton()
        {
            if (purchaseButton == null) return;

            bool can = _selectedGrid != null
                    && _selectedNodeIndex >= 0
                    && _selectedGrid.CanPurchase(_selectedNodeIndex);
            purchaseButton.interactable = can;
        }

        public override void RefreshAllRows()
        {
            base.RefreshAllRows();
            // 캐릭터 전환·구매 등으로 행이 다시 그려지면 강화 버튼 상태도 다시 평가한다.
            UpdatePurchaseButton();
        }

        protected override IEnumerable<BaseUpgradeRow> GetRows()
        {
            // [����] ���̾��Ű���� ������ ������ grid�� skillGrids ����Ʈ�� ������ NPC ����/���� ���� ���Ű� Ŭ�� �����ʰ� ����ǵ��� �ڽ� grid�� �Բ� ����մϴ�.
            foreach (var grid in skillGrids)
            {
                if (grid != null)
                    yield return grid;
            }

            foreach (var grid in GetComponentsInChildren<InformantSkillGrid>(true))
            {
                if (grid != null && !skillGrids.Contains(grid))
                    yield return grid;
            }
        }
    }
}