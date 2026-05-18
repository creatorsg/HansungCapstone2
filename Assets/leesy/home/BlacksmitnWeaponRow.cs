using UnityEngine;

namespace Lsy
{
    public class BlacksmithWeaponRow : BaseUpgradeRow
    {
        public WeaponUpgradeData weaponData;

        private void Start()
        {
            if (weaponData == null)
                Debug.LogWarning($"[BlacksmithWeaponRow] {gameObject.name}의 weaponData가 NULL - ScriptableObject를 연결하세요.");
            else
                Debug.Log($"<color=green>[BlacksmithWeaponRow] {gameObject.name} 초기화 완료. 무기:{weaponData.weaponId}, 노드 수:{weaponData.nodes.Count}</color>");
        }

        protected override int GetNodePrice(int nodeIndex, CharacterUnit unit)
        {
            if (weaponData == null || nodeIndex >= weaponData.nodes.Count) return 0;
            return weaponData.nodes[nodeIndex].price;
        }

        protected override bool CanPurchaseNode(int nodeIndex, int npcLevel, CharacterUnit unit)
        {
            if (weaponData == null || nodeIndex >= weaponData.nodes.Count) return false;

            WeaponUpgradeNode nodeData = weaponData.nodes[nodeIndex];

            if (npcLevel < nodeData.requiredNpcLevel)
            {
                Debug.Log($"[BlacksmithWeaponRow] 노드[{nodeIndex}] 잠김 - NPC레벨 부족 (현재:{npcLevel}, 필요:{nodeData.requiredNpcLevel})");
                return false;
            }

            if (unit != null)
            {
                if (unit.selectedWeaponId != "" && unit.selectedWeaponId != weaponData.weaponId)
                {
                    Debug.Log($"[BlacksmithWeaponRow] 노드[{nodeIndex}] 잠김 - 다른 무기 선택 중 ({unit.selectedWeaponId})");
                    return false;
                }

                if (nodeIndex > 0 && unit.purchasedNodeCount < nodeIndex)
                {
                    Debug.Log($"[BlacksmithWeaponRow] 노드[{nodeIndex}] 잠김 - 이전 노드 미구매 (구매수:{unit.purchasedNodeCount})");
                    return false;
                }

                if (unit.purchasedNodeCount > nodeIndex)
                {
                    Debug.Log($"[BlacksmithWeaponRow] 노드[{nodeIndex}] 이미 구매됨");
                    return false;
                }
            }

            return true;
        }

        protected override void OnNodeClicked(int nodeIndex)
        {
            Debug.Log($"<color=orange>[BlacksmithWeaponRow] 노드[{nodeIndex}] 클릭됨</color>");

            if (weaponData == null || nodeIndex >= weaponData.nodes.Count)
            {
                Debug.LogWarning("[BlacksmithWeaponRow] weaponData가 없거나 노드 인덱스 초과");
                return;
            }

            CharacterShop shop = GetLocalShop();
            if (shop == null) return;

            NPCState npcState = GetNpcState();
            if (npcState == null)
            {
                Debug.LogWarning("[BlacksmithWeaponRow] NPCState를 찾지 못했습니다.");
                return;
            }

            int npcLevel = npcState.currentLevel;
            int price = weaponData.nodes[nodeIndex].price;
            int weaponIndex = transform.GetSiblingIndex();

            Debug.Log($"<color=orange>[BlacksmithWeaponRow] CmdUpgradeWeapon 호출 - weaponId:{weaponData.weaponId}, weaponIndex:{weaponIndex}, nodeIndex:{nodeIndex}, npcLevel:{npcLevel}, price:{price}</color>");

            // 서버로 보낸다: 강화된 무기 인덱스(weaponIndex)와 노드 인덱스(nodeIndex)
            shop.CmdUpgradeWeapon(weaponData.weaponId, weaponIndex, nodeIndex, npcLevel, price);
        }
    }
}
