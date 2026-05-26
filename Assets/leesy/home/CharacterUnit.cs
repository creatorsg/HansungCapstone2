using Jun;
using Mirror;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using TMPro.Examples;
using UnityEngine;
using static UnityEditor.Progress;

namespace Lsy
{
    [System.Serializable]
    public enum ItemType
    {
        Weapon,
        Armor,
        Consumable
    }
    [System.Serializable]
    public struct InventoryItem
    {
        public string itemName;
        public ItemType Type;
        public ConsumableInfo ConsumInfo;
        public EqpInfo EquipInfo;
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


        // 장착 슬롯 관리 컴포넌트
        public EquipmentSlot equipmentSlot;
        private void Awake()
        {
            equipmentSlot = GetComponent<EquipmentSlot>();
        }
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

            // ── 미장착 아이템 주입 (Items = 인벤 전체) ───────────────────
            myInventory.Clear();
            if (pd.Info.Items != null)
            {
                foreach (var invItem in pd.Info.Items)
                    myInventory.Add(invItem);
                Debug.Log($"[캐릭터] 미장착 아이템 연동 완료: {pd.Info.Items.Count}개");
            }

            // ── 기본 장착 처리 ────────────────────────────────────────────
            if (equipmentSlot != null)
            {
                // 무기 자동 장착
                if (pd.Info.Weapon != null && !string.IsNullOrEmpty(pd.Info.Weapon.Name))
                {
                    var weaponItem = new InventoryItem
                    {
                        itemName  = pd.Info.Weapon.Name,
                        Type      = ItemType.Weapon,
                        EquipInfo = pd.Info.Weapon,
                        amount    = 1
                    };
                    equipmentSlot.EquipWeapon(weaponItem);
                    Debug.Log($"[캐릭터] 무기 자동 장착: {pd.Info.Weapon.Name}");
                }

                // 방어구 자동 장착
                if (pd.Info.Armor != null && !string.IsNullOrEmpty(pd.Info.Armor.Name))
                {
                    var armorItem = new InventoryItem
                    {
                        itemName  = pd.Info.Armor.Name,
                        Type      = ItemType.Armor,
                        EquipInfo = pd.Info.Armor,
                        amount    = 1
                    };
                    equipmentSlot.EquipArmor(armorItem);
                    Debug.Log($"[캐릭터] 방어구 자동 장착: {pd.Info.Armor.Name}");
                }

                // 기본 소모품 자동 장착 (Expendables)
                if (pd.Info.Expendables != null)
                {
                    foreach (var consumInfo in pd.Info.Expendables)
                    {
                        if (consumInfo == null) continue;
                        var consumItem = new InventoryItem
                        {
                            itemName  = consumInfo.Name,
                            Type      = ItemType.Consumable,
                            ConsumInfo = consumInfo,
                            amount    = 1
                        };
                        equipmentSlot.EquipConsumable(consumItem);
                    }
                    Debug.Log($"[캐릭터] 소모품 자동 장착: {pd.Info.Expendables.Count}개");
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
        // 나중에 지울 것 아래 AddItemWithInfo로 교체 할 것
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

        // 기존 AddItem 대신 ConsumInfo까지 같이 넣어주는 전용 함수
        [Server]
        public void AddItemWithInfo(InventoryItem item)
        {
            for (int i = 0; i < myInventory.Count; i++)
            {
                if (myInventory[i].itemName == item.itemName)
                {
                    InventoryItem temp = myInventory[i];
                    temp.amount += 1;
                    myInventory[i] = temp;
                    return;
                }
            }
            // ConsumInfo가 포함된 온전한 아이템 객체를 Add
            myInventory.Add(item);
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

        //────인벤토리에서 아이템 장착──────────

        [Command]
        public void CmdEquipConsumableToSlot(string itemName)
        {
            if (equipmentSlot == null) return;

            // 인벤토리에서 장착하려는 아이템 찾기
            for (int i = 0; i < myInventory.Count; i++)
            {
                if (myInventory[i].itemName == itemName)
                {
                    InventoryItem itemInInv = myInventory[i];
                    bool success = false;

                    switch (itemInInv.Type) {
                        case ItemType.Consumable:
                            // 장착 슬롯에 넘겨줄 아이템 데이터 (1개씩 장착)
                            InventoryItem equipData = itemInInv;
                            equipData.amount = 1;
                            success = equipmentSlot.EquipConsumable(equipData);
                            break;

                        case ItemType.Weapon:
                            string oldWeapon = equipmentSlot.equippedWeaponId;
                            InventoryItem oldWeaponItem = equipmentSlot.equippedWeapon;
                            if (equipmentSlot.EquipWeapon(itemInInv))  // string → InventoryItem
                            {
                                success = true;
                                if (!string.IsNullOrEmpty(oldWeapon))
                                    AddItemWithInfo(oldWeaponItem);
                            }
                            break;

                        case ItemType.Armor:
                            string oldArmor = equipmentSlot.equippedArmorId;
                            InventoryItem oldArmorItem = equipmentSlot.equippedArmor;
                            if (equipmentSlot.EquipArmor(itemInInv))  // string → InventoryItem
                            {
                                success = true;
                                if (!string.IsNullOrEmpty(oldArmor))
                                    AddItemWithInfo(oldArmorItem);
                            }
                            break;
                    }
                    if (success)
                    {
                        // EquipmentSlot에 장착 시도 (최대 6개 슬롯 검사 포함)
                        // 장착 성공 시, 인벤토리에서 1개 차감
                        itemInInv.amount -= 1;

                        if (itemInInv.amount <= 0)
                            myInventory.RemoveAt(i);
                        else
                            myInventory[i] = itemInInv; // SyncList 갱신

                        Debug.Log($"<color=green>[장착 성공] {itemName} (인벤토리 남은 수량: {itemInInv.amount})</color>");

                    }
                    return;
                }
            }
        }

        [Command]
        public void CmdUnequipConsumableFromSlot(string itemName)
        {
            if (equipmentSlot == null) return;

            // 장착 슬롯에서 해당 아이템을 먼저 찾아서 데이터를 복사해둡니다.
            InventoryItem? itemToReturn = null;
            foreach (var item in equipmentSlot.equippedConsumables)
            {
                if (item.itemName == itemName)
                {
                    itemToReturn = item;
                    
                    break;
                }
            }

            // 슬롯에서 실제 해제 (데이터가 삭제되기 전에 위에서 미리 복사해둠)
            if (equipmentSlot.UnequipConsumable(itemName, 1))
            {
                // 해제 성공 시, 복사해둔 데이터(ConsumInfo 포함)를 인벤토리에 추가
                if (itemToReturn.HasValue)
                {
                    InventoryItem returnItem = itemToReturn.Value;
                    returnItem.amount = 1; 

                    AddItemWithInfo(returnItem);
                }
            }
        }
        [Command]
        public void CmdUnequipWeapon()
        {
            if (equipmentSlot == null) return;
            InventoryItem returnItem = equipmentSlot.equippedWeapon;
            if (equipmentSlot.UnequipWeapon())
            {
                returnItem.amount = 1;
                AddItemWithInfo(returnItem);
            }
        }

        [Command]
        public void CmdUnequipArmor()
        {
            if (equipmentSlot == null) return;
            InventoryItem returnItem = equipmentSlot.equippedArmor;
            if (equipmentSlot.UnequipArmor())
            {
                returnItem.amount = 1;
                AddItemWithInfo(returnItem);
            }
        }

        [Command]
        public void CmdSyncEquipmentToPlayerData()
        {
            if (equipmentSlot == null)
            {
                Debug.LogError($"[{characterName}] equipmentSlot null");
                return;
            }
            //CharacterUnit myChar = PlayerAccount.LocalInstance.currentSelectedCharacter;
            // heroPos로 내 PlayerData 찾기
            PlayerData pd = null;
            foreach (var player in FindObjectsByType<PlayerData>(FindObjectsSortMode.None))
            {
                if (player.FinalHeroPos == heroPos)
                {
                    pd = player;
                    break;
                }
            }

            if (pd == null)
            {
                Debug.LogError($"[{characterName}] PlayerData를 찾지 못했습니다. heroPos:{heroPos}");
                return;
            }

            var info = pd.Info;

            // 장착 소모품 → Expendables
            info.Expendables = new List<ConsumableInfo>();
            foreach (var item in equipmentSlot.equippedConsumables)
            {
                if (item.ConsumInfo != null)
                {
                    for (int i = 0; i < item.amount; i++)
                        info.Expendables.Add(item.ConsumInfo);
                }
            }
            // 미장착 인벤 → Items
            info.Items = new List<InventoryItem>(myInventory);

            // 무기/방어구
            info.Weapon = equipmentSlot.equippedWeapon.EquipInfo;
            info.Armor = equipmentSlot.equippedArmor.EquipInfo;

            pd.Info = info;
            Debug.Log($"[CharacterUnit] {characterName} 장착 정보 PlayerData 동기화 완료");
        }
    }
}
