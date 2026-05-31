using Mirror;
using UnityEngine;

namespace Lsy
{
    /// <summary>
    /// NPC 상호작용용 커맨드 처리.
    /// 골드는 PlayerAccount가 관리하므로 sender의 PlayerAccount를 찾아 처리합니다.
    /// requiresAuthority = false: 어떤 클라이언트든 호출 가능 (sender로 호출자 식별).
    /// </summary>
    public class CharacterShop : NetworkBehaviour
    {
        // sender → PlayerAccount 헬퍼

        [Server]
        private PlayerAccount FindAccount(NetworkConnectionToClient sender)
        {
            if (sender == null) return null;
            foreach (var account in FindObjectsByType<PlayerAccount>(FindObjectsSortMode.None))
                if (account.connectionToClient == sender)
                    return account;
            return null;
        }

        // 커맨드 ────────────────────────────────────────────────────────────

        [Command(requiresAuthority = false)]
        public void CmdBuyItem(string itemName, int price, NetworkConnectionToClient sender = null)
        {
            Debug.Log($"[CharacterShop][Server] CmdBuyItem - {itemName}, {price}G");
            // 소모품 구매 공통 서버 처리 — NPCPopupUI 병합 후 호출명을 모두 지원합니다.
            BuyInventoryItem(itemName, price, sender, CreateConsumableInventoryItem);
        }

        // NPCPopupUI가 호출하던 병합 전 커맨드명 호환용
        [Command(requiresAuthority = false)]
        public void CmdBuyConsumable(string itemName, int price, NetworkConnectionToClient sender = null)
        {
            Debug.Log($"[CharacterShop][Server] CmdBuyConsumable - {itemName}, {price}G");
            BuyInventoryItem(itemName, price, sender, CreateConsumableInventoryItem);
        }

        // NPCPopupUI의 장비 구매 호출 — 서버 인벤토리 추가 처리
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
                SendNotification(sender, $"[{itemName}] 구매 실패: ItemManager에 등록되지 않은 아이템입니다.");
                return;
            }

            if (account.currentGold < price)
            {
                SendNotification(sender, "골드가 부족합니다.");
                return;
            }
            if (unit.GetItemAmount(itemName) >= 5)
            {
                SendNotification(sender, $"{itemName}은(는) 이미 5개를 보유하고 있습니다.");
                return;
            }

            account.currentGold -= price;

            unit.AddItemWithInfo(invItem);
            account.SyncInventoryToPlayerData();

