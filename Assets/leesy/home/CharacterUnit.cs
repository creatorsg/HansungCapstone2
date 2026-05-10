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

        public readonly SyncList<PlayerSkill> mySkills = new SyncList<PlayerSkill>();

        [SyncVar] public string characterName;

        [SyncVar(hook = nameof(OnCurrentGoldChanged))]
        public int currentGold;

        public readonly SyncList<InventoryItem> myInventory = new SyncList<InventoryItem>();

        [SyncVar(hook = nameof(OnSelectedWeaponIdChanged))]
        public string selectedWeaponId = "";

        [SyncVar(hook = nameof(OnPurchasedNodeCountChanged))]
        public int purchasedNodeCount = 0;

        public event Action<int> OnGoldChanged;

        [Server]
        public void SetupFromData(CharacterData data)
        {
            characterName = data.charName;
            currentGold = data.gold;
            maxHp = data.maxHp;
            maxSan = data.maxSan;

            if (myInfo == null) myInfo = new PlayerInfo();
            myInfo.Hp = maxHp;
            myInfo.San = maxSan;

            Debug.Log($"<color=green>[서버] 캐릭터 세팅 완료: {characterName} (HP:{maxHp}, 골드:{currentGold}G)</color>");
        }

        public override void OnStartAuthority()
        {
            base.OnStartAuthority();

            if (PlayerAccount.LocalInstance != null)
                PlayerAccount.LocalInstance.currentSelectedCharacter = this;

            myInventory.Callback -= OnInventoryChanged;
            myInventory.Callback += OnInventoryChanged;

            mySkills.Callback -= OnSkillsChanged;
            mySkills.Callback += OnSkillsChanged;

            if (GoldUI.Instance != null)
                GoldUI.Instance.SetTrackedUnit(this);
        }

        public override void OnStopAuthority()
        {
            base.OnStopAuthority();
            myInventory.Callback -= OnInventoryChanged;
            mySkills.Callback -= OnSkillsChanged;
        }

        private void OnCurrentGoldChanged(int oldVal, int newVal)
        {
            if (!isOwned) return;
            OnGoldChanged?.Invoke(newVal);
        }

        private void OnSelectedWeaponIdChanged(string oldVal, string newVal)
        {
            if (!isOwned) return;
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

        private void RefreshUpgradeUI()
        {
            var uis = FindObjectsByType<BaseUpgradeUI>(FindObjectsSortMode.None);
            foreach (var ui in uis)
            {
                if (ui.gameObject.activeInHierarchy)
                    ui.RefreshAllRows();
            }
        }

        private void OnInventoryChanged(SyncList<InventoryItem>.Operation op, int itemIndex, InventoryItem oldItem, InventoryItem newItem)
        {
            if (PlayerAccount.LocalInstance == null) return;
            if (PlayerAccount.LocalInstance.currentSelectedCharacter != this) return;
            if (InventoryUI.Instance == null) return;

            if (isOwned && InventoryUI.Instance.gameObject.activeInHierarchy)
                InventoryUI.Instance.RefreshInventory();
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
            if (myInfo.Hp >= maxHp && myInfo.San >= maxSan) return false;
            myInfo.Hp = maxHp;
            myInfo.San = maxSan;
            return true;
        }

        [Server]
        public bool ApplyBlacksmithUpgrade(string weaponId, int nodeIndex, int npcLevel, int price)
        {
            if (currentGold < price) return false;
            if (npcLevel < nodeIndex + 1) return false;
            if (selectedWeaponId != "" && selectedWeaponId != weaponId) return false;
            if (nodeIndex > 0 && purchasedNodeCount < nodeIndex) return false;
            if (purchasedNodeCount > nodeIndex) return false;

            currentGold -= price;
            selectedWeaponId = weaponId;
            purchasedNodeCount = nodeIndex + 1;

            return true;
        }

        [Server]
        public bool ApplySkillPurchase(string skillId, int npcLevel, int price, int requiredNpcLevel)
        {
            if (currentGold < price) return false;
            if (npcLevel < requiredNpcLevel) return false;

            foreach (var skill in mySkills)
                if (skill.skillName == skillId) return false;

            currentGold -= price;
            mySkills.Add(new PlayerSkill { skillName = skillId, currentLevel = 1 });

            return true;
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