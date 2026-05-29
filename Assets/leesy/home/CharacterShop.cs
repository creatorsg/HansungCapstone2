using Mirror;
using UnityEngine;

namespace Lsy
{
    /// <summary>
    /// NPC ?�호?�용 커맨??처리.
    /// 골드??PlayerAccount 귀?�이므�?sender??PlayerAccount�?찾아 처리?�니??
    /// requiresAuthority = false: ?�떤 ?�라?�언?�든 ?�출 가??(sender�??�출???�별).
    /// </summary>
    public class CharacterShop : NetworkBehaviour
    {
        // ?�?�?� sender ??PlayerAccount ?�퍼 ?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�

        [Server]
        private PlayerAccount FindAccount(NetworkConnectionToClient sender)
        {
            if (sender == null) return null;
            foreach (var account in FindObjectsByType<PlayerAccount>(FindObjectsSortMode.None))
                if (account.connectionToClient == sender)
                    return account;
            return null;
        }

        // ?�?�?� 커맨???�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�

        [Command(requiresAuthority = false)]
        public void CmdBuyItem(string itemName, int price, NetworkConnectionToClient sender = null)
        {
            Debug.Log($"[CharacterShop][Server] CmdBuyItem - {itemName}, {price}G");
            // [?�정] ?�모??구매??공통 ?�버 처리�??�임??NPCPopupUI??병합 ?????�출명을 모두 지?�합?�다.
            BuyInventoryItem(itemName, price, sender, CreateConsumableInventoryItem);
        }

        // [?�정] NPCPopupUI가 ?�출?�던 병합 ??커맨?�명???��??�니??
        [Command(requiresAuthority = false)]
        public void CmdBuyConsumable(string itemName, int price, NetworkConnectionToClient sender = null)
        {
            Debug.Log($"[CharacterShop][Server] CmdBuyConsumable - {itemName}, {price}G");
            BuyInventoryItem(itemName, price, sender, CreateConsumableInventoryItem);
        }

        // [?�정] NPCPopupUI???�비 구매 ?�출???�버 ?�벤?�리 추�?�??�결?�니??
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
                SendNotification(sender, $"[{itemName}] ?�매 ?�이?��? ItemManager???�록?�어 ?��? ?�습?�다.");
                return;
            }

            if (account.currentGold < price)
            {
                SendNotification(sender, "골드가 부족합?�다.");
                return;
            }
            if (unit.GetItemAmount(itemName) >= 5)
            {
                SendNotification(sender, $"{itemName}?�(?? ?��? 5개�? ?��??�고 ?�습?�다.");
                return;
            }

            account.currentGold -= price;

            unit.AddItemWithInfo(invItem);
            account.SyncInventoryToPlayerData();

