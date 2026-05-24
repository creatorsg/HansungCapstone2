using Jun;
using Mirror;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

namespace Lsy
{
    [System.Serializable]
    public enum ItemType
    {
        Equipment,
        Consumable
    }
    [System.Serializable]
    public struct InventoryItem
    {
        public string itemName;
        public ItemType Type;
        public ConsumableInfo ConsumInfo;
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

        /// <summary>FinalHeroPos — 초상화 슬롯 인덱스 (0~3). 설정되면 OnAnyUnitReady 발화.</summary>
        [SyncVar(hook = nameof(OnHeroPosChanged))]
        public int heroPos = -1;

        /// <summary>FinalHeroCode — 초상화 이미지 매핑용</summary>
        [SyncVar] public string heroCode = "";

        public readonly SyncList<InventoryItem> myInventory = new SyncList<InventoryItem>();

        [SyncVar(hook = nameof(OnSelectedWeaponIdChanged))]
        public string selectedWeaponId = "";

        [SyncVar(hook = nameof(OnPurchasedNodeCountChanged))]
        public int purchasedNodeCount = 0;

        // ─── UI 레이어용 이벤트 ───────────────────────────────────────────
        /// <summary>로컬 권한 획득 시 — 초기화용</summary>
        public static event Action<CharacterUnit> OnLocalUnitSpawned;
        /// <summary>heroPos가 설정된 유닛 — 초상화 UI 갱신용 (전체 클라이언트)</summary>
        public static event Action<CharacterUnit> OnAnyUnitReady;
        /// <summary>인벤토리 변경 시 — InventoryUI 갱신용</summary>
        public static event Action OnLocalInventoryChanged;
        /// <summary>강화/스킬 상태 변경 시 — BaseUpgradeUI 갱신용</summary>
        public static event Action OnLocalUpgradeStateChanged;
        /// <summary>HP 또는 San이 변경됐을 때 — 초상화 슬라이더 갱신용</summary>
        public static event Action<CharacterUnit> OnAnyUnitStatsChanged;

        private void OnCurrentHpChanged(float oldVal, float newVal) => OnAnyUnitStatsChanged?.Invoke(this);
        private void OnCurrentSanChanged(int oldVal, int newVal)    => OnAnyUnitStatsChanged?.Invoke(this);

        private void OnHeroPosChanged(int oldVal, int newVal)
        {
            if (newVal >= 0)
                OnAnyUnitReady?.Invoke(this);
        }

        /// <summary>
        /// 클라이언트에서 이 오브젝트가 완전히 초기화된 후 호출됩니다.
        /// 초기 스폰 시 SyncVar 훅이 발동하지 않는 Mirror 버전 대비용.
        /// heroPos가 이미 유효하면 여기서 명시적으로 OnAnyUnitReady를 발화합니다.
        /// </summary>
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
            currentHp  = maxHp;
            currentSan = maxSan;

            if (myInfo == null) myInfo = new PlayerInfo();
            myInfo.Hp = maxHp;
            myInfo.San = maxSan;

            Debug.Log($"<color=green>[캐릭터] 초기화 완료 (CharacterData): {characterName} (HP:{maxHp})</color>");
        }

        /// <summary>
        /// CharacterSelect → PlayerData 경로로 넘어온 데이터로 초기화합니다.
        /// 골드는 PlayerAccount에서 관리하므로 여기서 설정하지 않습니다.
        /// </summary>
        [Server]
        public void SetupFromPlayerData(PlayerData pd)
        {
            characterName = string.IsNullOrEmpty(pd.Info.Name) ? pd.FinalHeroCode : pd.Info.Name;
            heroCode      = pd.FinalHeroCode;
            heroPos       = pd.FinalHeroPos; // hook → OnAnyUnitReady 발화
            maxHp         = pd.Info.Hp;
            maxSan        = pd.Info.San;
            currentHp     = maxHp;
            currentSan    = maxSan;

            // ── 아이템 목록 주입 ───
            myInventory.Clear();
            if (pd.Info.Items != null)
            {
                foreach (var consumInfo in pd.Info.Items)
                {
                    myInventory.Add(new InventoryItem
                    {
                        itemName = consumInfo.Name,
                        Type = ItemType.Consumable,
                        ConsumInfo = consumInfo,
                        amount = 1   // 캐릭터 기본 지급 수량
                    });
                    Debug.Log("아이템 연동");
                }
            }
            if (myInfo == null) myInfo = new PlayerInfo();
            myInfo.Hp  = maxHp;
            myInfo.San = maxSan;

            Debug.Log($"<color=green>[캐릭터] 초기화 완료 (PlayerData): {characterName} / code={pd.FinalHeroCode} (HP:{maxHp})</color>");
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

            unlockedNodeIds.OnChange -= OnUnlockedNodeIdsChanged;
            unlockedNodeIds.OnChange += OnUnlockedNodeIdsChanged;

            OnLocalUnitSpawned?.Invoke(this);
        }

        public override void OnStopAuthority()
        {
            base.OnStopAuthority();
            myInventory.Callback -= OnInventoryChanged;
            mySkills.Callback -= OnSkillsChanged;
            unlockedNodeIds.OnChange -= OnUnlockedNodeIdsChanged;
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
            currentHp  = maxHp;
            currentSan = maxSan;
            if (myInfo == null) myInfo = new PlayerInfo();
            myInfo.Hp  = maxHp;
            myInfo.San = maxSan;
            return true;
        }

        /// <summary>
        /// 골드 체크/차감은 PlayerAccount.CmdBlacksmithUpgrade에서 처리합니다.
        /// 이 메서드는 무기 강화 상태만 변경합니다.
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
        /// 골드 체크/차감은 PlayerAccount.CmdSkillPurchase에서 처리합니다.
        /// 이 메서드는 스킬 추가 상태만 변경합니다.
        /// </summary>
        [Server]
        public bool ApplySkillPurchase(string skillId, int npcLevel, int requiredNpcLevel)
        {
            if (npcLevel < requiredNpcLevel) return false;

            foreach (var skill in mySkills)
                if (skill.skillName == skillId) return false;

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