            SendNotification(sender, $"[알림] {itemName} 구매 완료.");
        }

        [Server]
        private InventoryItem CreateConsumableInventoryItem(string itemName)
        {
            //[정] 바텐NPC 업 매 이는 ItemManager.AllItems(Consum)선 용니
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

            //[정] ItemData/ItemSO fallback 거. NPC 매 이 으구매 패 처리니
            return default;
        }

        [Server]
        private InventoryItem CreateEquipmentInventoryItem(string itemName)
        {
            //[정] 장NPC 업 매 이는 ItemManager.AllEqps(Equipment)선 용니
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

            //[정] ItemSO/Resources fallback 거. NPC 매 이 으구매 패 처리니
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
                Debug.Log($"<color=green>[CharacterShop][Server] 강화 성공! weaponId:{weaponId}, node:{nodeIndex}</color>");
                SendNotification(sender, $"[{weaponId}] {nodeIndex + 1}단계 강화 완료!");
            }
            else
            {
                int currentWeaponLevel = unit.GetBlacksmithWeaponLevel(weaponId);
                // 강화 실패 사유별 안내
                if (npcLevel < nodeIndex + 1)
                    SendNotification(sender, "NPC 레벨이 부족합니다.");
                else if (currentWeaponLevel > nodeIndex)
                    SendNotification(sender, "이미 강화된 단계입니다.");
                else
                    SendNotification(sender, "이전 단계를 먼저 강화해야 합니다.");
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
                SendNotification(sender, "골드가 부족합니다.");
                return;
            }

            // npcLevel 게이트는 ApplySkillUpgrade가 아니라 여기서 한 번 더 명시 검사(선행 레벨은 ApplySkillUpgrade가 검증)
            if (npcLevel < requiredNpcLevel) { SendNotification(sender, "NPC Lv 부족"); return; }
            bool success = unit.ApplySkillUpgrade(skillIndex, targetLevel, skillId);
            if (success)
            {
                bool synced = false;
                foreach (var pd in FindObjectsByType<PlayerData>(FindObjectsSortMode.None))
                {
                    if (pd.connectionToClient != sender) continue;
                    if (pd.FinalHeroCode != unit.heroCode) continue;

                    synced = PlayerAccount.SyncSkillUpgradesToPlayerData(pd, unit.mySkills, "shop");
                    break;
                }
                if (!synced)
                    synced = account.SyncCurrentCharacterSkillUpgradesToPlayerData();
                if (!synced)
                    Debug.LogWarning($"[CharacterShop] SkillBridge sync failed after upgrade. hero={unit.heroCode}, idx={skillIndex}, lv={targetLevel}");

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

        /// <summary>
        /// 고유 특성을 targetLevel로 강화합니다.
        /// 가격 검증은 서버에서 UniqueTraitSO를 직접 조회합니다.
        /// </summary>
        [Command(requiresAuthority = false)]
        public void CmdUpgradeUniqueTrait(int targetLevel, string heroCode, NetworkConnectionToClient sender = null)
        {
            Debug.Log($"[CharacterShop][Server] CmdUpgradeUniqueTrait - targetLevel:{targetLevel}, heroCode:{heroCode}");

            PlayerAccount account = FindAccount(sender);
            if (account == null) return;

            // currentSelectedCharacter는 클라이언트 기준이라 서버에서 신뢰할 수 없음.
            // heroCode + connectionToClient로 정확한 CharacterUnit을 찾습니다.
            CharacterUnit unit = null;
            foreach (var u in FindObjectsByType<CharacterUnit>(FindObjectsSortMode.None))
            {
                if (u.connectionToClient == sender && u.heroCode == heroCode)
                {
                    unit = u;
                    break;
                }
            }
            if (unit == null)
            {
                Debug.LogWarning($"[CharacterShop] heroCode={heroCode}인 CharacterUnit을 찾지 못했습니다.");
                return;
            }

            // 서버에서 직접 UniqueTraitSO 조회하여 가격 검증 (클라이언트 가격 신뢰 X)
            if (!CharacterRegistry.TryGet(unit.heroCode, out var entry) || entry.UniqueTrait == null)
            {
                SendNotification(sender, "고유 특성 데이터를 찾을 수 없습니다.");
                return;
            }

            if (targetLevel < 1 || targetLevel > UniqueTraitSO.MaxLevel)
            {
                SendNotification(sender, "잘못된 강화 단계입니다.");
                return;
            }

            int price = entry.UniqueTrait.GetLevel(targetLevel).price;

            if (account.currentGold < price)
            {
                SendNotification(sender, $"골드가 부족합니다. (필요: {price}G)");
                return;
            }

            bool success = unit.ApplyUniqueTraitUpgrade(targetLevel);
            if (success)
            {
                account.currentGold -= price;
                SendNotification(sender, $"고유 특성 {targetLevel}단계 강화 완료! (-{price}G)");
            }
            else
            {
                SendNotification(sender, "강화 조건이 맞지 않습니다.");
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
                SendNotification(sender, "착이름비어 습다.");
                return;
            }
            if (unit.GetItemAmount(itemName) <= 0)
            {
                SendNotification(sender, $"[{itemName}] 이이 벤리습다.");
                return;
            }

            unit.selectedWeaponId = itemName;
            SendNotification(sender, $"[{itemName}] 착 료.");
        }

            //림 ?

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
