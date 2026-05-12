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

        [Command(requiresAuthority = false)]
        public void CmdBuyItem(string itemName, int price, NetworkConnectionToClient sender = null)
        {
            // 서버로 보낸다: 아이템 구매 요청(itemName, price)
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

        [Command(requiresAuthority = false)]
        public void CmdInvestToNPC(uint npcNetId, int amount, NetworkConnectionToClient sender = null)
        {
            // 서버로 보낸다: NPC 투자 요청(npcNetId, amount)
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

        [Command(requiresAuthority = false)]
        public void CmdUseBartender(int price, NetworkConnectionToClient sender = null)
        {
            // 서버로 보낸다: 바텐더 회복 요청(price) + 현재 HP/SAN을 최대치로 회복
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

        [Command(requiresAuthority = false)]
        public void CmdUpgradeWeapon(string weaponId, int weaponIndex, int nodeIndex, int npcLevel, int price, NetworkConnectionToClient sender = null)
        {
            // 서버로 보낸다: 무기 노드 강화 요청(weaponId, weaponIndex, nodeIndex, npcLevel, price)
            Debug.Log($"[CharacterShop][Server] CmdUpgradeWeapon - weaponId:{weaponId}, weaponIndex:{weaponIndex}, node:{nodeIndex}, npcLv:{npcLevel}, price:{price}");

            bool success = _unit.ApplyBlacksmithUpgrade(weaponId, nodeIndex, npcLevel, price);

            if (success)
            {
                Debug.Log($"<color=green>[CharacterShop][Server] 강화 성공! weaponId:{weaponId}, node:{nodeIndex}</color>");
                SendNotification(sender, $"[{weaponId}] {nodeIndex + 1}단계 강화 완료!");
            }
            else
            {
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

        [Command(requiresAuthority = false)]
        public void CmdUpgradeSkillWithLevel(string skillId, int skillIndex, int price, int npcLevel, int requiredNpcLevel, NetworkConnectionToClient sender = null)
        {
            // 서버로 보낸다: 스킬 구매/강화 요청(skillId, skillIndex, price, npcLevel, requiredNpcLevel)
            Debug.Log($"[CharacterShop][Server] CmdUpgradeSkillWithLevel - skillId:{skillId}, skillIndex:{skillIndex}, price:{price}, npcLv:{npcLevel}, required:{requiredNpcLevel}");

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

        [Command(requiresAuthority = false)]
        public void CmdEquipItem(string itemName, NetworkConnectionToClient sender = null)
        {
            // 서버로 보낸다: 인벤토리 아이템 장착 요청(itemName)
            Debug.Log($"[CharacterShop][Server] CmdEquipItem - item:{itemName}");

            if (string.IsNullOrWhiteSpace(itemName))
            {
                SendNotification(sender, "장착할 아이템 이름이 비어 있습니다.");
                return;
            }

            if (_unit.GetItemAmount(itemName) <= 0)
            {
                SendNotification(sender, $"[{itemName}] 아이템이 인벤토리에 없습니다.");
                return;
            }

            _unit.selectedWeaponId = itemName;
            SendNotification(sender, $"[{itemName}] 장착 완료.");
        }

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
            // 서버에서 받아온다: 상점 처리 결과 알림 메시지(message)
            Debug.Log($"<color=white>[CharacterShop][Client] 알림 수신: {message}</color>");
        }
    }
}
