using Mirror;
using UnityEngine;

namespace Lsy
{
    /// <summary>
    /// NPC 상호작용 커맨드 처리.
    /// 골드는 PlayerAccount 귀속이므로 sender의 PlayerAccount를 찾아 처리합니다.
    /// requiresAuthority = false: 어떤 클라이언트든 호출 가능 (sender로 호출자 식별).
    /// </summary>
    public class CharacterShop : NetworkBehaviour
    {
        // ─── sender → PlayerAccount 헬퍼 ──────────────────────────────

        [Server]
        private PlayerAccount FindAccount(NetworkConnectionToClient sender)
        {
            if (sender == null) return null;
            foreach (var account in FindObjectsByType<PlayerAccount>(FindObjectsSortMode.None))
                if (account.connectionToClient == sender)
                    return account;
            return null;
        }

        [Server]
        private bool TryGetWeaponDataById(string weaponId, out WeaponUpgradeData data)
        {
            foreach (var row in FindObjectsByType<BlacksmithWeaponRow>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (row == null || row.weaponData == null) continue;
                if (row.weaponData.weaponId != weaponId) continue;
                data = row.weaponData;
                return true;
            }

            data = null;
            return false;
        }

        [Server]
        private string ResolveWeaponInventoryKey(string weaponId)
        {
            if (TryGetWeaponDataById(weaponId, out WeaponUpgradeData data))
            {
                if (!string.IsNullOrWhiteSpace(data.inventoryItemName)) return data.inventoryItemName;
                if (!string.IsNullOrWhiteSpace(data.weaponName)) return data.weaponName;
            }

            return weaponId;
        }

        [Server]
        private string ResolveWeaponIdFromInventoryKey(string key)
        {
            foreach (var row in FindObjectsByType<BlacksmithWeaponRow>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (row == null || row.weaponData == null) continue;
                if (row.weaponData.weaponId == key) return row.weaponData.weaponId;
                if (row.weaponData.weaponName == key) return row.weaponData.weaponId;
                if (row.weaponData.inventoryItemName == key) return row.weaponData.weaponId;
            }

            return key;
        }

        // ─── 커맨드 ────────────────────────────────────────────────────

        [Command(requiresAuthority = false)]
        public void CmdBuyItem(string itemName, int price, NetworkConnectionToClient sender = null)
        {
            Debug.Log($"[CharacterShop][Server] CmdBuyItem - {itemName}, {price}G");

            PlayerAccount account = FindAccount(sender);
            if (account == null) return;
            CharacterUnit unit = account.currentSelectedCharacter;
            if (unit == null) return;

            if (account.currentGold < price)
            {
                SendNotification(sender, "골드가 부족합니다.");
                return;
            }
            if (unit.GetItemAmount(itemName) >= 5)
            {
                SendNotification(sender, $"{itemName}은(는) 이미 5개를 소지하고 있습니다.");
                return;
            }

            account.currentGold -= price;
            unit.AddItem(itemName, 1);
            SendNotification(sender, $"[시스템 알림] {itemName} 구매 완료.");
        }

        [Command(requiresAuthority = false)]
        public void CmdInvestToNPC(uint npcNetId, int amount, NetworkConnectionToClient sender = null)
        {
            Debug.Log($"[CharacterShop][Server] CmdInvestToNPC - netId:{npcNetId}, amount:{amount}");

            PlayerAccount account = FindAccount(sender);
            if (account == null) return;

            if (account.currentGold < amount)
            {
                SendNotification(sender, "골드가 부족합니다.");
                return;
            }
            if (NetworkServer.spawned.TryGetValue(npcNetId, out NetworkIdentity identity))
            {
                NPCState npc = identity.GetComponent<NPCState>();
                if (npc != null)
                {
                    account.currentGold -= amount;
                    npc.ReceiveInvestment(amount);
                }
            }
        }

        [Command(requiresAuthority = false)]
        public void CmdUseBartender(int price, NetworkConnectionToClient sender = null)
        {
            Debug.Log($"[CharacterShop][Server] CmdUseBartender - price:{price}");

            PlayerAccount account = FindAccount(sender);
            if (account == null) return;
            CharacterUnit unit = account.currentSelectedCharacter;
            if (unit == null) return;

            if (account.currentGold < price)
            {
                SendNotification(sender, "골드가 부족합니다.");
                return;
            }
            if (unit.ApplyBartenderHeal())
            {
                account.currentGold -= price;
                SendNotification(sender, $"{unit.characterName}의 체력/정신력이 회복되었습니다.");
            }
            else
            {
                SendNotification(sender, "이미 체력과 정신력이 최대입니다.");
            }
        }

