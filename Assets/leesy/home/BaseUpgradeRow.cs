using System.Collections.Generic;
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

            for (int i = 0; i < upgradeNodes.Count; i++)
            {
                if (upgradeNodes[i] == null)
                {
                    Debug.LogWarning($"[UpgradeRow] upgradeNodes[{i}]가 NULL - 인스펙터에서 연결을 확인하세요.");
                    continue;
                }

                bool canUpgrade = CanPurchaseNode(i, npcLevel, unit);
                int price = GetNodePrice(i);

                Debug.Log($"<color=yellow>[UpgradeRow] 노드[{i}] canUpgrade:{canUpgrade}, price:{price}</color>");

                upgradeNodes[i].SetNodeState(canUpgrade, price);

                int capturedIndex = i;
                upgradeNodes[i].nodeButton.onClick.RemoveAllListeners();
                upgradeNodes[i].nodeButton.onClick.AddListener(() => OnNodeClicked(capturedIndex));
            }
        }

        protected abstract bool CanPurchaseNode(int nodeIndex, int npcLevel, CharacterUnit unit);
        protected abstract void OnNodeClicked(int nodeIndex);
        protected virtual int GetNodePrice(int nodeIndex) => 0;

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