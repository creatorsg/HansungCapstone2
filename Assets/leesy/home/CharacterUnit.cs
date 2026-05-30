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

        // [?˜ì •] Jun ?„íˆ¬ UIê°€ ë³‘í•© ??ItemInfoì²˜ëŸ¼ Name/icon/null ì²´í¬ë¥??¬ìš©?˜ë?ë¡??¸í™˜ ?„ë¡œ?¼í‹°?€ ?°ì‚°?ë? ?œê³µ?©ë‹ˆ??
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

        /// <summary>FinalHeroPos ??ì´ˆìƒ???¬ë¡¯ ?¸ë±??(0~3). ?¤ì •?˜ë©´ OnAnyUnitReady ë°œí™”.</summary>
        [SyncVar(hook = nameof(OnHeroPosChanged))]
        public int heroPos = -1;

        /// <summary>FinalHeroCode ??ì´ˆìƒ???´ë?ì§€ ë§¤í•‘??/summary>
        [SyncVar] public string heroCode = "";

        public readonly SyncList<InventoryItem> myInventory = new SyncList<InventoryItem>();

        [SyncVar(hook = nameof(OnSelectedWeaponIdChanged))]
        public string selectedWeaponId = "";

        [SyncVar(hook = nameof(OnPurchasedNodeCountChanged))]
        public int purchasedNodeCount = 0;

        // [¼öÁ¤] ´ëÀåÀåÀÌ ¹«±â °­È­ ´Ü°è¸¦ ¹«±âº°·Î ÀúÀåÇÕ´Ï´Ù. key=weaponId, value=±¸¸ÅÇÑ ³ëµå ¼ö
        public readonly SyncDictionary<string, int> blacksmithWeaponLevels = new SyncDictionary<string, int>();

        // ?€?€?€ UI ?ˆì´?´ìš© ?´ë²¤???€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€
        /// <summary>ë¡œì»¬ ê¶Œí•œ ?ë“ ????ì´ˆê¸°?”ìš©</summary>
        public static event Action<CharacterUnit> OnLocalUnitSpawned;
        /// <summary>heroPosê°€ ?¤ì •??? ë‹› ??ì´ˆìƒ??UI ê°±ì‹ ??(?„ì²´ ?´ë¼?´ì–¸??</summary>
        public static event Action<CharacterUnit> OnAnyUnitReady;
        /// <summary>?¸ë²¤? ë¦¬ ë³€ê²?????InventoryUI ê°±ì‹ ??/summary>
        public static event Action OnLocalInventoryChanged;
        /// <summary>ê°•í™”/?¤í‚¬ ?íƒœ ë³€ê²?????BaseUpgradeUI ê°±ì‹ ??/summary>
        public static event Action OnLocalUpgradeStateChanged;
        /// <summary>HP ?ëŠ” San??ë³€ê²½ë??????ì´ˆìƒ???¬ë¼?´ë” ê°±ì‹ ??/summary>
        public static event Action<CharacterUnit> OnAnyUnitStatsChanged;

        private void OnCurrentHpChanged(float oldVal, float newVal) => OnAnyUnitStatsChanged?.Invoke(this);
        private void OnCurrentSanChanged(int oldVal, int newVal)    => OnAnyUnitStatsChanged?.Invoke(this);


        // ?¥ì°© ?¬ë¡¯ ê´€ë¦?ì»´í¬?ŒíŠ¸
        public EquipmentSlot equipmentSlot;
        private void Awake()
        {
            equipmentSlot = GetComponent<EquipmentSlot>();
        }

        [Server]
        private void ResetBlacksmithUpgradeState()
        {
            // [?˜ì •] ê°™ì? CharacterUnit???¬ì‚¬?©í•  ???´ì „ ìºë¦­?°ì˜ ?€?¥ì¥??ë¬´ê¸° ê°•í™” ?íƒœê°€ ?˜ì–´ê°€ì§€ ?Šë„ë¡?ì´ˆê¸°?”í•©?ˆë‹¤.
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
        /// ?´ë¼?´ì–¸?¸ì—?????¤ë¸Œ?íŠ¸ê°€ ?„ì „??ì´ˆê¸°?”ëœ ???¸ì¶œ?©ë‹ˆ??
        /// ì´ˆê¸° ?¤í° ??SyncVar ?…ì´ ë°œë™?˜ì? ?ŠëŠ” Mirror ë²„ì „ ?€ë¹„ìš©.
        /// heroPosê°€ ?´ë? ? íš¨?˜ë©´ ?¬ê¸°??ëª…ì‹œ?ìœ¼ë¡?OnAnyUnitReadyë¥?ë°œí™”?©ë‹ˆ??
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

            Debug.Log($"<color=green>[ìºë¦­?? ì´ˆê¸°???„ë£Œ (CharacterData): {characterName} (HP:{maxHp})</color>");
        }

        /// <summary>
        /// CharacterSelect ??PlayerData ê²½ë¡œë¡??˜ì–´???°ì´?°ë¡œ ì´ˆê¸°?”í•©?ˆë‹¤.
        /// ê³¨ë“œ??PlayerAccount?ì„œ ê´€ë¦¬í•˜ë¯€ë¡??¬ê¸°???¤ì •?˜ì? ?ŠìŠµ?ˆë‹¤.
        /// </summary>
        [Server]
        public void SetupFromPlayerData(PlayerData pd)
        {
            characterName = string.IsNullOrEmpty(pd.Info.Name) ? pd.FinalHeroCode : pd.Info.Name;
            heroCode      = pd.FinalHeroCode;
            heroPos       = pd.FinalHeroPos; // hook ??OnAnyUnitReady ë°œí™”
            maxHp         = pd.Info.Hp;
            maxSan        = pd.Info.San;
            currentHp     = maxHp;
            currentSan    = maxSan;
            ResetBlacksmithUpgradeState();

            // ?€?€ ë¯¸ì¥ì°??„ì´??ì£¼ì… (Items = ?¸ë²¤ ?„ì²´) ?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€
            myInventory.Clear();
            if (pd.Info.Items != null)
            {
                foreach (var invItem in pd.Info.Items)
                    myInventory.Add(invItem);
                Debug.Log($"[CharacterUnit] Inventory synced: {pd.Info.Items.Count}");
            }

            // ?€?€ ê¸°ë³¸ ?¥ì°© ì²˜ë¦¬ ?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€
            if (equipmentSlot != null)
            {
                // ë¬´ê¸° ?ë™ ?¥ì°©
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
                    Debug.Log($"[ìºë¦­?? ë¬´ê¸° ?ë™ ?¥ì°©: {pd.Info.Weapon.Name}");
                }

                // ë°©ì–´êµ??ë™ ?¥ì°©
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
                    Debug.Log($"[ìºë¦­?? ë°©ì–´êµ??ë™ ?¥ì°©: {pd.Info.Armor.Name}");
                }

                // ¼Ò¸ğÇ° ÀÚµ¿ º¹¿ø
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
                        Debug.Log($"[Ä³¸¯ÅÍ] ¼Ò¸ğÇ° ÀÚµ¿ º¹¿ø: {consumInfo.Name} x{consumInfo.amount}");
                    }
                }
            }
            if (myInfo == null) myInfo = new PlayerInfo();
            myInfo.Hp  = maxHp;
            myInfo.San = maxSan;

            // ?€?€ ?¥ë¹„ ?ë™ ?¥ì°© ?„ë£Œ ???¤íƒ¯ ?¬ê³„???€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€
            // ì´ˆê¸° ?¥ì°© ?íƒœë¥?ê¸°ì??¼ë¡œ pd.Info??Hp/Atk/Def ?±ì„ ì¦‰ì‹œ ê°±ì‹ ?©ë‹ˆ??
            ServerSyncToPlayerData(pd);

            Debug.Log($"<color=green>[ìºë¦­?? ì´ˆê¸°???„ë£Œ (PlayerData): {characterName} / code={pd.FinalHeroCode} (HP:{maxHp})</color>");
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
        // ?˜ì¤‘??ì§€??ê²??„ë˜ AddItemWithInfoë¡?êµì²´ ??ê²?
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

        // ê¸°ì¡´ AddItem ?€??ConsumInfoê¹Œì? ê°™ì´ ?£ì–´ì£¼ëŠ” ?„ìš© ?¨ìˆ˜
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
            // ConsumInfoê°€ ?¬í•¨???¨ì „???„ì´??ê°ì²´ë¥?Add
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
        /// ê³¨ë“œ ì²´í¬/ì°¨ê°?€ PlayerAccount.CmdBlacksmithUpgrade?ì„œ ì²˜ë¦¬?©ë‹ˆ??
        /// ??ë©”ì„œ?œëŠ” ë¬´ê¸° ê°•í™” ?íƒœë§?ë³€ê²½í•©?ˆë‹¤.
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
            // [¼öÁ¤] ¹«±âº° °­È­ ´Ü°è¸¦ ±âÁØÀ¸·Î ÀÌÀü ´Ü°è ±¸¸Å ¿©ºÎ¿Í Áßº¹ ±¸¸Å¸¦ °Ë»çÇÕ´Ï´Ù.
            if (nodeIndex > 0 && currentWeaponLevel < nodeIndex) return false;
            if (currentWeaponLevel > nodeIndex) return false;

            blacksmithWeaponLevels[weaponId] = nodeIndex + 1;
            selectedWeaponId = weaponId;
            purchasedNodeCount = nodeIndex + 1;

            return true;
        }

        /// <summary>
        /// ê³¨ë“œ ì²´í¬/ì°¨ê°?€ PlayerAccount.CmdSkillPurchase?ì„œ ì²˜ë¦¬?©ë‹ˆ??
        /// ??ë©”ì„œ?œëŠ” ?¤í‚¬ ì¶”ê? ?íƒœë§?ë³€ê²½í•©?ˆë‹¤.
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

        //?€?€?€?€?¸ë²¤? ë¦¬?ì„œ ?„ì´???¥ì°©?€?€?€?€?€?€?€?€?€?€

        [Command]
        public void CmdEquipConsumableToSlot(string itemName)
        {
            if (equipmentSlot == null) return;

            // ?¸ë²¤? ë¦¬?ì„œ ?¥ì°©?˜ë ¤???„ì´??ì°¾ê¸°
            for (int i = 0; i < myInventory.Count; i++)
            {
                if (myInventory[i].itemName == itemName)
                {
                    InventoryItem itemInInv = myInventory[i];
                    bool success = false;

                    switch (itemInInv.Type) {
                        case ItemType.Consumable:
                            // ?¥ì°© ?¬ë¡¯???˜ê²¨ì¤??„ì´???°ì´??(1ê°œì”© ?¥ì°©)
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

                        Debug.Log($"<color=green>[?¥ì°© ?±ê³µ] {itemName} (?¸ë²¤? ë¦¬ ?¨ì? ?˜ëŸ‰: {itemInInv.amount})</color>");
                        ServerSyncToPlayerData();   // ??PlayerData??ì¦‰ì‹œ ë°˜ì˜
                    }
                    return;
                }
            }
        }

        [Command]
        public void CmdUnequipConsumableFromSlot(string itemName)
        {
            if (equipmentSlot == null) return;

            // ?¥ì°© ?¬ë¡¯?ì„œ ?´ë‹¹ ?„ì´?œì„ ë¨¼ì? ì°¾ì•„???°ì´?°ë? ë³µì‚¬?´ë‘¡?ˆë‹¤.
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
                ServerSyncToPlayerData();   // ??PlayerData??ì¦‰ì‹œ ë°˜ì˜
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
                ServerSyncToPlayerData();   // ??PlayerData??ì¦‰ì‹œ ë°˜ì˜
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
                ServerSyncToPlayerData();   // ??PlayerData??ì¦‰ì‹œ ë°˜ì˜
            }
        }

        [Command]
        public void CmdSyncEquipmentToPlayerData()
        {
            ServerSyncToPlayerData();
        }

        /// <summary>
        /// ?œë²„ ?„ìš©. myInventory + equipmentSlot ?íƒœë¥?PlayerData.Info???ë‹ˆ??
        /// ?¥ì°©/?´ì œ Cmd ?´ë??ì„œ ì§ì ‘ ?¸ì¶œ?©ë‹ˆ??
        ///
        /// ???¤íƒ¯ ?¬ê³„???ë¦„
        ///   ê¸°ë³¸ ?¤íƒ¯ (CharacterDatabase) + ?„ì¬ ?¥ì°© ë¬´ê¸° + ?„ì¬ ?¥ì°© ë°©ì–´êµ?+ ê³ ìœ  ?¹ì„±
        ///   ??ìµœì¢… ?¤íƒ¯??Info??ê¸°ë¡?©ë‹ˆ??
        ///   ?´ë ‡ê²??´ì•¼ Home?¬ì—???¥ë¹„ë¥?ë°”ê? ?Œë§ˆ??ë°°í? ì§„ì… ?¤íƒ¯???•í™•?´ì§‘?ˆë‹¤.
        /// </summary>
        [Server]
        private void ServerSyncToPlayerData(PlayerData providedPd = null)
        {
            if (equipmentSlot == null)
            {
                Debug.LogError($"[{characterName}] equipmentSlot null");
                return;
            }

            // connectionToClient + FinalHeroCode ?????¼ì¹˜?˜ëŠ” PlayerData ì°¾ê¸°
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
                Debug.LogError($"[{characterName}] PlayerDataë¥?ì°¾ì? ëª»í–ˆ?µë‹ˆ?? " +
                               $"conn={connectionToClient}, heroCode={heroCode}");
                return;
            }

            var info = pd.Info;
            int savedGold = info.Gold; // ¸ÕÀú º¸Á¸
            // ?€?€ 1. ê¸°ë³¸ ?¤íƒ¯ ?¬ì„¤??(CharacterDatabase ê¸°ì?) ?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€
            // ?¥ë¹„ ?¤íƒ¯???„ì ?˜ì? ?Šë„ë¡?ë§??™ê¸°?”ë§ˆ??ê¸°ë³¸ê°’ìœ¼ë¡?ì´ˆê¸°?”í•©?ˆë‹¤.
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
                // Ctm?€ CharacterDatabase???„ë“œê°€ ?†ìœ¼ë¯€ë¡?ê¸°ì¡´ ê°?? ì?
            }
            info.Gold = savedGold;
            // ?€?€ 2. ?„ì¬ ?¥ì°© ?¥ë¹„ ?¤íƒ¯ ?©ì‚° ?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€
            EqpInfo curWeapon = equipmentSlot.equippedWeapon.EquipInfo;
            EqpInfo curArmor  = equipmentSlot.equippedArmor.EquipInfo;
            AddEqpStats(ref info, curWeapon);
            AddEqpStats(ref info, curArmor);

            // ?€?€ 3. ê³ ìœ  ?¹ì„± ?¤íƒ¯ ?©ì‚° (UniqueTraitLv ê¸°ì?, 0?´ë©´ ë¯¸ì ?? ?€?€
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

            // ?€?€ 4. CharacterUnit??maxHp/maxSan??ê°±ì‹  (Home??HPë°?ë°˜ì˜) ?€?€
            maxHp  = info.Hp;
            maxSan = info.San;

            // ?€?€ 5. ?¥ì°© ?Œëª¨????Expendables ?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€
            info.Expendables = new List<ConsumableInfo>();
            foreach (var item in equipmentSlot.equippedConsumables)
            {
                if (item.ConsumInfo == null || item.amount <= 0) continue;

                // ÀÌ¹Ì ¸®½ºÆ®¿¡ °°Àº ÀÌ¸§ÀÇ ¼Ò¸ğÇ°ÀÌ µé¾îÀÖ´ÂÁö È®ÀÎ
                ConsumableInfo existingItem = info.Expendables.Find(x => x.Name == item.ConsumInfo.Name);

                if (existingItem != null)
                {
                    // ÀÌ¹Ì Á¸ÀçÇÑ´Ù¸é °³¼ö(amount)¸¸ ´õÇØÁÜ
                    existingItem.amount += item.amount;
                }
                else
                {
                    // Á¸ÀçÇÏÁö ¾Ê´Â´Ù¸é º¹»çº»À» ¸¸µé¾î¼­ Ãß°¡ÇÏ°í, °³¼ö¸¦ ¼¼ÆÃÇÔ
                    ConsumableInfo newItem = item.ConsumInfo.Clone();
                    newItem.amount = item.amount; // ÀåÂø ½½·Ô¿¡ ÀÖ´ø °³¼ö·Î È®Á¤
                    info.Expendables.Add(newItem);
                }
            }

            // ?€?€ 6. ë¯¸ì¥ì°??¸ë²¤ ??Items ?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€
            info.Items = new List<InventoryItem>(myInventory);

            // ?€?€ 7. ?¥ì°© ?¥ë¹„ ì°¸ì¡° ?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€
            info.Weapon = curWeapon;
            info.Armor  = curArmor;

            pd.Info = info;

            Debug.Log($"[CharacterUnit] {characterName} ??PlayerData ?™ê¸°???„ë£Œ " +
                      $"HP:{info.Hp} ATK:{info.Atk} DEF:{info.Def} " +
                      $"(Weapon={info.Weapon?.Name ?? "?†ìŒ"} Armor={info.Armor?.Name ?? "?†ìŒ"})");
        }

        /// <summary>EqpInfo ?¤íƒ¯??PlayerInfo???”í•©?ˆë‹¤. null?´ë©´ ?„ë¬´ê²ƒë„ ?˜ì? ?ŠìŠµ?ˆë‹¤.</summary>
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
