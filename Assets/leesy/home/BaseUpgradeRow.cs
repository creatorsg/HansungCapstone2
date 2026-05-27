using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Lsy
{
    public abstract class BaseUpgradeRow : MonoBehaviour
    {
        [Header("노드 버튼들 (순서대로 연결)")]
        public List<UpgradeNode> upgradeNodes = new List<UpgradeNode>();

        public void RefreshNodes(int npcLevel, CharacterUnit unit)
        {
            Debug.Log($"<color=yellow>[UpgradeRow] {gameObject.name} RefreshNodes 호출 - 노드 수:{upgradeNodes.Count}, NPC Lv:{npcLevel}</color>");

            List<UpgradeNode> orderedNodes = GetOrderedUpgradeNodes();

            for (int i = 0; i < orderedNodes.Count; i++)
            {
                UpgradeNode node = orderedNodes[i];

                bool canUpgrade = CanPurchaseNode(i, npcLevel, unit);
                int price = GetNodePrice(i);

                Debug.Log($"<color=yellow>[UpgradeRow] 노드[{i}] {node.gameObject.name} canUpgrade:{canUpgrade}, price:{price}</color>");

                node.SetNodeState(canUpgrade, price);

                int capturedIndex = i;
                node.nodeButton.onClick.RemoveAllListeners();
                node.nodeButton.onClick.AddListener(() => OnNodeClicked(capturedIndex));
            }

            OnRefreshCompleted(npcLevel, unit);
        }

        private List<UpgradeNode> GetOrderedUpgradeNodes()
        {
            for (int i = 0; i < upgradeNodes.Count; i++)
            {
                if (upgradeNodes[i] == null)
                    Debug.LogWarning($"[UpgradeRow] upgradeNodes[{i}]가 NULL - 인스펙터에서 연결을 확인하세요.");
            }

            // [수정] 중첩된 UI에서는 anchoredPosition의 기준 부모가 달라질 수 있으므로, 월드 중심점을 현재 row 기준 좌표로 변환해 화면 기준 가로 우선 순서를 고정합니다.
            List<UpgradeNode> orderedNodes = upgradeNodes
                .Where(node => node != null)
                .OrderByDescending(node => GetNodeSortPosition(node).y)
                .ThenBy(node => GetNodeSortPosition(node).x)
                .ToList();

            for (int i = 0; i < orderedNodes.Count; i++)
            {
                Vector2 sortPosition = GetNodeSortPosition(orderedNodes[i]);
                Debug.Log($"<color=yellow>[UpgradeRow] 정렬[{i}] {orderedNodes[i].gameObject.name} pos:{sortPosition}</color>");
            }

            return orderedNodes;
        }

        private Vector2 GetNodeSortPosition(UpgradeNode node)
        {
            RectTransform rect = node.GetComponent<RectTransform>();
            if (rect == null)
                return transform.InverseTransformPoint(node.transform.position);

            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Vector3 worldCenter = (corners[0] + corners[2]) * 0.5f;
            return transform.InverseTransformPoint(worldCenter);
        }

        protected abstract bool CanPurchaseNode(int nodeIndex, int npcLevel, CharacterUnit unit);
        protected abstract void OnNodeClicked(int nodeIndex);
        protected virtual int GetNodePrice(int nodeIndex) => 0;
        // [수정] 각 강화 row가 노드 갱신 이후 추가 UI를 갱신할 수 있도록 확장 지점을 제공합니다.
        protected virtual void OnRefreshCompleted(int npcLevel, CharacterUnit unit) { }

        protected CharacterShop GetLocalShop()
        {
            if (PlayerAccount.LocalInstance == null)
            {
                Debug.LogWarning("[UpgradeRow] PlayerAccount.LocalInstance가 NULL");
                return null;
            }
            if (PlayerAccount.LocalInstance.currentSelectedCharacter == null)
            {
                Debug.LogWarning("[UpgradeRow] currentSelectedCharacter가 NULL");
                return null;
            }

            CharacterShop shop = PlayerAccount.LocalInstance.currentSelectedCharacter
                .GetComponent<CharacterShop>();

            if (shop == null)
                Debug.LogError("[UpgradeRow] CharacterShop 컴포넌트가 프리팹에 없습니다!");

            return shop;
        }

        protected NPCState GetNpcState()
        {
            BaseUpgradeUI parentUI = GetComponentInParent<BaseUpgradeUI>();
            if (parentUI == null)
            {
                Debug.LogWarning($"[UpgradeRow] {gameObject.name}의 부모에서 BaseUpgradeUI를 찾지 못했습니다. 하이어라키 구조를 확인하세요.");
                return null;
            }
            if (parentUI.NpcState == null)
            {
                Debug.LogWarning("[UpgradeRow] BaseUpgradeUI는 찾았지만 NpcState가 NULL");
                return null;
            }
            return parentUI.NpcState;
        }
    }
}
