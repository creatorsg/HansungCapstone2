using Mirror;
using UnityEngine;

namespace Lsy
{
    /// <summary>
    /// NPC ?í˜¸?‘ìš© ì»¤ë§¨??ì²˜ë¦¬.
    /// ê³¨ë“œ??PlayerAccount ê·€?ì´ë¯€ë¡?sender??PlayerAccountë¥?ì°¾ì•„ ì²˜ë¦¬?©ë‹ˆ??
    /// requiresAuthority = false: ?´ë–¤ ?´ë¼?´ì–¸?¸ë“  ?¸ì¶œ ê°€??(senderë¡??¸ì¶œ???ë³„).
    /// </summary>
    public class CharacterShop : NetworkBehaviour
    {
        // ?€?€?€ sender ??PlayerAccount ?¬í¼ ?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€

        [Server]
        private PlayerAccount FindAccount(NetworkConnectionToClient sender)
        {
            if (sender == null) return null;
            foreach (var account in FindObjectsByType<PlayerAccount>(FindObjectsSortMode.None))
                if (account.connectionToClient == sender)
                    return account;
            return null;
        }

        // ?€?€?€ ì»¤ë§¨???€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€

        [Command(requiresAuthority = false)]
        public void CmdBuyItem(string itemName, int price, NetworkConnectionToClient sender = null)
        {
            Debug.Log($"[CharacterShop][Server] CmdBuyItem - {itemName}, {price}G");
            // [?˜ì •] ?Œëª¨??êµ¬ë§¤??ê³µí†µ ?œë²„ ì²˜ë¦¬ë¡??„ì„??NPCPopupUI??ë³‘í•© ?????¸ì¶œëª…ì„ ëª¨ë‘ ì§€?í•©?ˆë‹¤.
            BuyInventoryItem(itemName, price, sender, CreateConsumableInventoryItem);
        }

        // [?˜ì •] NPCPopupUIê°€ ?¸ì¶œ?˜ë˜ ë³‘í•© ??ì»¤ë§¨?œëª…??? ì??©ë‹ˆ??
        [Command(requiresAuthority = false)]
        public void CmdBuyConsumable(string itemName, int price, NetworkConnectionToClient sender = null)
        {
            Debug.Log($"[CharacterShop][Server] CmdBuyConsumable - {itemName}, {price}G");
            BuyInventoryItem(itemName, price, sender, CreateConsumableInventoryItem);
        }

        // [?˜ì •] NPCPopupUI???¥ë¹„ êµ¬ë§¤ ?¸ì¶œ???œë²„ ?¸ë²¤? ë¦¬ ì¶”ê?ë¡??°ê²°?©ë‹ˆ??
        [Command(requiresAuthority = false)]
        public void CmdBuyEquipment(string itemName, int price, NetworkConnectionToClient sender = null)
        {
            Debug.Log($"[CharacterShop][Server] CmdBuyEquipment - {itemName}, {price}G");
            BuyInventoryItem(itemName, price, sender, CreateEquipmentInventoryItem);
        }

        [Server]
        private void BuyInventoryItem(string itemName, int price, NetworkConnectionToClient sender, System.Func<string, InventoryItem> createItem)
        {
            PlayerAccount account = FindAccount(sender);
            if (account == null) return;
            CharacterUnit unit = account.currentSelectedCharacter;
            if (unit == null) return;

            InventoryItem invItem = createItem != null ? createItem(itemName) : default;
            if (string.IsNullOrEmpty(invItem.itemName))
            {
                SendNotification(sender, $"[{itemName}] ?ë§¤ ?°ì´?°ê? ItemManager???±ë¡?˜ì–´ ?ˆì? ?ŠìŠµ?ˆë‹¤.");
                return;
            }

            if (account.currentGold < price)
            {
                SendNotification(sender, "ê³¨ë“œê°€ ë¶€ì¡±í•©?ˆë‹¤.");
                return;
            }
            if (unit.GetItemAmount(itemName) >= 5)
            {
                SendNotification(sender, $"{itemName}?€(?? ?´ë? 5ê°œë? ?Œì??˜ê³  ?ˆìŠµ?ˆë‹¤.");
                return;
            }

            account.currentGold -= price;

            unit.AddItemWithInfo(invItem);
            account.SyncInventoryToPlayerData();

            SendNotification(sender, $"[?œìŠ¤???Œë¦¼] {itemName} êµ¬ë§¤ ?„ë£Œ.");
        }

        [Server]
        private InventoryItem CreateConsumableInventoryItem(string itemName)
        {
            // [?˜ì •] ë°”í…??NPC ?ì—… ?ë§¤ ?°ì´?°ëŠ” ItemManager.AllItems(Consum)ë¥??°ì„  ?¬ìš©?©ë‹ˆ??
            Consum consumData = ItemManager.Instance != null ? ItemManager.Instance.GetConsumData(itemName) : null;
            if (consumData != null && consumData.ConsumItem != null)
            {
                return new InventoryItem
                {
                    itemName   = consumData.ConsumItem.Name,
                    Type       = ItemType.Consumable,
                    ConsumInfo = consumData.ConsumItem,
                    amount     = 1
                };
            }

            // [?˜ì •] ItemData/ItemSO fallback ?œê±°. NPC ?ë§¤ ?°ì´?°ê? ?†ìœ¼ë©?êµ¬ë§¤ ?¤íŒ¨ ì²˜ë¦¬?©ë‹ˆ??
            return default;
        }

