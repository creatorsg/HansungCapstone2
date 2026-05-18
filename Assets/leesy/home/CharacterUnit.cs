using Mirror;
using System;
using UnityEngine;

namespace Lsy
{
    [System.Serializable]
    public struct InventoryItem
    {
        public string itemName;
        public int amount;
    }

    public class CharacterUnit : NetworkBehaviour
    {
        private PlayerInfo myInfo;

        [SyncVar] public float maxHp;
        [SyncVar] public int maxSan;

        [SyncVar(hook = nameof(OnCurrentHpChanged))]
        public float currentHp;

        [SyncVar(hook = nameof(OnCurrentSanChanged))]
        public int currentSan;

        public readonly SyncList<PlayerSkill> mySkills = new SyncList<PlayerSkill>();
        public readonly SyncHashSet<string> unlockedNodeIds = new SyncHashSet<string>();

        [SyncVar] public string characterName;


        [SyncVar(hook = nameof(OnHeroPosChanged))]
        public int heroPos = -1;


        [SyncVar(hook = nameof(OnHeroCodeChanged))]
        public string heroCode = "";

        public readonly SyncList<InventoryItem> myInventory = new SyncList<InventoryItem>();

        [SyncVar(hook = nameof(OnSelectedWeaponIdChanged))]
        public string selectedWeaponId = "";

        [SyncVar(hook = nameof(OnPurchasedNodeCountChanged))]
        public int purchasedNodeCount = 0;

        public static event Action<CharacterUnit> OnLocalUnitSpawned;
        public static CharacterUnit LocalOwnedUnit { get; private set; }
        public static event Action<CharacterUnit> OnAnyUnitReady;
        public static event Action OnLocalInventoryChanged;
        public static event Action OnLocalUpgradeStateChanged;
        public static event Action<CharacterUnit> OnAnyUnitStatsChanged;

        private void OnCurrentHpChanged(float oldVal, float newVal) => OnAnyUnitStatsChanged?.Invoke(this);
        private void OnCurrentSanChanged(int oldVal, int newVal) => OnAnyUnitStatsChanged?.Invoke(this);

        private void OnHeroCodeChanged(string oldVal, string newVal)
        {
            if (!isOwned) return;
            RefreshUpgradeUI();
        }

        private void OnHeroPosChanged(int oldVal, int newVal)
        {
            if (newVal >= 0)
                OnAnyUnitReady?.Invoke(this);
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (heroPos >= 0)
                OnAnyUnitReady?.Invoke(this);
        }

        [Server]
        public void SetupFromData(CharacterData data)
        {
            characterName = data.charName;
            maxHp = data.maxHp;
            maxSan = data.maxSan;
            currentHp = maxHp;
            currentSan = maxSan;

            if (myInfo == null) myInfo = new PlayerInfo();
            myInfo.Hp = maxHp;
            myInfo.San = maxSan;

            Debug.Log($"<color=green>[캐릭터] 초기화 완료 (CharacterData): {characterName} (HP:{maxHp})</color>");
        }

        /// <summary>
        /// CharacterSelect에서 생성된 PlayerData를 기반으로 캐릭터를 초기화합니다.

        /// </summary>
        [Server]
        public void SetupFromPlayerData(PlayerData pd)
        {
            characterName = string.IsNullOrEmpty(pd.Info.Name) ? pd.FinalHeroCode : pd.Info.Name;
            heroCode = pd.FinalHeroCode;
            heroPos = pd.FinalHeroPos;
            maxHp = pd.Info.Hp;
            maxSan = pd.Info.San;
            currentHp = maxHp;
            currentSan = maxSan;

            if (myInfo == null) myInfo = new PlayerInfo();
            myInfo.Hp = maxHp;
            myInfo.San = maxSan;

            Debug.Log($"<color=green>[캐릭터] 초기화 완료 (PlayerData): {characterName} / code={pd.FinalHeroCode} (HP:{maxHp})</color>");
        }

        public override void OnStartAuthority()
        {
            base.OnStartAuthority();

            LocalOwnedUnit = this;

            if (PlayerAccount.LocalInstance != null)
                PlayerAccount.LocalInstance.currentSelectedCharacter = this;

            myInventory.Callback -= OnInventoryChanged;
            myInventory.Callback += OnInventoryChanged;

            mySkills.Callback -= OnSkillsChanged;
            mySkills.Callback += OnSkillsChanged;

            unlockedNodeIds.OnChange -= OnUnlockedNodeIdsChanged;
            unlockedNodeIds.OnChange += OnUnlockedNodeIdsChanged;

            OnLocalUnitSpawned?.Invoke(this);
            RefreshUpgradeUI();
        }

        public override void OnStopAuthority()
        {
            base.OnStopAuthority();
            if (LocalOwnedUnit == this)
                LocalOwnedUnit = null;
            myInventory.Callback -= OnInventoryChanged;
            mySkills.Callback -= OnSkillsChanged;
            unlockedNodeIds.OnChange -= OnUnlockedNodeIdsChanged;
        }

        private void OnSelectedWeaponIdChanged(string oldVal, string newVal)
        {
            if (!isOwned) return;
            OnLocalInventoryChanged?.Invoke();
            RefreshUpgradeUI();
        }

        private void OnPurchasedNodeCountChanged(int oldVal, int newVal)
        {
            if (!isOwned) return;
            RefreshUpgradeUI();
        }