            SendNotification(sender, $"[?�스???�림] {itemName} 구매 ?�료.");
        }

        [Server]
        private InventoryItem CreateConsumableInventoryItem(string itemName)
        {
            // [?�정] 바텐??NPC ?�업 ?�매 ?�이?�는 ItemManager.AllItems(Consum)�??�선 ?�용?�니??
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

            // [?�정] ItemData/ItemSO fallback ?�거. NPC ?�매 ?�이?��? ?�으�?구매 ?�패 처리?�니??
            return default;
        }

        [Server]
        private InventoryItem CreateEquipmentInventoryItem(string itemName)
        {
            // [?�정] ?�?�장??NPC ?�업 ?�매 ?�이?�는 ItemManager.AllEqps(Equipment)�??�선 ?�용?�니??
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

            // [?�정] ItemSO/Resources fallback ?�거. NPC ?�매 ?�이?��? ?�으�?구매 ?�패 처리?�니??
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
                SendNotification(sender, "골드가 부족합?�다.");
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
                SendNotification(sender, "골드가 부족합?�다.");
                return;
            }
            if (unit.ApplyBartenderHeal())
            {
                account.currentGold -= price;
                SendNotification(sender, $"{unit.characterName}??체력/?�신?�이 ?�복?�었?�니??");
            }
            else
            {
                SendNotification(sender, "?��? 체력�??�신?�이 최�??�니??");
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
                SendNotification(sender, "골드가 부족합?�다.");
                return;
            }

            bool success = unit.ApplyBlacksmithUpgrade(weaponId, nodeIndex, npcLevel);
            if (success)
            {
                account.currentGold -= price;
                Debug.Log($"<color=green>[CharacterShop][Server] 강화 ?�공! weaponId:{weaponId}, node:{nodeIndex}</color>");
                SendNotification(sender, $"[{weaponId}] {nodeIndex + 1}?�계 강화 ?�료!");
            }
            else
            {
                int currentWeaponLevel = unit.GetBlacksmithWeaponLevel(weaponId);
                // [����] ���⺰ ��ȭ �ܰ� �������� ���� ������ �ȳ��մϴ�.
                if (npcLevel < nodeIndex + 1)
                    SendNotification(sender, "NPC ������ �����մϴ�.");
                else if (currentWeaponLevel > nodeIndex)
                    SendNotification(sender, "�̹� ������ ����Դϴ�.");
                else
                    SendNotification(sender, "���� �ܰ踦 ���� �����ؾ� �մϴ�.");
            }
        }

        [Command(requiresAuthority = false)]
        public void CmdUpgradeSkillWithLevel(int skillIndex, int targetLevel, int npcLevel, NetworkConnectionToClient sender = null)
        {
            // [정보상 일원화] price/requiredNpcLevel/skillId는 서버가 SkillUpgradeRegistry에서 직접 조회한다(클라 신뢰 X).
            // 아래 기존 코드가 price/requiredNpcLevel/skillId 지역변수를 사용하므로 여기서 미리 채운다.
            string skillId = "";
            int price = int.MaxValue;          // 노드 미발견 시 골드 체크에서 자연 reject
            int requiredNpcLevel = int.MaxValue;
            var __acc = FindAccount(sender);
            if (__acc != null && __acc.currentSelectedCharacter != null &&
                SkillUpgradeRegistry.TryGet(__acc.currentSelectedCharacter.heroCode, skillIndex, targetLevel, out var __node))
            {
                skillId          = __node.skillName;
                price            = __node.price;
                requiredNpcLevel = __node.requiredNpcLevel;
            }
            Debug.Log($"[CharacterShop][Server] CmdUpgradeSkillWithLevel - idx:{skillIndex}, targetLv:{targetLevel}, price:{price}, npcLv:{npcLevel}, required:{requiredNpcLevel}");

            PlayerAccount account = FindAccount(sender);
            if (account == null) return;
            CharacterUnit unit = account.currentSelectedCharacter;
            if (unit == null) return;

            if (account.currentGold < price)
            {
                SendNotification(sender, "골드가 부족합?�다.");
                return;
            }

            // npcLevel 게이트는 ApplySkillUpgrade가 아니라 여기서 한 번 더 명시 검사(선행 레벨은 ApplySkillUpgrade가 검증)
            if (npcLevel < requiredNpcLevel) { SendNotification(sender, "NPC Lv 부족"); return; }
            bool success = unit.ApplySkillUpgrade(skillIndex, targetLevel, skillId);
            if (success)
            {
                account.currentGold -= price;
                Debug.Log($"<color=green>[CharacterShop][Server] ?�킬 ?�득 ?�공! skillId:{skillId}</color>");
                SendNotification(sender, $"[{skillId}] ?�킬 ?�득 ?�료!");
            }
            else
            {
                if (npcLevel < requiredNpcLevel)
                    SendNotification(sender, "NPC ?�벨??부족합?�다.");
                else
                    SendNotification(sender, "?��? 보유???�킬?�니??");
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
                SendNotification(sender, "?�착???�이???�름??비어 ?�습?�다.");
                return;
            }
            if (unit.GetItemAmount(itemName) <= 0)
            {
                SendNotification(sender, $"[{itemName}] ?�이?�이 ?�벤?�리???�습?�다.");
                return;
            }

            unit.selectedWeaponId = itemName;
            SendNotification(sender, $"[{itemName}] ?�착 ?�료.");
        }

        // ?�?�?� ?�림 ?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�

        [Server]
        private void SendNotification(NetworkConnectionToClient target, string message)
        {
            if (target != null)
                RpcNotify(target, message);
            else
                Debug.LogWarning("[CharacterShop] sender가 NULL?�라 ?�림??보낼 ???�습?�다.");
        }

        [TargetRpc]
        private void RpcNotify(NetworkConnectionToClient target, string message)
        {
            Debug.Log($"<color=white>[CharacterShop][Client] ?�림 ?�신: {message}</color>");
        }
    }
}
