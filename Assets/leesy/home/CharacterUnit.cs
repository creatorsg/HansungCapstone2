using Mirror;
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
        [SyncVar] public int currentGold;

        public readonly SyncList<InventoryItem> myInventory = new SyncList<InventoryItem>();

        // ==========================================
        // 무기 업그레이드 상태
        // Mirror SyncVar hook은 제네릭 불가 - 타입별로 따로 선언
        // ==========================================
        [SyncVar(hook = nameof(OnSelectedWeaponIdChanged))]
        public string selectedWeaponId = "";

        [SyncVar(hook = nameof(OnPurchasedNodeCountChanged))]
        public int purchasedNodeCount = 0;

        // ==========================================
        // 1. 서버 세팅
        // ==========================================
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

            Debug.Log($"<color=green>[서버] 캐릭터 세팅 완료: {characterName} (HP: {maxHp}, 골드: {currentGold}G)</color>");
        }

        // ==========================================
        // 2. 권한 발급 완료
        // ==========================================
        public override void OnStartAuthority()
        {
            base.OnStartAuthority();

            if (PlayerAccount.LocalInstance != null)
                PlayerAccount.LocalInstance.currentSelectedCharacter = this;

            myInventory.Callback -= OnInventoryChanged;
            myInventory.Callback += OnInventoryChanged;

            mySkills.Callback -= OnSkillsChanged;
            mySkills.Callback += OnSkillsChanged;
        }

        public override void OnStopAuthority()
        {
            base.OnStopAuthority();
            myInventory.Callback -= OnInventoryChanged;
            mySkills.Callback -= OnSkillsChanged;
        }

        // ==========================================
        // 3. 무기 업그레이드 SyncVar 훅 (타입별로 분리)
        // ==========================================
        private void OnSelectedWeaponIdChanged(string oldVal, string newVal)
        {
            if (!isOwned) return;
            Debug.Log($"<color=yellow>[CharacterUnit] selectedWeaponId 변경: {oldVal} → {newVal}</color>");
            RefreshUpgradeUI();
        }

        private void OnPurchasedNodeCountChanged(int oldVal, int newVal)
        {
            if (!isOwned) return;
            Debug.Log($"<color=yellow>[CharacterUnit] purchasedNodeCount 변경: {oldVal} → {newVal}</color>");
            RefreshUpgradeUI();
        }

        // ==========================================
        // 4. 스킬 변경 콜백
        // ==========================================
        private void OnSkillsChanged(SyncList<PlayerSkill>.Operation op, int index, PlayerSkill oldItem, PlayerSkill newItem)
        {
            if (!isOwned) return;
            Debug.Log("<color=yellow>[CharacterUnit] 스킬 목록 변경 → UI 갱신</color>");
            RefreshUpgradeUI();
        }

        // ==========================================
        // 5. 활성화된 UpgradeUI 전부 갱신
        // ==========================================
        private void RefreshUpgradeUI()
        {
            foreach (var ui in FindObjectsByType<BaseUpgradeUI>(FindObjectsSortMode.None))
            {
                if (ui.gameObject.activeInHierarchy)
                    ui.RefreshAllRows();
            }
        }

        // ==========================================
        // 6. 인벤토리 변경 UI 콜백
        // ==========================================
        private void OnInventoryChanged(SyncList<InventoryItem>.Operation op, int itemIndex, InventoryItem oldItem, InventoryItem newItem)
        {
            if (PlayerAccount.LocalInstance == null) return;
            if (PlayerAccount.LocalInstance.currentSelectedCharacter != this) return;
            if (InventoryUI.Instance == null) return;

            if (isOwned && InventoryUI.Instance.gameObject.activeInHierarchy)
                InventoryUI.Instance.RefreshInventory();
        }

        // ==========================================
        // 7. 인벤토리 유틸
        // ==========================================
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

        // ==========================================
        // 8. 바텐더
        // ==========================================
        [Server]
        public bool ApplyBartenderHeal()
        {
            if (myInfo.Hp >= maxHp && myInfo.San >= maxSan) return false;
            myInfo.Hp = maxHp;
            myInfo.San = maxSan;
            return true;
        }

        // ==========================================
        // 9. 대장장이 무기 업그레이드 (선형)
        // ==========================================
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

        // ==========================================
        // 10. 정보상 스킬 구매 (독립 해금)
        // ==========================================
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

        // ==========================================
        // 11. 정보상 스킬 레벨업
        // ==========================================
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