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

        /// <summary>FinalHeroPos ??珥덉긽???щ’ ?몃뜳??(0~3). ?ㅼ젙?섎㈃ OnAnyUnitReady 諛쒗솕.</summary>
        [SyncVar(hook = nameof(OnHeroPosChanged))]
        public int heroPos = -1;

        /// <summary>FinalHeroCode ??珥덉긽???대?吏 留ㅽ븨??/summary>
        [SyncVar] public string heroCode = "";

        public readonly SyncList<InventoryItem> myInventory = new SyncList<InventoryItem>();

        [SyncVar(hook = nameof(OnSelectedWeaponIdChanged))]
        public string selectedWeaponId = "";

        [SyncVar(hook = nameof(OnPurchasedNodeCountChanged))]
        public int purchasedNodeCount = 0;

        // ??? UI ?덉씠?댁슜 ?대깽?????????????????????????????????????????????
        /// <summary>濡쒖뺄 沅뚰븳 ?띾뱷 ????珥덇린?붿슜</summary>
        public static event Action<CharacterUnit> OnLocalUnitSpawned;
        /// <summary>heroPos媛 ?ㅼ젙???좊떅 ??珥덉긽??UI 媛깆떊??(?꾩껜 ?대씪?댁뼵??</summary>
        public static event Action<CharacterUnit> OnAnyUnitReady;
        /// <summary>?몃깽?좊━ 蹂寃?????InventoryUI 媛깆떊??/summary>
        public static event Action OnLocalInventoryChanged;
        /// <summary>媛뺥솕/?ㅽ궗 ?곹깭 蹂寃?????BaseUpgradeUI 媛깆떊??/summary>
        public static event Action OnLocalUpgradeStateChanged;
        /// <summary>HP ?먮뒗 San??蹂寃쎈릱??????珥덉긽???щ씪?대뜑 媛깆떊??/summary>
        public static event Action<CharacterUnit> OnAnyUnitStatsChanged;

        private void OnCurrentHpChanged(float oldVal, float newVal) => OnAnyUnitStatsChanged?.Invoke(this);
        private void OnCurrentSanChanged(int oldVal, int newVal)    => OnAnyUnitStatsChanged?.Invoke(this);


        // ?μ갑 ?щ’ 愿由?而댄룷?뚰듃
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
        /// ?대씪?댁뼵?몄뿉?????ㅻ툕?앺듃媛 ?꾩쟾??珥덇린?붾맂 ???몄텧?⑸땲??
        /// 珥덇린 ?ㅽ룿 ??SyncVar ?낆씠 諛쒕룞?섏? ?딅뒗 Mirror 踰꾩쟾 ?鍮꾩슜.
        /// heroPos媛 ?대? ?좏슚?섎㈃ ?ш린??紐낆떆?곸쑝濡?OnAnyUnitReady瑜?諛쒗솕?⑸땲??
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

            Debug.Log($"<color=green>[罹먮┃?? 珥덇린???꾨즺 (CharacterData): {characterName} (HP:{maxHp})</color>");
        }

        /// <summary>
        /// CharacterSelect ??PlayerData 寃쎈줈濡??섏뼱???곗씠?곕줈 珥덇린?뷀빀?덈떎.
        /// 怨⑤뱶??PlayerAccount?먯꽌 愿由ы븯誘濡??ш린???ㅼ젙?섏? ?딆뒿?덈떎.
        /// </summary>
        [Server]
        public void SetupFromPlayerData(PlayerData pd)
        {
            characterName = string.IsNullOrEmpty(pd.Info.Name) ? pd.FinalHeroCode : pd.Info.Name;
            heroCode      = pd.FinalHeroCode;
            heroPos       = pd.FinalHeroPos; // hook ??OnAnyUnitReady 諛쒗솕
            maxHp         = pd.Info.Hp;
            maxSan        = pd.Info.San;
            currentHp     = maxHp;
            currentSan    = maxSan;

            // ?? ?꾩씠??紐⑸줉 二쇱엯(媛숈? 醫낅쪟???꾩씠?쒖씪 寃쎌슦 媛쒖닔瑜??뷀븯湲? ???
            myInventory.Clear();
            if (pd.Info.Items != null)
            {
                Dictionary<string, InventoryItem> tempDict = new Dictionary<string, InventoryItem>();
                foreach (var consumInfo in pd.Info.Items)
                {
                    if (tempDict.ContainsKey(consumInfo.Name))
                    {
                        var item = tempDict[consumInfo.Name];
                        item.amount += 1;
                        tempDict[consumInfo.Name] = item;
                    }
                    else
                    {
                        tempDict.Add(consumInfo.Name, new InventoryItem
                        {
                            itemName = consumInfo.Name,
                            Type = ItemType.Consumable,
                            ConsumInfo = consumInfo,
                            amount = 1
                        });
                    }
                }
                foreach (var kvp in tempDict)
                {
                    myInventory.Add(kvp.Value);
                }
                Debug.Log($"[罹먮┃?? ?꾩씠???곕룞 ?꾨즺 (醫낅쪟: {tempDict.Count})");
            }
            // ?? ?λ퉬 紐⑸줉 二쇱엯 ???
            if (pd.Info.Weapon != null)
            {
                myInventory.Add(new InventoryItem
                {
                    itemName = pd.Info.Weapon.Name,
                    Type = ItemType.Weapon,
                    EquipInfo = pd.Info.Weapon,
                    amount = 1
                });
                Debug.Log($"[罹먮┃?? 臾닿린 ?곕룞 ?꾨즺: {pd.Info.Weapon.Name}");
            }

            if (pd.Info.Armor != null)
            {
                myInventory.Add(new InventoryItem
                {
                    itemName = pd.Info.Armor.Name,
                    Type = ItemType.Armor,
                    EquipInfo = pd.Info.Armor,
                    amount = 1
                });
                Debug.Log($"[罹먮┃?? 諛⑹뼱援??곕룞 ?꾨즺: {pd.Info.Armor.Name}");
            }
            if (myInfo == null) myInfo = new PlayerInfo();
            myInfo.Hp  = maxHp;
            myInfo.San = maxSan;

            Debug.Log($"<color=green>[罹먮┃?? 珥덇린???꾨즺 (PlayerData): {characterName} / code={pd.FinalHeroCode} (HP:{maxHp})</color>");
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
        // ?섏쨷??吏??寃??꾨옒 AddItemWithInfo濡?援먯껜 ??寃?
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

        // 湲곗〈 AddItem ???ConsumInfo源뚯? 媛숈씠 ?ｌ뼱二쇰뒗 ?꾩슜 ?⑥닔
        [Server]
        public void AddItemWithInfo(InventoryItem item)
        {
            int addAmount = item.amount > 0 ? item.amount : 1;

            for (int i = 0; i < myInventory.Count; i++)
            {
                if (myInventory[i].itemName == item.itemName)
                {
                    InventoryItem temp = myInventory[i];
                    temp.amount += addAmount;
                    temp.Type = item.Type;

                    if (temp.ConsumInfo == null && item.ConsumInfo != null)
                        temp.ConsumInfo = item.ConsumInfo;

                    if (temp.EquipInfo == null && item.EquipInfo != null)
                        temp.EquipInfo = item.EquipInfo;

                    myInventory[i] = temp;
                    return;
                }
            }

            item.amount = addAmount;
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
        /// 怨⑤뱶 泥댄겕/李④컧? PlayerAccount.CmdBlacksmithUpgrade?먯꽌 泥섎━?⑸땲??
        /// ??硫붿꽌?쒕뒗 臾닿린 媛뺥솕 ?곹깭留?蹂寃쏀빀?덈떎.
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
        /// 怨⑤뱶 泥댄겕/李④컧? PlayerAccount.CmdSkillPurchase?먯꽌 泥섎━?⑸땲??
        /// ??硫붿꽌?쒕뒗 ?ㅽ궗 異붽? ?곹깭留?蹂寃쏀빀?덈떎.
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

        //?????몃깽?좊━?먯꽌 ?꾩씠???μ갑??????????

        [Command]
        public void CmdEquipConsumableToSlot(string itemName)
        {
            if (equipmentSlot == null) return;

            // ?몃깽?좊━?먯꽌 ?μ갑?섎젮???꾩씠??李얘린
            for (int i = 0; i < myInventory.Count; i++)
            {
                if (myInventory[i].itemName == itemName)
                {
                    InventoryItem itemInInv = myInventory[i];
                    bool success = false;

                    switch (itemInInv.Type) {
                        case ItemType.Consumable:
                            // ?μ갑 ?щ’???섍꺼以??꾩씠???곗씠??(1媛쒖뵫 ?μ갑)
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
                        // EquipmentSlot???μ갑 ?쒕룄 (理쒕? 6媛??щ’ 寃???ы븿)
                        // ?μ갑 ?깃났 ?? ?몃깽?좊━?먯꽌 1媛?李④컧
                        itemInInv.amount -= 1;

                        if (itemInInv.amount <= 0)
                            myInventory.RemoveAt(i);
                        else
                            myInventory[i] = itemInInv; // SyncList 媛깆떊

                        Debug.Log($"<color=green>[?μ갑 ?깃났] {itemName} (?몃깽?좊━ ?⑥? ?섎웾: {itemInInv.amount})</color>");

                    }
                    return;
                }
            }
        }

        [Command]
        public void CmdUnequipConsumableFromSlot(string itemName)
        {
            if (equipmentSlot == null) return;

            // ?μ갑 ?щ’?먯꽌 ?대떦 ?꾩씠?쒖쓣 癒쇱? 李얠븘???곗씠?곕? 蹂듭궗?대몼?덈떎.
            InventoryItem? itemToReturn = null;
            foreach (var item in equipmentSlot.equippedConsumables)
            {
                if (item.itemName == itemName)
                {
                    itemToReturn = item;
                    
                    break;
                }
            }

            // ?щ’?먯꽌 ?ㅼ젣 ?댁젣 (?곗씠?곌? ??젣?섍린 ?꾩뿉 ?꾩뿉??誘몃━ 蹂듭궗?대몺)
            if (equipmentSlot.UnequipConsumable(itemName, 1))
            {
                // ?댁젣 ?깃났 ?? 蹂듭궗?대몦 ?곗씠??ConsumInfo ?ы븿)瑜??몃깽?좊━??異붽?
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
            // heroPos濡???PlayerData 李얘린
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
                Debug.LogError($"[{characterName}] PlayerData瑜?李얠? 紐삵뻽?듬땲?? heroPos:{heroPos}");
                return;
            }

            var info = pd.Info;

            // ?뚮え??
            info.Items = new List<ConsumableInfo>();
            foreach (var item in equipmentSlot.equippedConsumables)
            {
                if (item.ConsumInfo != null)
                {
                    for (int i = 0; i < item.amount; i++)
                        info.Items.Add(item.ConsumInfo);
                }
            }

            // 臾닿린/諛⑹뼱援?
            info.Weapon = equipmentSlot.equippedWeapon.EquipInfo;
            info.Armor = equipmentSlot.equippedArmor.EquipInfo;

            pd.Info = info;
            Debug.Log($"[CharacterUnit] {characterName} ?μ갑 ?뺣낫 PlayerData ?숆린???꾨즺");
        }
    }
}