        [Server]
        private InventoryItem CreateEquipmentInventoryItem(string itemName)
        {
            // [?˜ì •] ?€?¥ì¥??NPC ?ì—… ?ë§¤ ?°ì´?°ëŠ” ItemManager.AllEqps(Equipment)ë¥??°ì„  ?¬ìš©?©ë‹ˆ??
            Equipment equipmentData = ItemManager.Instance != null ? ItemManager.Instance.GetEquipmentData(itemName) : null;
            if (equipmentData != null && equipmentData.EqpItem != null)
            {
                return new InventoryItem
                {
                    itemName  = equipmentData.EqpItem.Name,
                    Type      = equipmentData.itemType,
                    EquipInfo = equipmentData.EqpItem,
                    amount    = 1
                };
            }

            // [?˜ì •] ItemSO/Resources fallback ?œê±°. NPC ?ë§¤ ?°ì´?°ê? ?†ìœ¼ë©?êµ¬ë§¤ ?¤íŒ¨ ì²˜ë¦¬?©ë‹ˆ??
            return default;
        }

        [Command(requiresAuthority = false)]
        public void CmdInvestToNPC(uint npcNetId, int amount, NetworkConnectionToClient sender = null)
        {
            Debug.Log($"[CharacterShop][Server] CmdInvestToNPC - netId:{npcNetId}, amount:{amount}");

            PlayerAccount account = FindAccount(sender);
            if (account == null) return;

            if (account.currentGold < amount)
            {
                SendNotification(sender, "ê³¨ë“œê°€ ë¶€ì¡±í•©?ˆë‹¤.");
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
                SendNotification(sender, "ê³¨ë“œê°€ ë¶€ì¡±í•©?ˆë‹¤.");
                return;
            }
            if (unit.ApplyBartenderHeal())
            {
                account.currentGold -= price;
                SendNotification(sender, $"{unit.characterName}??ì²´ë ¥/?•ì‹ ?¥ì´ ?Œë³µ?˜ì—ˆ?µë‹ˆ??");
            }
            else
            {
                SendNotification(sender, "?´ë? ì²´ë ¥ê³??•ì‹ ?¥ì´ ìµœë??…ë‹ˆ??");
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
                SendNotification(sender, "ê³¨ë“œê°€ ë¶€ì¡±í•©?ˆë‹¤.");
                return;
            }

            bool success = unit.ApplyBlacksmithUpgrade(weaponId, nodeIndex, npcLevel);
            if (success)
            {
                account.currentGold -= price;
                Debug.Log($"<color=green>[CharacterShop][Server] ê°•í™” ?±ê³µ! weaponId:{weaponId}, node:{nodeIndex}</color>");
                SendNotification(sender, $"[{weaponId}] {nodeIndex + 1}?¨ê³„ ê°•í™” ?„ë£Œ!");
            }
            else
            {
                int currentWeaponLevel = unit.GetBlacksmithWeaponLevel(weaponId);
                // [¼öÁ¤] ¹«±âº° °­È­ ´Ü°è ±âÁØÀ¸·Î ½ÇÆĞ »çÀ¯¸¦ ¾È³»ÇÕ´Ï´Ù.
                if (npcLevel < nodeIndex + 1)
                    SendNotification(sender, "NPC ·¹º§ÀÌ ºÎÁ·ÇÕ´Ï´Ù.");
                else if (currentWeaponLevel > nodeIndex)
                    SendNotification(sender, "ÀÌ¹Ì ±¸¸ÅÇÑ ³ëµåÀÔ´Ï´Ù.");
                else
                    SendNotification(sender, "ÀÌÀü ´Ü°è¸¦ ¸ÕÀú ±¸¸ÅÇØ¾ß ÇÕ´Ï´Ù.");
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
                SendNotification(sender, "ê³¨ë“œê°€ ë¶€ì¡±í•©?ˆë‹¤.");
                return;
            }

            bool success = unit.ApplySkillPurchase(skillId, npcLevel, requiredNpcLevel);
            if (success)
            {
                account.currentGold -= price;
                Debug.Log($"<color=green>[CharacterShop][Server] ?¤í‚¬ ?µë“ ?±ê³µ! skillId:{skillId}</color>");
                SendNotification(sender, $"[{skillId}] ?¤í‚¬ ?µë“ ?„ë£Œ!");
            }
            else
            {
                if (npcLevel < requiredNpcLevel)
                    SendNotification(sender, "NPC ?ˆë²¨??ë¶€ì¡±í•©?ˆë‹¤.");
                else
                    SendNotification(sender, "?´ë? ë³´ìœ ???¤í‚¬?…ë‹ˆ??");
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
                SendNotification(sender, "?¥ì°©???„ì´???´ë¦„??ë¹„ì–´ ?ˆìŠµ?ˆë‹¤.");
                return;
            }
            if (unit.GetItemAmount(itemName) <= 0)
            {
                SendNotification(sender, $"[{itemName}] ?„ì´?œì´ ?¸ë²¤? ë¦¬???†ìŠµ?ˆë‹¤.");
                return;
            }

            unit.selectedWeaponId = itemName;
            SendNotification(sender, $"[{itemName}] ?¥ì°© ?„ë£Œ.");
        }

        // ?€?€?€ ?Œë¦¼ ?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€

        [Server]
        private void SendNotification(NetworkConnectionToClient target, string message)
        {
            if (target != null)
                RpcNotify(target, message);
            else
                Debug.LogWarning("[CharacterShop] senderê°€ NULL?´ë¼ ?Œë¦¼??ë³´ë‚¼ ???†ìŠµ?ˆë‹¤.");
        }

        [TargetRpc]
        private void RpcNotify(NetworkConnectionToClient target, string message)
        {
            Debug.Log($"<color=white>[CharacterShop][Client] ?Œë¦¼ ?˜ì‹ : {message}</color>");
        }
    }
}
