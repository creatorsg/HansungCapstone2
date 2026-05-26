using Mirror;
using UnityEngine;

namespace Lsy
{
    public class CharacterShop : NetworkBehaviour
    {
        [Server]
        private PlayerAccount FindAccount(NetworkConnectionToClient sender)
        {
            if (sender == null) return null;
            foreach (var account in FindObjectsByType<PlayerAccount>(FindObjectsSortMode.None))
                if (account.connectionToClient == sender)
                    return account;
            return null;
        }

        [Command(requiresAuthority = false)]
        public void CmdBuyItem(string itemName, int price, NetworkConnectionToClient sender = null)
        {
            BuyConsumableInternal(itemName, price, sender);
        }

        [Command(requiresAuthority = false)]
        public void CmdBuyConsumable(string itemName, int price, NetworkConnectionToClient sender = null)
        {
            BuyConsumableInternal(itemName, price, sender);
        }

        [Command(requiresAuthority = false)]
        public void CmdBuyEquipment(string equipmentName, int price, NetworkConnectionToClient sender = null)
        {
            BuyEquipmentInternal(equipmentName, price, sender);
        }

        [Server]
        private void BuyConsumableInternal(string itemName, int price, NetworkConnectionToClient sender)
        {
            Debug.Log($"[CharacterShop][Server] BuyConsumable - {itemName}, {price}G");

            PlayerAccount account = FindAccount(sender);
            if (account == null) return;
            CharacterUnit unit = account.currentSelectedCharacter;
            if (unit == null) return;

            if (account.currentGold < price)
            {
                SendNotification(sender, "Gold is not enough.");
                return;
            }

            if (unit.GetItemAmount(itemName) >= 5)
            {
                SendNotification(sender, $"[{itemName}] already has 5.");
                return;
            }

            Consum data = ItemManager.Instance != null ? ItemManager.Instance.GetItemData(itemName) : null;
            if (data == null || data.ConsumItem == null)
            {
                SendNotification(sender, $"Consumable data not found: {itemName}");
                return;
            }

            account.currentGold -= price;
            unit.AddItemWithInfo(new InventoryItem
            {
                itemName = data.ConsumItem.Name,
                Type = ItemType.Consumable,
                ConsumInfo = data.ConsumItem,
                amount = 1
            });

            SendNotification(sender, $"Purchased [{itemName}].");
        }

        [Server]
        private void BuyEquipmentInternal(string equipmentName, int price, NetworkConnectionToClient sender)
        {
            Debug.Log($"[CharacterShop][Server] BuyEquipment - {equipmentName}, {price}G");

            PlayerAccount account = FindAccount(sender);
            if (account == null) return;
            CharacterUnit unit = account.currentSelectedCharacter;
            if (unit == null) return;

            if (account.currentGold < price)
            {
                SendNotification(sender, "Gold is not enough.");
                return;
            }

            if (unit.GetItemAmount(equipmentName) >= 5)
            {
                SendNotification(sender, $"[{equipmentName}] already has 5.");
                return;
            }

            Equipment data = ItemManager.Instance != null ? ItemManager.Instance.GetEqpData(equipmentName) : null;
            if (data == null || data.EqpItem == null)
            {
                SendNotification(sender, $"Equipment data not found: {equipmentName}");
                return;
            }

            account.currentGold -= price;
            unit.AddItemWithInfo(new InventoryItem
            {
                itemName = data.EqpItem.Name,
                Type = data.itemType,
                EquipInfo = data.EqpItem,
                amount = 1
            });

            SendNotification(sender, $"Purchased [{equipmentName}].");
        }

        [Command(requiresAuthority = false)]
        public void CmdInvestToNPC(uint npcNetId, int amount, NetworkConnectionToClient sender = null)
        {
            Debug.Log($"[CharacterShop][Server] CmdInvestToNPC - netId:{npcNetId}, amount:{amount}");

            PlayerAccount account = FindAccount(sender);
            if (account == null) return;

            if (account.currentGold < amount)
            {
                SendNotification(sender, "Gold is not enough.");
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
                SendNotification(sender, "Gold is not enough.");
                return;
            }

            if (unit.ApplyBartenderHeal())
            {
                account.currentGold -= price;
                SendNotification(sender, $"{unit.characterName} recovered HP/SAN.");
            }
            else
            {
                SendNotification(sender, "HP/SAN is already full.");
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
                SendNotification(sender, "Gold is not enough.");
                return;
            }

            bool success = unit.ApplyBlacksmithUpgrade(weaponId, nodeIndex, npcLevel);
            if (success)
            {
                account.currentGold -= price;
                Debug.Log($"<color=green>[CharacterShop][Server] Upgrade success. weaponId:{weaponId}, node:{nodeIndex}</color>");
                SendNotification(sender, $"[{weaponId}] upgrade level {nodeIndex + 1} complete.");
            }
            else
            {
                if (unit.selectedWeaponId != "" && unit.selectedWeaponId != weaponId)
                    SendNotification(sender, "Another weapon is already selected.");
                else if (npcLevel < nodeIndex + 1)
                    SendNotification(sender, "NPC level is too low.");
                else if (unit.purchasedNodeCount > nodeIndex)
                    SendNotification(sender, "Already purchased node.");
                else
                    SendNotification(sender, "Purchase previous node first.");
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
                SendNotification(sender, "Gold is not enough.");
                return;
            }

            bool success = unit.ApplySkillPurchase(skillId, npcLevel, requiredNpcLevel);
            if (success)
            {
                account.currentGold -= price;
                Debug.Log($"<color=green>[CharacterShop][Server] Skill purchase success. skillId:{skillId}</color>");
                SendNotification(sender, $"[{skillId}] skill purchase complete.");
            }
            else
            {
                if (npcLevel < requiredNpcLevel)
                    SendNotification(sender, "NPC level is too low.");
                else
                    SendNotification(sender, "Skill already owned.");
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
                SendNotification(sender, "Item name is empty.");
                return;
            }

            if (unit.GetItemAmount(itemName) <= 0)
            {
                SendNotification(sender, $"[{itemName}] is not in inventory.");
                return;
            }

            unit.selectedWeaponId = itemName;
            SendNotification(sender, $"[{itemName}] equipped.");
        }

        [Server]
        private void SendNotification(NetworkConnectionToClient target, string message)
        {
            if (target != null)
                RpcNotify(target, message);
            else
                Debug.LogWarning("[CharacterShop] sender is null; notification skipped.");
        }

        [TargetRpc]
        private void RpcNotify(NetworkConnectionToClient target, string message)
        {
            Debug.Log($"<color=white>[CharacterShop][Client] Notify: {message}</color>");
        }
    }
}
