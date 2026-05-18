using System.Collections.Generic;
using UnityEngine;

namespace Lsy
{
    public abstract class BaseUpgradeRow : MonoBehaviour
    {
        [Header("Node buttons in display order")]
        public List<UpgradeNode> upgradeNodes = new List<UpgradeNode>();

        public void RefreshNodes(int npcLevel, CharacterUnit unit)
        {
            Debug.Log($"<color=yellow>[UpgradeRow] {gameObject.name} RefreshNodes - nodes:{upgradeNodes.Count}, NPC Lv:{npcLevel}</color>");

            for (int i = 0; i < upgradeNodes.Count; i++)
            {
                if (upgradeNodes[i] == null)
                {
                    Debug.LogWarning($"[UpgradeRow] upgradeNodes[{i}] is null. Check Inspector wiring.");
                    continue;
                }

                bool canUpgrade = CanPurchaseNode(i, npcLevel, unit);
                int price = GetNodePrice(i, unit);

                Debug.Log($"<color=yellow>[UpgradeRow] node[{i}] canUpgrade:{canUpgrade}, price:{price}</color>");

                upgradeNodes[i].SetNodeState(canUpgrade, price);

                int capturedIndex = i;
                upgradeNodes[i].nodeButton.onClick.RemoveAllListeners();
                upgradeNodes[i].nodeButton.onClick.AddListener(() => OnNodeClicked(capturedIndex));
            }
        }

        protected abstract bool CanPurchaseNode(int nodeIndex, int npcLevel, CharacterUnit unit);
        protected abstract void OnNodeClicked(int nodeIndex);
        protected virtual int GetNodePrice(int nodeIndex, CharacterUnit unit) => 0;

        protected CharacterShop GetLocalShop()
        {
            if (PlayerAccount.LocalInstance == null)
            {
                Debug.LogWarning("[UpgradeRow] PlayerAccount.LocalInstance is null");
                return null;
            }
            if (PlayerAccount.LocalInstance.currentSelectedCharacter == null)
            {
                Debug.LogWarning("[UpgradeRow] currentSelectedCharacter is null");
                return null;
            }

            CharacterShop shop = PlayerAccount.LocalInstance.currentSelectedCharacter
                .GetComponent<CharacterShop>();

            if (shop == null)
                Debug.LogError("[UpgradeRow] CharacterShop component is missing on the character prefab.");

            return shop;
        }

        protected NPCState GetNpcState()
        {
            BaseUpgradeUI parentUI = GetComponentInParent<BaseUpgradeUI>();
            if (parentUI == null)
            {
                Debug.LogWarning($"[UpgradeRow] {gameObject.name} has no BaseUpgradeUI parent. Check hierarchy.");
                return null;
            }
            if (parentUI.NpcState == null)
            {
                Debug.LogWarning("[UpgradeRow] BaseUpgradeUI found, but NpcState is null");
                return null;
            }
            return parentUI.NpcState;
        }
    }
}
