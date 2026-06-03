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

            //[정] Jun 투 UI가 병합 ?ItemInfo처럼 Name/icon/null 체크용환 로티 산 공니
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

 /// <summary>FinalHeroPos 초상롯 덱(0~3). 정면 OnAnyUnitReady 발화.</summary>
        [SyncVar(hook = nameof(OnHeroPosChanged))]
        public int heroPos = -1;

 /// <summary>FinalHeroCode 초상지 매핑/summary>
        [SyncVar] public string heroCode = "";

        public readonly SyncList<InventoryItem> myInventory = new SyncList<InventoryItem>();

        [SyncVar(hook = nameof(OnSelectedWeaponIdChanged))]
        public string selectedWeaponId = "";

        /// <summary>고유 특성 현재 강화 단계. 0 = 미강화, 1~3 = 강화 단계.</summary>
        [SyncVar(hook = nameof(OnUniqueTraitLevelChanged))]
        public int uniqueTraitLevel = 0;

        [SyncVar(hook = nameof(OnPurchasedNodeCountChanged))]
        public int purchasedNodeCount = 0;

        public readonly SyncDictionary<string, int> blacksmithWeaponLevels = new SyncDictionary<string, int>();

            //UI 이용 벤
 /// <summary>로컬 권한 득 초기용</summary>
        public static event Action<CharacterUnit> OnLocalUnitSpawned;
 /// <summary>heroPos가 정닛 초상UI 갱신(체 라언</summary>
        public static event Action<CharacterUnit> OnAnyUnitReady;
 /// <summary>벤리 변InventoryUI 갱신/summary>
        public static event Action OnLocalInventoryChanged;
 /// <summary>강화/킬 태 변BaseUpgradeUI 갱신/summary>
        public static event Action OnLocalUpgradeStateChanged;
 /// <summary>HP 는 San변경됐초상라더 갱신/summary>
        public static event Action<CharacterUnit> OnAnyUnitStatsChanged;

        private void OnCurrentHpChanged(float oldVal, float newVal) => OnAnyUnitStatsChanged?.Invoke(this);
        private void OnCurrentSanChanged(int oldVal, int newVal)    => OnAnyUnitStatsChanged?.Invoke(this);


            //착 롯 관컴포트
        public EquipmentSlot equipmentSlot;
        private void Awake()
        {
            equipmentSlot = GetComponent<EquipmentSlot>();
        }

        [Server]
        private void ResetBlacksmithUpgradeState()
        {
            //[정] 같 CharacterUnit사할 전 캐릭의 장무기 강화 태가 어가지 도초기합다.
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
 /// 라언에브트가 전초기된 출니
 /// 초기 폰 ?SyncVar 이 발동 는 Mirror 버전 비용.
 /// heroPos가 효면 기명시으OnAnyUnitReady발화니
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

 Debug.Log($"<color=green>[캐릭 초기료 (CharacterData): {characterName} (HP:{maxHp})</color>");
        }

        /// <summary>
 /// CharacterSelect ?PlayerData 경로어이로 초기합다.
 /// 골드PlayerAccount서 관리하므기정 습다.
        /// </summary>
        [Server]
        public void SetupFromPlayerData(PlayerData pd)
        {
            characterName = string.IsNullOrEmpty(pd.Info.Name) ? pd.FinalHeroCode : pd.Info.Name;
            heroCode      = pd.FinalHeroCode;
            heroPos       = pd.FinalHeroPos; // hook OnAnyUnitReady 발화
            // MaxHp/MaxSan 사용 (Hp/San은 현재값이므로 사망 시 0일 수 있음)
            maxHp         = pd.Info.MaxHp  > 0f ? pd.Info.MaxHp  : pd.Info.Hp;
            maxSan        = pd.Info.MaxSan > 0  ? pd.Info.MaxSan : pd.Info.San;
            currentHp     = pd.Info.Hp;   // 사망이면 0 유지
            currentSan    = pd.Info.San;
            ResetBlacksmithUpgradeState();

            //미장이주입 (Items = 벤 체) ?
            myInventory.Clear();
            if (pd.Info.Items != null)
            {
                foreach (var invItem in pd.Info.Items)
                    myInventory.Add(invItem);
                Debug.Log($"[CharacterUnit] Inventory synced: {pd.Info.Items.Count}");
            }

            //기본 착 처리 ?
            if (equipmentSlot != null)
            {
            //무기 동 착
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

                // �Ҹ�ǰ �ڵ� ����
                equipmentSlot.equippedConsumables.Clear();
                if (pd.Info.Expendables != null)
                {
                    foreach (var consumInfo in pd.Info.Expendables)
                    {
                        if (consumInfo == null || consumInfo.amount <= 0) continue;
                        var consumItem = new InventoryItem
                        {
                            itemName = consumInfo.Name,
                            Type = ItemType.Consumable,
                            ConsumInfo = consumInfo,
                            amount = consumInfo.amount
                        };
                        equipmentSlot.EquipConsumable(consumItem);
                        Debug.Log($"[ĳ����] �Ҹ�ǰ �ڵ� ����: {consumInfo.Name} x{consumInfo.amount}");
                    }
                }
            }
            if (myInfo == null) myInfo = new PlayerInfo();
            myInfo.Hp  = maxHp;
            myInfo.San = maxSan;

            // ?�?� ?�비 ?�동 ?�착 ?�료 ???�탯 ?�계???�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�
            // 초기 ?�착 ?�태�?기�??�로 pd.Info??Hp/Atk/Def ?�을 즉시 갱신?�니??
            ServerSyncToPlayerData(pd);

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

            // 장비 슬롯(무기ID/방어구ID/소모품) 변경 → UI 갱신 연결
            EquipmentSlot.OnEquipmentChanged -= OnEquipSlotChanged;
            EquipmentSlot.OnEquipmentChanged += OnEquipSlotChanged;

            OnLocalUnitSpawned?.Invoke(this);
        }

        public override void OnStopAuthority()
        {
            base.OnStopAuthority();
            myInventory.Callback -= OnInventoryChanged;
            mySkills.Callback -= OnSkillsChanged;
            unlockedNodeIds.OnChange -= OnUnlockedNodeIdsChanged;
            blacksmithWeaponLevels.OnChange -= OnBlacksmithWeaponLevelsChanged;
            EquipmentSlot.OnEquipmentChanged -= OnEquipSlotChanged;
        }

        private void OnEquipSlotChanged()
        {
            if (!isOwned) return;
            OnLocalInventoryChanged?.Invoke();
        }

        private void OnSelectedWeaponIdChanged(string oldVal, string newVal)
        {
            if (!isOwned) return;
            RefreshUpgradeUI();
        }

        private void OnUniqueTraitLevelChanged(int oldVal, int newVal)
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
            //중지래 AddItemWithInfo교체 ?
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

            //기존 AddItem ?ConsumInfo까 같이 어주는 용 수
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
            //ConsumInfo가 함전이객체Add
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
 /// 골드 체크/차감 PlayerAccount.CmdBlacksmithUpgrade서 처리니
 /// 메서는 무기 강화 태변경합다.
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
            if (nodeIndex > 0 && currentWeaponLevel < nodeIndex) return false;
            if (currentWeaponLevel > nodeIndex) return false;

            blacksmithWeaponLevels[weaponId] = nodeIndex + 1;
            selectedWeaponId = weaponId;
            purchasedNodeCount = nodeIndex + 1;

            return true;
        }

        /// <summary>
 /// 골드 체크/차감 PlayerAccount.CmdSkillPurchase서 처리니
 /// 메서는 킬 추 태변경합다.
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
        public bool ApplySkillUpgrade(int skillIndex, int targetLevel, string skillId)
        {
            if (skillIndex < 0) return false;
            if (targetLevel < 2) return false;

            for (int i = 0; i < mySkills.Count; i++)
            {
                if (mySkills[i].skillIndex != skillIndex) continue;

                PlayerSkill temp = mySkills[i];
                int currentLevel = Mathf.Max(1, temp.currentLevel);
                if (currentLevel != targetLevel - 1) return false;

                temp.skillIndex = skillIndex;
                temp.skillName = skillId;
                temp.currentLevel = targetLevel;
                mySkills[i] = temp;
                ServerSyncToPlayerData();   // 강화 즉시 PlayerData 클론에 스킬 반영
                return true;
            }

            if (targetLevel != 2) return false;

            mySkills.Add(new PlayerSkill
            {
                skillIndex = skillIndex,
                skillName = skillId,
                currentLevel = targetLevel
            });
            ServerSyncToPlayerData();   // 강화 즉시 PlayerData 클론에 스킬 반영
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

 //벤리서 이착

        [Command]
        public void CmdEquipConsumableToSlot(string itemName)
        {
            if (equipmentSlot == null) return;

            //벤리서 착려이찾기
            for (int i = 0; i < myInventory.Count; i++)
            {
                if (myInventory[i].itemName == itemName)
                {
                    InventoryItem itemInInv = myInventory[i];
                    bool success = false;
                    // [수정] 장비 교체 시 새 장비를 먼저 차감한 뒤 기존 장비를 반환합니다.
                    // 기존 장비를 먼저 반환하면 같은 인벤토리 인덱스가 덮어써질 수 있습니다.
                    InventoryItem? equipmentToReturn = null;

                    switch (itemInInv.Type) {
                        case ItemType.Consumable:
            //착 롯겨이이(1개씩 착)
                            InventoryItem equipData = itemInInv;
                            equipData.amount = 1;
                            success = equipmentSlot.EquipConsumable(equipData);
                            break;

                        case ItemType.Weapon:
                            // [수정] 기존 구조에 맞춰 장착 중인 장비 이름으로 동일 장비 재장착을 차단합니다.
                            if (equipmentSlot.equippedWeaponId == itemInInv.itemName)
                                return;

                            InventoryItem oldWeaponItem = equipmentSlot.equippedWeapon;
                            bool hadWeapon = !string.IsNullOrEmpty(equipmentSlot.equippedWeaponId);
                            // [수정] 장비 슬롯에는 인벤토리 스택 전체가 아니라 1개만 저장합니다.
                            InventoryItem weaponToEquip = itemInInv;
                            weaponToEquip.amount = 1;
                            if (equipmentSlot.EquipWeapon(weaponToEquip))  // string InventoryItem
                            {
                                success = true;
                                if (hadWeapon)
                                    equipmentToReturn = oldWeaponItem;
                            }
                            break;

                        case ItemType.Armor:
                            // [수정] 기존 구조에 맞춰 장착 중인 장비 이름으로 동일 장비 재장착을 차단합니다.
                            if (equipmentSlot.equippedArmorId == itemInInv.itemName)
                                return;

                            InventoryItem oldArmorItem = equipmentSlot.equippedArmor;
                            bool hadArmor = !string.IsNullOrEmpty(equipmentSlot.equippedArmorId);
                            // [수정] 장비 슬롯에는 인벤토리 스택 전체가 아니라 1개만 저장합니다.
                            InventoryItem armorToEquip = itemInInv;
                            armorToEquip.amount = 1;
                            if (equipmentSlot.EquipArmor(armorToEquip))  // string InventoryItem
                            {
                                success = true;
                                if (hadArmor)
                                    equipmentToReturn = oldArmorItem;
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

                        // [수정] 새 장비 차감이 끝난 뒤 서로 다른 기존 장비를 인벤토리로 반환합니다.
                        if (equipmentToReturn.HasValue)
                            AddItemWithInfo(equipmentToReturn.Value);

 Debug.Log($"<color=green>[착 공] {itemName} (벤리 량: {itemInInv.amount})</color>");
                        ServerSyncToPlayerData();   // PlayerData즉시 반영
                    }
                    return;
                }
            }
        }

        [Command]
        public void CmdUnequipConsumableFromSlot(string itemName)
        {
            if (equipmentSlot == null) return;

            //착 롯서 당 이을 먼 찾아이 복사둡다.
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
                ServerSyncToPlayerData();   // PlayerData즉시 반영
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
                ServerSyncToPlayerData();   // PlayerData즉시 반영
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
                ServerSyncToPlayerData();   // PlayerData즉시 반영
            }
        }

        [Command]
        public void CmdSyncEquipmentToPlayerData()
        {
            ServerSyncToPlayerData();
        }

        /// <summary>
 /// 버 용. myInventory + equipmentSlot 태PlayerData.Info니
 /// 착/제 Cmd 서 직접 출니
        ///
 /// 탯 계름
 /// 기본 탯 (CharacterDatabase) + 재 착 무기 + 재 착 방어+ 고유 성
 /// 최종 탯Info기록니
 /// 렇야 Home에비바 마배 진입 탯확집다.
        /// </summary>
        [Server]
        private void ServerSyncToPlayerData(PlayerData providedPd = null)
        {
            if (equipmentSlot == null)
            {
                Debug.LogError($"[{characterName}] equipmentSlot null");
                return;
            }

            //connectionToClient + FinalHeroCode 치는 PlayerData 찾기
            PlayerData pd = providedPd;
            if (pd == null)
            {
                foreach (var player in FindObjectsByType<PlayerData>(FindObjectsSortMode.None))
                {
                    if (player.connectionToClient == connectionToClient &&
                        player.FinalHeroCode == heroCode)
                    {
                        pd = player;
                        break;
                    }
                }
            }

            if (pd == null)
            {
 Debug.LogError($"[{characterName}] PlayerData찾 못했니 " +
                               $"conn={connectionToClient}, heroCode={heroCode}");
                return;
            }

            var info = PlayerAccount.ClonePlayerInfo(pd.Info);
            if (info == null) info = new Jun.PlayerInfo();
            int savedGold = info.Gold;

            // ── 비율 계산용: step 1이 info를 덮어쓰기 전에 이전 max/current 캡처 ──
            // pd.Info가 권위 있는 값 — SyncVar(this.maxSan/currentSan)보다 신뢰도 높음
            float prevMaxHp  = info.MaxHp  > 0 ? info.MaxHp  : (info.Hp  > 0 ? info.Hp  : 1f);
            int   prevMaxSan = info.MaxSan > 0 ? info.MaxSan : (info.San > 0 ? info.San : 1);
            float prevCurHp  = info.Hp;    // pd.Info.Hp  = 이전 currentHp
            int   prevCurSan = info.San;   // pd.Info.San = 이전 currentSan

            //1. 기본 탯 설(CharacterDatabase 기) ?
            //비 탯적 도기마기본값으초기합다.
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
            //Ctm CharacterDatabase드가 으므기존 ?
            }
            info.Gold = savedGold;
            //2. 재 착 비 탯 산 ?
            EqpInfo curWeapon = equipmentSlot.equippedWeapon.EquipInfo;
            EqpInfo curArmor  = equipmentSlot.equippedArmor.EquipInfo;
            AddEqpStats(ref info, curWeapon);
            AddEqpStats(ref info, curArmor);

            // ── 3. 고유 특성 스탯 합산 (uniqueTraitLevel SyncVar 기준, 0이면 미적용) ─
            info.UniqueTraitLv = uniqueTraitLevel; // pd.Info와 SyncVar 동기화
            if (CharacterRegistry.TryGet(heroCode, out var regEntry) && regEntry.UniqueTrait != null
                && uniqueTraitLevel >= 1)
            {
                TraitLevelData d = regEntry.UniqueTrait.GetLevel(
                    Mathf.Clamp(uniqueTraitLevel, 1, UniqueTraitSO.MaxLevel));
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

            //4. CharacterUnit의 maxHp/maxSan 갱신 + currentHp/currentSan 비율 스케일
            // 비율은 이미 캡처한 pd.Info 기반 값으로 계산 (SyncVar 타이밍 문제 회피)
            float hpRatio  = prevMaxHp  > 0 ? Mathf.Clamp01(prevCurHp  / prevMaxHp)            : 1f;
            float sanRatio = prevMaxSan > 0 ? Mathf.Clamp01(prevCurSan / (float)prevMaxSan)     : 1f;

            maxHp  = info.Hp;
            maxSan = info.San;
            info.MaxHp  = info.Hp;
            info.MaxSan = info.San;

            // 사망(hpRatio=0)이면 0 유지, 살아있으면 비율 그대로
            currentHp  = maxHp  > 0 ? Mathf.Floor(maxHp  * hpRatio)             : 0f;
            currentSan = maxSan > 0 ? Mathf.FloorToInt(maxSan * sanRatio)        : 0;
            info.Hp  = currentHp;
            info.San = currentSan;

            Debug.Log($"[SyncToPlayerData] {characterName} — " +
                      $"prevMax(Hp:{prevMaxHp}/San:{prevMaxSan}) " +
                      $"prevCur(Hp:{prevCurHp}/San:{prevCurSan}) " +
                      $"ratio(Hp:{hpRatio:F2}/San:{sanRatio:F2}) " +
                      $"→ newMax(Hp:{maxHp}/San:{maxSan}) " +
                      $"newCur(Hp:{currentHp}/San:{currentSan})");

            //5. 착 모Expendables ?
            info.Expendables = new List<ConsumableInfo>();
            foreach (var item in equipmentSlot.equippedConsumables)
            {
                if (item.ConsumInfo == null || item.amount <= 0) continue;

            //̹ Ʈ ̸ Ҹǰ ִ Ȯ
                ConsumableInfo existingItem = info.Expendables.Find(x => x.Name == item.ConsumInfo.Name);

                if (existingItem != null)
                {
            //̹ Ѵٸ (amount) 
                    existingItem.amount += item.amount;
                }
                else
                {
            //ʴ´ٸ 纻  ߰ϰ, 
                    ConsumableInfo newItem = item.ConsumInfo.Clone();
                    newItem.amount = item.amount; //  Կ ִ  Ȯ
                    info.Expendables.Add(newItem);
                }
            }

            //6. 미장벤 ?Items ?
            info.Items = new List<InventoryItem>(myInventory);

            //7. 착 비 참조 ?
            info.Weapon = curWeapon;
            info.Armor  = curArmor;

            // [정보상 일원화 브릿지] 강화 스킬을 base에서 매번 재구성해 PlayerData(DontDestroyOnLoad 클론)에 즉시 반영.
            // 항상 base에서 다시 빌드 → idempotent(여러 번 호출해도 누적 X). null이면 기존 Skills 유지.
            var upgradedSkills = PlayerAccount.BuildUpgradedSkills(heroCode, mySkills);
            if (upgradedSkills != null) info.Skills = upgradedSkills;

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
            info.MaxHp += eqp.Hp;
            info.San   += eqp.San;
            info.MaxSan += eqp.San;
            info.Atk   += eqp.Atk;
            info.Def   += eqp.Def;
            info.Spd   += eqp.Spd;
            info.Crit  += eqp.Crit;
            info.Ctm   += eqp.Ctm;
            info.Dodge += eqp.Dodge;
            info.Acc   += eqp.Acc;
            info.Res   += eqp.Res;
        }

        /// <summary>
        /// 고유 특성을 targetLevel로 강화합니다.
        /// 반드시 현재 레벨 + 1 단계만 강화 가능합니다.
        /// </summary>
        [Server]
        public bool ApplyUniqueTraitUpgrade(int targetLevel)
        {
            if (targetLevel != uniqueTraitLevel + 1)
            {
                Debug.LogWarning($"[CharacterUnit] 고유 특성 강화 거부: 현재={uniqueTraitLevel}, 요청={targetLevel}");
                return false;
            }
            if (targetLevel > UniqueTraitSO.MaxLevel)
            {
                Debug.LogWarning($"[CharacterUnit] 고유 특성 최대 단계 초과: {targetLevel}");
                return false;
            }

            uniqueTraitLevel = targetLevel; // SyncVar → 클라이언트 hook 발화 → UI 갱신
            ServerSyncToPlayerData();       // 스탯 재계산 + pd.Info.UniqueTraitLv 갱신
            Debug.Log($"[CharacterUnit] {characterName} 고유 특성 {targetLevel}단계 강화 완료");
            return true;
        }
    }
}