        private void OnSkillsChanged(SyncList<PlayerSkill>.Operation op, int index, PlayerSkill oldItem, PlayerSkill newItem)
        {
            if (!isOwned) return;
            RefreshUpgradeUI();
        }

        private void OnUnlockedNodeIdsChanged(SyncSet<string>.Operation op, string item)
        {
            if (!isOwned) return;
            RefreshUpgradeUI();
        }

        private void RefreshUpgradeUI()
        {
            OnLocalUpgradeStateChanged?.Invoke();
        }

        private void OnInventoryChanged(SyncList<InventoryItem>.Operation op, int itemIndex, InventoryItem oldItem, InventoryItem newItem)
        {
            if (PlayerAccount.LocalInstance == null) return;
            if (PlayerAccount.LocalInstance.currentSelectedCharacter != this) return;
            if (!isOwned) return;

            OnLocalInventoryChanged?.Invoke();
        }

        [Server]
        public int GetItemAmount(string itemName)
        {
            foreach (var item in myInventory)
                if (item.itemName == itemName) return item.amount;
            return 0;
        }

        [Server]
        public void AddItem(string itemName, int amount = 1)
        {
            for (int i = 0; i < myInventory.Count; i++)
            {
                if (myInventory[i].itemName == itemName)
                {
                    InventoryItem temp = myInventory[i];
                    temp.amount += amount;
                    myInventory[i] = temp;
                    return;
                }
            }
            myInventory.Add(new InventoryItem { itemName = itemName, amount = amount });
        }

        [Server]
        public bool ApplyBartenderHeal()
        {
            if (currentHp >= maxHp && currentSan >= maxSan) return false;
            currentHp = maxHp;
            currentSan = maxSan;
            if (myInfo == null) myInfo = new PlayerInfo();
            myInfo.Hp = maxHp;
            myInfo.San = maxSan;
            return true;
        }

        /// <summary>


        /// </summary>
        [Server]
        public bool ApplyBlacksmithUpgrade(string weaponId, int nodeIndex, int npcLevel)
        {
            if (npcLevel < nodeIndex + 1) return false;
            if (selectedWeaponId != "" && selectedWeaponId != weaponId) return false;
            if (nodeIndex > 0 && purchasedNodeCount < nodeIndex) return false;
            if (purchasedNodeCount > nodeIndex) return false;

            selectedWeaponId = weaponId;
            purchasedNodeCount = nodeIndex + 1;

            return true;
        }

        /// <summary>

        /// </summary>
        [Server]
        public bool ApplySkillPurchase(string skillId, int npcLevel, int requiredNpcLevel, string nodeId = "")
        {
            return ApplySkillPurchase(string.IsNullOrEmpty(nodeId) ? skillId : nodeId, npcLevel);
        }

        [Server]
        public bool ApplySkillPurchase(string nodeId, int npcLevel)
        {
            if (!CanUnlockSkillNode(nodeId, npcLevel, out SkillTreeNodeSO node))
                return false;

            unlockedNodeIds.Add(nodeId);
            RecordInformantSkillNode(node);

            return true;
        }

        public bool CanUnlockSkillNode(string nodeId, out SkillTreeNodeSO node)
        {
            return CanUnlockSkillNode(nodeId, int.MaxValue, out node);
        }

        public bool CanUnlockSkillNode(string nodeId, int npcLevel, out SkillTreeNodeSO node)
        {
            node = null;
            if (string.IsNullOrEmpty(nodeId)) return false;

            string characterCode = SkillTreeCharacterCode;
            if (string.IsNullOrEmpty(characterCode)) return false;

            if (!SkillTreeRegistry.TryFind(characterCode, nodeId, out node))
                return false;

            if (npcLevel < GetRequiredInformantNpcLevel(node))
                return false;

            if (unlockedNodeIds.Contains(nodeId))
                return false;

            if (node.prerequisite != null && !unlockedNodeIds.Contains(node.prerequisite.nodeId))
                return false;

            foreach (string unlockedNodeId in unlockedNodeIds)
            {
                SkillTreeNodeSO unlockedNode = SkillTreeRegistry.Find(characterCode, unlockedNodeId);
                if (unlockedNode == null) continue;

                bool sameBranchChoice =
                    unlockedNode.skillIndex == node.skillIndex &&
                    unlockedNode.level == node.level;

                if (sameBranchChoice)
                    return false;
            }

            return true;
        }

        public string SkillTreeCharacterCode => !string.IsNullOrEmpty(heroCode) ? heroCode : characterName;

        public static int GetRequiredInformantNpcLevel(SkillTreeNodeSO node)
        {
            if (node == null) return int.MaxValue;
            return Mathf.Max(1, node.level - 1);
        }

        private void RecordInformantSkillNode(SkillTreeNodeSO node)
        {
            string recordId = node != null ? node.nodeId : null;
            if (string.IsNullOrEmpty(recordId)) return;

            foreach (var skill in mySkills)
                if (skill.skillName == recordId) return;

            mySkills.Add(new PlayerSkill
            {
                skillName = recordId,
                currentLevel = node != null ? node.level : 1
            });
        }

        [Server]
        public bool ApplyInformantUpgrade(string targetSkillName, int npcLevel)
        {
            for (int i = 0; i < mySkills.Count; i++)
            {
                if (mySkills[i].skillName == targetSkillName)
                {
                    PlayerSkill temp = mySkills[i];
                    if (temp.currentLevel >= npcLevel) return false;
                    temp.currentLevel++;
                    mySkills[i] = temp;
                    return true;
                }
            }
            return false;
        }
    }
}
