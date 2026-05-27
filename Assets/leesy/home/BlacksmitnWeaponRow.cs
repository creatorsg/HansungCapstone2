using TMPro;
using UnityEngine;

namespace Lsy
{
    public class BlacksmithWeaponRow : BaseUpgradeRow
    {
        public WeaponUpgradeData weaponData;
        public TextMeshProUGUI upgradeProgressText;

        private void Start()
        {
            if (weaponData == null)
                Debug.LogWarning($"[BlacksmithWeaponRow] {gameObject.name}의 weaponData가 NULL - ScriptableObject를 연결하세요.");
            else
                Debug.Log($"<color=green>[BlacksmithWeaponRow] {gameObject.name} 초기화 완료. 무기:{weaponData.weaponId}, 노드 수:{weaponData.nodes.Count}</color>");
        }

        protected override int GetNodePrice(int nodeIndex)
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
                int currentWeaponLevel = unit.GetBlacksmithWeaponLevel(weaponData.weaponId);
                // [수정] 다른 무기의 강화 상태가 이 무기 row를 잠그지 않도록 무기별 단계만 검사합니다.
                if (nodeIndex > 0 && currentWeaponLevel < nodeIndex)
                {
                    Debug.Log($"[BlacksmithWeaponRow] 노드[{nodeIndex}] 잠김 - 이전 노드 미구매 (구매수:{currentWeaponLevel})");
                    return false;
                }

                if (currentWeaponLevel > nodeIndex)
                {
                    Debug.Log($"[BlacksmithWeaponRow] 노드[{nodeIndex}] 이미 구매됨");
                    return false;
                }
            }

            return true;
        }

        protected override void OnRefreshCompleted(int npcLevel, CharacterUnit unit)
        {
            if (upgradeProgressText == null) return;

            int currentLevel = 0;
            // [수정] 무기 강화 표시는 NPC 레벨이 아니라 현재 캐릭터가 해당 무기에 구매한 노드 수를 보여줍니다.
            if (unit != null && weaponData != null)
                currentLevel = unit.GetBlacksmithWeaponLevel(weaponData.weaponId);

            upgradeProgressText.text = $"Lv.{currentLevel}";
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