        [Command(requiresAuthority = false)]
        public void CmdUpgradeWeapon(string weaponId, int weaponIndex, int nodeIndex, int npcLevel, int price, NetworkConnectionToClient sender = null)
        {
            Debug.Log($"[CharacterShop][Server] CmdUpgradeWeapon - weaponId:{weaponId}, node:{nodeIndex}, npcLv:{npcLevel}, price:{price}");

            PlayerAccount account = FindAccount(sender);
            if (account == null) return;
            CharacterUnit unit = account.currentSelectedCharacter;
            if (unit == null) return;

            if (account.currentGold < price)
            {
                SendNotification(sender, "골드가 부족합니다.");
                return;
            }

            bool success = unit.ApplyBlacksmithUpgrade(weaponId, nodeIndex, npcLevel);
            if (success)
            {
                account.currentGold -= price;
                string weaponInventoryKey = ResolveWeaponInventoryKey(weaponId);
                if (nodeIndex == 0)
                    unit.AddItem(weaponInventoryKey, 1);
                Debug.Log($"<color=green>[CharacterShop][Server] 강화 성공! weaponId:{weaponId}, node:{nodeIndex}</color>");
                SendNotification(sender, $"[{weaponId}] {nodeIndex + 1}단계 강화 완료!");
            }
            else
            {
                if (unit.selectedWeaponId != "" && unit.selectedWeaponId != weaponId)
                    SendNotification(sender, "이미 다른 무기를 강화 중입니다.");
                else if (npcLevel < nodeIndex + 1)
                    SendNotification(sender, "NPC 레벨이 부족합니다.");
                else if (unit.purchasedNodeCount > nodeIndex)
                    SendNotification(sender, "이미 구매한 노드입니다.");
                else
                    SendNotification(sender, "이전 단계를 먼저 구매해야 합니다.");
            }
        }

        [Command(requiresAuthority = false)]
        public void CmdUpgradeSkillWithLevel(string skillId, int skillIndex, int price, int npcLevel, int requiredNpcLevel, NetworkConnectionToClient sender = null)
        {
            Debug.Log($"[CharacterShop][Server] CmdUpgradeSkillWithLevel - skillId:{skillId}, price:{price}, npcLv:{npcLevel}, required:{requiredNpcLevel}");

            PlayerAccount account = FindAccount(sender);
            if (account == null) return;
            CharacterUnit unit = account.currentSelectedCharacter;
            if (unit == null) return;

            if (account.currentGold < price)
            {
                SendNotification(sender, "골드가 부족합니다.");
                return;
            }

            bool success = unit.ApplySkillPurchase(skillId, npcLevel, requiredNpcLevel);
            if (success)
            {
                account.currentGold -= price;
                Debug.Log($"<color=green>[CharacterShop][Server] 스킬 습득 성공! skillId:{skillId}</color>");
                SendNotification(sender, $"[{skillId}] 스킬 습득 완료!");
            }
            else
            {
                if (npcLevel < requiredNpcLevel)
                    SendNotification(sender, "NPC 레벨이 부족합니다.");
                else
                    SendNotification(sender, "이미 보유한 스킬입니다.");
            }
        }

        [Command(requiresAuthority = false)]
        public void CmdEquipItem(string itemName, NetworkConnectionToClient sender = null)
        {
            Debug.Log($"[CharacterShop][Server] CmdEquipItem - item:{itemName}");

            PlayerAccount account = FindAccount(sender);
            if (account == null) return;
            CharacterUnit unit = account.currentSelectedCharacter;
            if (unit == null) return;

            if (string.IsNullOrWhiteSpace(itemName))
            {
                SendNotification(sender, "장착할 아이템 이름이 비어 있습니다.");
                return;
            }
            if (unit.GetItemAmount(itemName) <= 0)
            {
                SendNotification(sender, $"[{itemName}] 아이템이 인벤토리에 없습니다.");
                return;
            }

            string resolvedWeaponId = ResolveWeaponIdFromInventoryKey(itemName);

            if (unit.selectedWeaponId == resolvedWeaponId)
            {
                unit.selectedWeaponId = "";
                SendNotification(sender, $"[{itemName}] 장착 해제.");
            }
            else
            {
                unit.selectedWeaponId = resolvedWeaponId;
                SendNotification(sender, $"[{itemName}] 장착 완료.");
            }
        }

        // ─── 알림 ──────────────────────────────────────────────────────

        [Server]
        private void SendNotification(NetworkConnectionToClient target, string message)
        {
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
