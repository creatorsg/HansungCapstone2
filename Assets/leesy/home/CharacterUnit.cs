using Jun;
using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine;

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

        // [?�정] Jun ?�투 UI가 병합 ??ItemInfo처럼 Name/icon/null 체크�??�용?��?�??�환 ?�로?�티?� ?�산?��? ?�공?�니??
        public string Name => !string.IsNullOrEmpty(itemName) ? itemName : ConsumInfo?.Name ?? EquipInfo?.Name ?? "";
        public Sprite icon => ConsumInfo != null && ConsumInfo.icon != null ? ConsumInfo.icon : EquipInfo?.icon;
        public static bool operator ==(InventoryItem item, object other) => other == null && string.IsNullOrEmpty(item.Name);
        public static bool operator !=(InventoryItem item, object other) => !(item == other);
        public override bool Equals(object obj) => obj is InventoryItem other && Name == other.Name && Type == other.Type && amount == other.amount;
        public override int GetHashCode() => HashCode.Combine(Name, Type, amount);
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

        /// <summary>FinalHeroPos ??초상???�롯 ?�덱??(0~3). ?�정?�면 OnAnyUnitReady 발화.</summary>
        [SyncVar(hook = nameof(OnHeroPosChanged))]
        public int heroPos = -1;

        /// <summary>FinalHeroCode ??초상???��?지 매핑??/summary>
        [SyncVar] public string heroCode = "";

        public readonly SyncList<InventoryItem> myInventory = new SyncList<InventoryItem>();

        [SyncVar(hook = nameof(OnSelectedWeaponIdChanged))]
        public string selectedWeaponId = "";

        [SyncVar(hook = nameof(OnPurchasedNodeCountChanged))]
        public int purchasedNodeCount = 0;

        // [����] �������� ���� ��ȭ �ܰ踦 ���⺰�� �����մϴ�. key=weaponId, value=������ ��� ��
        public readonly SyncDictionary<string, int> blacksmithWeaponLevels = new SyncDictionary<string, int>();

        // ?�?�?� UI ?�이?�용 ?�벤???�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�
        /// <summary>로컬 권한 ?�득 ????초기?�용</summary>
        public static event Action<CharacterUnit> OnLocalUnitSpawned;
        /// <summary>heroPos가 ?�정???�닛 ??초상??UI 갱신??(?�체 ?�라?�언??</summary>
        public static event Action<CharacterUnit> OnAnyUnitReady;
        /// <summary>?�벤?�리 변�?????InventoryUI 갱신??/summary>
        public static event Action OnLocalInventoryChanged;
        /// <summary>강화/?�킬 ?�태 변�?????BaseUpgradeUI 갱신??/summary>
        public static event Action OnLocalUpgradeStateChanged;
        /// <summary>HP ?�는 San??변경됐??????초상???�라?�더 갱신??/summary>
        public static event Action<CharacterUnit> OnAnyUnitStatsChanged;

        private void OnCurrentHpChanged(float oldVal, float newVal) => OnAnyUnitStatsChanged?.Invoke(this);
        private void OnCurrentSanChanged(int oldVal, int newVal)    => OnAnyUnitStatsChanged?.Invoke(this);


        // ?�착 ?�롯 관�?컴포?�트
        public EquipmentSlot equipmentSlot;
        private void Awake()
        {
            equipmentSlot = GetComponent<EquipmentSlot>();
        }

        [Server]
        private void ResetBlacksmithUpgradeState()
        {
            // [?�정] 같�? CharacterUnit???�사?�할 ???�전 캐릭?�의 ?�?�장??무기 강화 ?�태가 ?�어가지 ?�도�?초기?�합?�다.
            selectedWeaponId = "";
            purchasedNodeCount = 0;
            blacksmithWeaponLevels.Clear();
        }

        private void OnHeroPosChanged(int oldVal, int newVal)
        {
            if (newVal >= 0)
                OnAnyUnitReady?.Invoke(this);
        }

        /// <summary>
        /// ?�라?�언?�에?????�브?�트가 ?�전??초기?�된 ???�출?�니??
        /// 초기 ?�폰 ??SyncVar ?�이 발동?��? ?�는 Mirror 버전 ?�비용.
        /// heroPos가 ?��? ?�효?�면 ?�기??명시?�으�?OnAnyUnitReady�?발화?�니??
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
            ResetBlacksmithUpgradeState();

            if (myInfo == null) myInfo = new PlayerInfo();
            myInfo.Hp = maxHp;
            myInfo.San = maxSan;

            Debug.Log($"<color=green>[캐릭?? 초기???�료 (CharacterData): {characterName} (HP:{maxHp})</color>");
        }

        /// <summary>
        /// CharacterSelect ??PlayerData 경로�??�어???�이?�로 초기?�합?�다.
        /// 골드??PlayerAccount?�서 관리하므�??�기???�정?��? ?�습?�다.
        /// </summary>
        [Server]
        public void SetupFromPlayerData(PlayerData pd)
        {
            characterName = string.IsNullOrEmpty(pd.Info.Name) ? pd.FinalHeroCode : pd.Info.Name;
            heroCode      = pd.FinalHeroCode;
            heroPos       = pd.FinalHeroPos; // hook ??OnAnyUnitReady 발화
            maxHp         = pd.Info.Hp;
            maxSan        = pd.Info.San;
            currentHp     = maxHp;
            currentSan    = maxSan;
            ResetBlacksmithUpgradeState();

            // ?�?� 미장�??�이??주입 (Items = ?�벤 ?�체) ?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�
            myInventory.Clear();
            if (pd.Info.Items != null)
            {
                foreach (var invItem in pd.Info.Items)
                    myInventory.Add(invItem);
                Debug.Log($"[CharacterUnit] Inventory synced: {pd.Info.Items.Count}");
            }

            // ?�?� 기본 ?�착 처리 ?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�
            if (equipmentSlot != null)
            {
                // 무기 ?�동 ?�착
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
                    Debug.Log($"[캐릭?? 무기 ?�동 ?�착: {pd.Info.Weapon.Name}");
                }

                // 방어�??�동 ?�착
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
                    Debug.Log($"[캐릭?? 방어�??�동 ?�착: {pd.Info.Armor.Name}");
                }

                // [?�정] Inspector 기본 ?�모?��? ???�착 ?�롯???�동 ?�착?��? ?�습?�다.
                // ?�투???�모?��? Inventory?�서 ?�착??equipmentSlot.equippedConsumables�??�달?�니??
            }
            if (myInfo == null) myInfo = new PlayerInfo();
            myInfo.Hp  = maxHp;
            myInfo.San = maxSan;

            // ?�?� ?�비 ?�동 ?�착 ?�료 ???�탯 ?�계???�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�
            // 초기 ?�착 ?�태�?기�??�로 pd.Info??Hp/Atk/Def ?�을 즉시 갱신?�니??
            ServerSyncToPlayerData();

            Debug.Log($"<color=green>[캐릭?? 초기???�료 (PlayerData): {characterName} / code={pd.FinalHeroCode} (HP:{maxHp})</color>");
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

            blacksmithWeaponLevels.OnChange -= OnBlacksmithWeaponLevelsChanged;
            blacksmithWeaponLevels.OnChange += OnBlacksmithWeaponLevelsChanged;

            OnLocalUnitSpawned?.Invoke(this);
        }

        public override void OnStopAuthority()
        {
            base.OnStopAuthority();
            myInventory.Callback -= OnInventoryChanged;
            mySkills.Callback -= OnSkillsChanged;
            unlockedNodeIds.OnChange -= OnUnlockedNodeIdsChanged;
            blacksmithWeaponLevels.OnChange -= OnBlacksmithWeaponLevelsChanged;
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

        private void OnBlacksmithWeaponLevelsChanged(SyncDictionary<string, int>.Operation op, string key, int value)
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
        // ?�중??지??�??�래 AddItemWithInfo�?교체 ??�?
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

        // 기존 AddItem ?�??ConsumInfo까�? 같이 ?�어주는 ?�용 ?�수
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
            // ConsumInfo가 ?�함???�전???�이??객체�?Add
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
        /// 골드 체크/차감?� PlayerAccount.CmdBlacksmithUpgrade?�서 처리?�니??
        /// ??메서?�는 무기 강화 ?�태�?변경합?�다.
        /// </summary>
        public int GetBlacksmithWeaponLevel(string weaponId)
        {
            if (string.IsNullOrEmpty(weaponId)) return 0;
            return blacksmithWeaponLevels.TryGetValue(weaponId, out int level) ? level : 0;
        }

        [Server]
        public bool ApplyBlacksmithUpgrade(string weaponId, int nodeIndex, int npcLevel)
        {
            if (npcLevel < nodeIndex + 1) return false;

            int currentWeaponLevel = GetBlacksmithWeaponLevel(weaponId);
            // [����] ���⺰ ��ȭ �ܰ踦 �������� ���� �ܰ� ���� ���ο� �ߺ� ���Ÿ� �˻��մϴ�.
            if (nodeIndex > 0 && currentWeaponLevel < nodeIndex) return false;
            if (currentWeaponLevel > nodeIndex) return false;

            blacksmithWeaponLevels[weaponId] = nodeIndex + 1;
            selectedWeaponId = weaponId;
            purchasedNodeCount = nodeIndex + 1;

            return true;
        }

        /// <summary>
        /// 골드 체크/차감?� PlayerAccount.CmdSkillPurchase?�서 처리?�니??
        /// ??메서?�는 ?�킬 추�? ?�태�?변경합?�다.
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

        /// <summary>
        /// [정보상 일원화] skillIndex 기준 선형 레벨업. mySkills에 없으면 현재 Lv1로 간주.
        /// 선행 조건: 현재 레벨 == targetLevel-1. 성공 시 currentLevel을 targetLevel(최대 4)로 올린다.
        /// 골드 체크/차감은 호출부(CharacterShop.CmdUpgradeSkillWithLevel)에서 처리한다.
        /// </summary>
        [Server]
        public bool ApplySkillUpgrade(int skillIndex, int targetLevel, string skillName = null)
        {
            if (skillIndex < 0) return false;
            if (targetLevel < 2 || targetLevel > 4) return false; // Lv1은 기본값(구매 대상 아님)

            // 현재 레벨 조회 (없으면 Lv1)
            int curLevel = 1;
            int foundAt = -1;
            for (int i = 0; i < mySkills.Count; i++)
            {
                if (mySkills[i].skillIndex == skillIndex)
                {
                    curLevel = mySkills[i].currentLevel;
                    foundAt = i;
                    break;
                }
            }

            // 선행: 바로 직전 레벨에서만 구매 가능 (더블클릭/레벨 점프 방어)
            if (curLevel != targetLevel - 1) return false;

            if (foundAt >= 0)
            {
                PlayerSkill temp = mySkills[foundAt];
                temp.currentLevel = targetLevel;
                if (!string.IsNullOrEmpty(skillName)) temp.skillName = skillName;
                mySkills[foundAt] = temp;
            }
            else
            {
                // 첫 강화(Lv1→Lv2): 항목 신규 추가
                mySkills.Add(new PlayerSkill
                {
                    skillIndex   = skillIndex,
                    skillName    = skillName ?? $"skill{skillIndex}",
                    currentLevel = targetLevel
                });
            }

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

        //?�?�?�?�?�벤?�리?�서 ?�이???�착?�?�?�?�?�?�?�?�?�?�

        [Command]
        public void CmdEquipConsumableToSlot(string itemName)
        {
            if (equipmentSlot == null) return;

            // ?�벤?�리?�서 ?�착?�려???�이??찾기
            for (int i = 0; i < myInventory.Count; i++)
            {
                if (myInventory[i].itemName == itemName)
                {
                    InventoryItem itemInInv = myInventory[i];
                    bool success = false;

                    switch (itemInInv.Type) {
                        case ItemType.Consumable:
                            // ?�착 ?�롯???�겨�??�이???�이??(1개씩 ?�착)
                            InventoryItem equipData = itemInInv;
                            equipData.amount = 1;
                            success = equipmentSlot.EquipConsumable(equipData);
                            break;

                        case ItemType.Weapon:
                            string oldWeapon = equipmentSlot.equippedWeaponId;
                            InventoryItem oldWeaponItem = equipmentSlot.equippedWeapon;
                            if (equipmentSlot.EquipWeapon(itemInInv))  // string ??InventoryItem
                            {
                                success = true;
                                if (!string.IsNullOrEmpty(oldWeapon))
                                    AddItemWithInfo(oldWeaponItem);
                            }
                            break;

                        case ItemType.Armor:
                            string oldArmor = equipmentSlot.equippedArmorId;
                            InventoryItem oldArmorItem = equipmentSlot.equippedArmor;
                            if (equipmentSlot.EquipArmor(itemInInv))  // string ??InventoryItem
                            {
                                success = true;
                                if (!string.IsNullOrEmpty(oldArmor))
                                    AddItemWithInfo(oldArmorItem);
                            }
                            break;
                    }
                    if (success)
                    {
                        itemInInv.amount -= 1;

                        if (itemInInv.amount <= 0)
                            myInventory.RemoveAt(i);
                        else
                            myInventory[i] = itemInInv;

                        Debug.Log($"<color=green>[?�착 ?�공] {itemName} (?�벤?�리 ?��? ?�량: {itemInInv.amount})</color>");
                        ServerSyncToPlayerData();   // ??PlayerData??즉시 반영
                    }
                    return;
                }
            }
        }

        [Command]
        public void CmdUnequipConsumableFromSlot(string itemName)
        {
            if (equipmentSlot == null) return;

            // ?�착 ?�롯?�서 ?�당 ?�이?�을 먼�? 찾아???�이?��? 복사?�둡?�다.
            InventoryItem? itemToReturn = null;
            foreach (var item in equipmentSlot.equippedConsumables)
            {
                if (item.itemName == itemName)
                {
                    itemToReturn = item;
                    
                    break;
                }
            }

            if (equipmentSlot.UnequipConsumable(itemName, 1))
            {
                if (itemToReturn.HasValue)
                {
                    InventoryItem returnItem = itemToReturn.Value;
                    returnItem.amount = 1;
                    AddItemWithInfo(returnItem);
                }
                ServerSyncToPlayerData();   // ??PlayerData??즉시 반영
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
                ServerSyncToPlayerData();   // ??PlayerData??즉시 반영
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
                ServerSyncToPlayerData();   // ??PlayerData??즉시 반영
            }
        }

        [Command]
        public void CmdSyncEquipmentToPlayerData()
        {
            ServerSyncToPlayerData();
        }

        /// <summary>
        /// ?�버 ?�용. myInventory + equipmentSlot ?�태�?PlayerData.Info???�니??
        /// ?�착/?�제 Cmd ?��??�서 직접 ?�출?�니??
        ///
        /// ???�탯 ?�계???�름
        ///   기본 ?�탯 (CharacterDatabase) + ?�재 ?�착 무기 + ?�재 ?�착 방어�?+ 고유 ?�성
        ///   ??최종 ?�탯??Info??기록?�니??
        ///   ?�렇�??�야 Home?�에???�비�?바�? ?�마??배�? 진입 ?�탯???�확?�집?�다.
        /// </summary>
        [Server]
        private void ServerSyncToPlayerData()
        {
            if (equipmentSlot == null)
            {
                Debug.LogError($"[{characterName}] equipmentSlot null");
                return;
            }

            // connectionToClient + FinalHeroCode ?????�치?�는 PlayerData 찾기
            PlayerData pd = null;
            foreach (var player in FindObjectsByType<PlayerData>(FindObjectsSortMode.None))
            {
                if (player.connectionToClient == connectionToClient &&
                    player.FinalHeroCode      == heroCode)
                {
                    pd = player;
                    break;
                }
            }

            if (pd == null)
            {
                Debug.LogError($"[{characterName}] PlayerData�?찾�? 못했?�니?? " +
                               $"conn={connectionToClient}, heroCode={heroCode}");
                return;
            }

            var info = pd.Info;
            int savedGold = info.Gold; // ���� ����
            // ?�?� 1. 기본 ?�탯 ?�설??(CharacterDatabase 기�?) ?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�
            // ?�비 ?�탯???�적?��? ?�도�?�??�기?�마??기본값으�?초기?�합?�다.
            if (CharacterDatabase.Stats.TryGetValue(heroCode, out var c))
            {
                info.Hp    = c.hp;
                info.Atk   = c.attack;
                info.Def   = c.defense;
                info.Acc   = c.accuracy;
                info.Dodge = c.evasion;
                info.Spd   = c.speed;
                info.Crit  = c.critical;
                info.San   = c.stress;
                info.Res   = c.effectResistance;
                //info.Gold  = c.gold;
                // Ctm?� CharacterDatabase???�드가 ?�으므�?기존 �??��?
            }
            info.Gold = savedGold;
            // ?�?� 2. ?�재 ?�착 ?�비 ?�탯 ?�산 ?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�
            EqpInfo curWeapon = equipmentSlot.equippedWeapon.EquipInfo;
            EqpInfo curArmor  = equipmentSlot.equippedArmor.EquipInfo;
            AddEqpStats(ref info, curWeapon);
            AddEqpStats(ref info, curArmor);

            // ?�?� 3. 고유 ?�성 ?�탯 ?�산 (UniqueTraitLv 기�?, 0?�면 미적?? ?�?�
            if (CharacterRegistry.TryGet(heroCode, out var regEntry) && regEntry.UniqueTrait != null
                && info.UniqueTraitLv >= 1)
            {
                TraitLevelData d = regEntry.UniqueTrait.GetLevel(
                    Mathf.Clamp(info.UniqueTraitLv, 1, UniqueTraitSO.MaxLevel));
                info.Hp    += d.hp;
                info.San   += d.san;
                info.Atk   += d.atk;
                info.Def   += d.def;
                info.Spd   += d.spd;
                info.Crit  += d.crit;
                info.Ctm   += d.ctm;
                info.Dodge += d.dodge;
                info.Acc   += d.acc;
                info.Res   += d.res;
            }

            // ?�?� 4. CharacterUnit??maxHp/maxSan??갱신 (Home??HP�?반영) ?�?�
            maxHp  = info.Hp;
            maxSan = info.San;

            // ?�?� 5. ?�착 ?�모????Expendables ?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�
            info.Expendables = new List<ConsumableInfo>();
            foreach (var item in equipmentSlot.equippedConsumables)
            {
                if (item.ConsumInfo == null || item.amount <= 0) continue;

                // �̹� ����Ʈ�� ���� �̸��� �Ҹ�ǰ�� ����ִ��� Ȯ��
                ConsumableInfo existingItem = info.Expendables.Find(x => x.Name == item.ConsumInfo.Name);

                if (existingItem != null)
                {
                    // �̹� �����Ѵٸ� ����(amount)�� ������
                    existingItem.amount += item.amount;
                }
                else
                {
                    // �������� �ʴ´ٸ� ���纻�� ���� �߰��ϰ�, ������ ������
                    ConsumableInfo newItem = item.ConsumInfo.Clone();
                    newItem.amount = item.amount; // ���� ���Կ� �ִ� ������ Ȯ��
                    info.Expendables.Add(newItem);
                }
            }

            // ?�?� 6. 미장�??�벤 ??Items ?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�
            info.Items = new List<InventoryItem>(myInventory);

            // ?�?� 7. ?�착 ?�비 참조 ?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�
            info.Weapon = curWeapon;
            info.Armor  = curArmor;

            pd.Info = info;

            Debug.Log($"[CharacterUnit] {characterName} ??PlayerData ?�기???�료 " +
                      $"HP:{info.Hp} ATK:{info.Atk} DEF:{info.Def} " +
                      $"(Weapon={info.Weapon?.Name ?? "?�음"} Armor={info.Armor?.Name ?? "?�음"})");
        }

        /// <summary>EqpInfo ?�탯??PlayerInfo???�합?�다. null?�면 ?�무것도 ?��? ?�습?�다.</summary>
        private static void AddEqpStats(ref Jun.PlayerInfo info, Jun.EqpInfo eqp)
        {
            if (eqp == null) return;
            info.Hp    += eqp.Hp;
            info.San   += eqp.San;
            info.Atk   += eqp.Atk;
            info.Def   += eqp.Def;
            info.Spd   += eqp.Spd;
            info.Crit  += eqp.Crit;
            info.Ctm   += eqp.Ctm;
            info.Dodge += eqp.Dodge;
            info.Acc   += eqp.Acc;
            info.Res   += eqp.Res;
        }
    }
}
