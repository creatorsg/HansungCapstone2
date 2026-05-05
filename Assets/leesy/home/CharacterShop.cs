using Mirror;
using UnityEngine;

namespace Lsy
{
    public class CharacterShop : NetworkBehaviour
    {
        private CharacterUnit _unit;

        private void Awake()
        {
            _unit = GetComponent<CharacterUnit>();
            if (_unit == null)
                Debug.LogError("[CharacterShop] 같은 오브젝트에 CharacterUnit이 없습니다!");
            else
                Debug.Log("<color=green>[CharacterShop] CharacterUnit 연결 완료</color>");
        }

        // ==========================================
        // 1. 일반 상점 결제
        // ==========================================
        [Command(requiresAuthority = false)]
        public void CmdBuyItem(string itemName, int price, NetworkConnectionToClient sender = null)
        {
            Debug.Log($"[CharacterShop][Server] CmdBuyItem - {itemName}, {price}G");

            if (_unit.currentGold < price)
            {
                SendNotification(sender, "골드가 부족합니다.");
                return;
            }
            if (_unit.GetItemAmount(itemName) >= 5)
            {
                SendNotification(sender, $"{itemName}은(는) 이미 5개를 소지하고 있습니다.");
                return;
            }

            _unit.currentGold -= price;
            _unit.AddItem(itemName, 1);
            SendNotification(sender, $"[시스템 알림] {itemName} 구매 완료.");
        }

        // ==========================================
        // 2. NPC 투자
        // ==========================================
        [Command(requiresAuthority = false)]
        public void CmdInvestToNPC(uint npcNetId, int amount, NetworkConnectionToClient sender = null)
        {
            Debug.Log($"[CharacterShop][Server] CmdInvestToNPC - netId:{npcNetId}, amount:{amount}");

            if (_unit.currentGold < amount)
            {
                SendNotification(sender, "골드가 부족합니다.");
                return;
            }
            if (NetworkServer.spawned.TryGetValue(npcNetId, out NetworkIdentity identity))
            {
                NPCState npc = identity.GetComponent<NPCState>();
                if (npc != null)
                {
                    _unit.currentGold -= amount;
                    npc.ReceiveInvestment(amount);
                }
            }
        }

        // ==========================================
        // 3. 바텐더
        // ==========================================
        [Command(requiresAuthority = false)]
        public void CmdUseBartender(int price, NetworkConnectionToClient sender = null)
        {
            Debug.Log($"[CharacterShop][Server] CmdUseBartender - price:{price}");

            if (_unit.currentGold < price)
            {
                SendNotification(sender, "골드가 부족합니다.");
                return;
            }
            if (_unit.ApplyBartenderHeal())
            {
                _unit.currentGold -= price;
                SendNotification(sender, $"{_unit.characterName}의 체력/정신력이 회복되었습니다.");
            }
            else
            {
                SendNotification(sender, "이미 체력과 정신력이 최대입니다.");
            }
        }

        // ==========================================
        // 4. 대장장이 무기 업그레이드
        // ==========================================
        [Command(requiresAuthority = false)]
        public void CmdUpgradeWeapon(string weaponId, int nodeIndex, int npcLevel, int price, NetworkConnectionToClient sender = null)
        {
            Debug.Log($"[CharacterShop][Server] CmdUpgradeWeapon - weaponId:{weaponId}, node:{nodeIndex}, npcLv:{npcLevel}, price:{price}");

            bool success = _unit.ApplyBlacksmithUpgrade(weaponId, nodeIndex, npcLevel, price);

            if (success)
            {
                Debug.Log($"<color=green>[CharacterShop][Server] 강화 성공! weaponId:{weaponId}, node:{nodeIndex}</color>");
                SendNotification(sender, $"[{weaponId}] {nodeIndex + 1}단계 강화 완료!");
            }
            else
            {
                // 실패 원인 서버 로그
                Debug.Log($"<color=red>[CharacterShop][Server] 강화 실패 - gold:{_unit.currentGold}/{price}, selectedWeapon:{_unit.selectedWeaponId}, purchasedCount:{_unit.purchasedNodeCount}</color>");

                if (_unit.currentGold < price)
                    SendNotification(sender, "골드가 부족합니다.");
                else if (_unit.selectedWeaponId != "" && _unit.selectedWeaponId != weaponId)
                    SendNotification(sender, "이미 다른 무기를 강화 중입니다.");
                else if (npcLevel < nodeIndex + 1)
                    SendNotification(sender, "NPC 레벨이 부족합니다.");
                else if (_unit.purchasedNodeCount > nodeIndex)
                    SendNotification(sender, "이미 구매한 노드입니다.");
                else
                    SendNotification(sender, "이전 단계를 먼저 구매해야 합니다.");
            }
        }

        // ==========================================
        // 5. 정보상 스킬 구매
        // ==========================================
        [Command(requiresAuthority = false)]
        public void CmdUpgradeSkillWithLevel(string skillId, int price, int npcLevel, int requiredNpcLevel, NetworkConnectionToClient sender = null)
        {
            Debug.Log($"[CharacterShop][Server] CmdUpgradeSkillWithLevel - skillId:{skillId}, price:{price}, npcLv:{npcLevel}, required:{requiredNpcLevel}");

            bool success = _unit.ApplySkillPurchase(skillId, npcLevel, price, requiredNpcLevel);

            if (success)
            {
                Debug.Log($"<color=green>[CharacterShop][Server] 스킬 습득 성공! skillId:{skillId}</color>");
                SendNotification(sender, $"[{skillId}] 스킬 습득 완료!");
            }
            else
            {
                if (_unit.currentGold < price)
                    SendNotification(sender, "골드가 부족합니다.");
                else if (npcLevel < requiredNpcLevel)
                    SendNotification(sender, "NPC 레벨이 부족합니다.");
                else
                    SendNotification(sender, "이미 보유한 스킬입니다.");
            }
        }

        // ==========================================
        // 알림 전송
        // ==========================================
        [Server]
        private void SendNotification(NetworkConnectionToClient target, string message)
        {
            Debug.Log($"[CharacterShop][Server] SendNotification - target:{(target != null ? "있음" : "NULL")}, msg:{message}");

            if (target != null)
                RpcNotify(target, message);
            else
                Debug.LogWarning("[CharacterShop] sender가 NULL이라 알림을 보낼 수 없습니다.");
        }

        [TargetRpc]
        private void RpcNotify(NetworkConnectionToClient target, string message)
        {
            Debug.Log($"<color=white>[CharacterShop][Client] 알림 수신: {message}</color>");
        }
    }
}