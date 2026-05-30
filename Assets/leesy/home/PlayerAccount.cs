using Jun;
using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lsy
{
    public class CharacterSaveData
    {
        public List<InventoryItem> inventory = new List<InventoryItem>();
        public string selectedWeaponId = "";
        public int purchasedNodeCount = 0;
        // [¼öÁ¤] ´ëÀåÀåÀÌ ¹«±â °­È­ ´Ü°è¸¦ Ä³¸¯ÅÍº°/¹«±âº°·Î ÀúÀåÇÕ´Ï´Ù.
        public Dictionary<string, int> blacksmithWeaponLevels = new Dictionary<string, int>();
        public List<PlayerSkill> skills = new List<PlayerSkill>();
        public List<string> unlockedNodeIds = new List<string>();
        public int gold = 0;

        // --- Ä³¸¯ÅÍº° ÀåÂø »óÅÂ ÀúÀåÀ» À§ÇÑ ÇÊµå Ãß°¡ ---
        public string equippedWeaponId = "";
        public string equippedArmorId = "";
        public InventoryItem equippedWeapon; // ¹«±â °´Ã¼ Á÷Á¢ ¹é¾÷
        public InventoryItem equippedArmor;  // ¹æ¾î±¸ °´Ã¼ Á÷Á¢ ¹é¾÷
        public List<InventoryItem> equippedConsumables = new List<InventoryItem>(); // ¼ÒºñÅÛ °´Ã¼ ¸®½ºÆ® ¹é¾÷
    }

    public class PlayerAccount : NetworkBehaviour
    {
        public static PlayerAccount LocalInstance;

        /// <summary>ë¡œì»¬ PlayerAccountê°€ ì¤€ë¹„ë??????GoldUI ??ì´ˆê¸°?”ìš©</summary>
        public static event Action<PlayerAccount> OnLocalAccountReady;
        public static event Action OnCharacterSwitched;

        /// <summary>ëª¨ë“  PlayerAccount(ë¡œì»¬+?ê²©)ê°€ myHeroCodesê°€ ì±„ì›Œì§???ë°œìƒ ??ì´ˆìƒ???„ì²´ ë¡œë“œ??/summary>
        public static event Action<PlayerAccount> OnAnyAccountReady;

        // ?€?€?€ ê³¨ë“œ (?Œë ˆ?´ì–´ ê·€?? ?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€
        [SyncVar(hook = nameof(OnCurrentGoldChanged))]
        public int currentGold;

        public event Action<int> OnGoldChanged;

        private void OnCurrentGoldChanged(int oldVal, int newVal)
        {
            if (!isOwned) return;
            OnGoldChanged?.Invoke(newVal);
        }

        // ?€?€?€ ìºë¦­??ê´€ë¦??€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€
        public int currentActiveIndex { get; private set; } = -1;

        public CharacterUnit currentSelectedCharacter;

        public List<GameObject> myCharacterPrefabs = new List<GameObject>();
        public List<CharacterData> myCharacterDataList = new List<CharacterData>();

        [SyncVar] public int myCharacterCount = 0;

        /// <summary>?´ê? ì¡°ì¢…?˜ëŠ” ìºë¦­?°ì˜ heroPos ëª©ë¡ ??ì´ˆìƒ???Œìœ ê¶??ë‹¨??/summary>
        public readonly SyncList<int> myHeroPositions = new SyncList<int>();

        /// <summary>myHeroPositions?€ 1:1 ?€?‘í•˜??heroCode ëª©ë¡ ??ì´ˆìƒ???´ë?ì§€ ?œì‹œ??/summary>
        public readonly SyncList<string> myHeroCodes = new SyncList<string>();

        // ?œë²„ ?„ìš©: ??connection??? íƒ??PlayerData ëª©ë¡ (FinalHeroPos ???•ë ¬)
        private List<PlayerData> _myPlayerDatas = new List<PlayerData>();

        private Dictionary<int, CharacterSaveData> savedCharacterData = new Dictionary<int, CharacterSaveData>(); public Dictionary<int, CharacterSaveData> SavedCharacterData => savedCharacterData;

        // ?€?€?€ ì´ˆê¸°???€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€

        public override void OnStartClient()
        {
            base.OnStartClient();
            // myHeroCodesê°€ ?˜ì¤‘??ì±„ì›Œì§??Œë? ?€ë¹„í•´ ì½œë°± ?±ë¡
            myHeroCodes.Callback += OnHeroCodesChanged;

            // ?œë²„?ì„œ ?´ë? ?°ì´?°ê? ?ˆìœ¼ë©?(???¬ì§„???? ì¦‰ì‹œ ë°œë™
            if (myHeroCodes.Count > 0)
                OnAnyAccountReady?.Invoke(this);
        }

        public override void OnStopClient()
        {
            base.OnStopClient();
            myHeroCodes.Callback -= OnHeroCodesChanged;
        }

        private void OnHeroCodesChanged(SyncList<string>.Operation op, int index, string oldItem, string newItem)
        {
            // ìµœì´ˆ ?°ì´?°ê? ?„ì „???¤ì–´???œì (myHeroPositionsê³??¬ê¸° ?¼ì¹˜)???´ë²¤??ë°œìƒ
            if (myHeroCodes.Count > 0 && myHeroCodes.Count == myHeroPositions.Count)
                OnAnyAccountReady?.Invoke(this);
        }

        public override void OnStartLocalPlayer()
        {
            LocalInstance = this;
            Debug.Log("<color=green>[ê³„ì •] ?‘ì† ?±ê³µ!</color>");
            OnLocalAccountReady?.Invoke(this);
            CmdRequestMyCharacters();
        }

        [Command]
        public void CmdRequestMyCharacters()
        {
            if (myCharacterPrefabs == null || myCharacterPrefabs.Count == 0) return;

            // ??connection???í•œ ëª¨ë“  PlayerDataë¥??˜ì§‘ (FinalHeroPos ???•ë ¬)
            _myPlayerDatas.Clear();
            var allPlayerDatas = FindObjectsByType<PlayerData>(FindObjectsSortMode.None);
            Debug.Log($"[PlayerAccount] CmdRequestMyCharacters: ?„ì²´ PlayerData {allPlayerDatas.Length}ê°?ë°œê²¬, ??connectionToClient={connectionToClient}");
            foreach (var pd in allPlayerDatas)
            {
                Debug.Log($"[PlayerAccount] PlayerData ê²€?? code={pd.FinalHeroCode}, pd.connectionToClient={pd.connectionToClient}, ?¼ì¹˜={pd.connectionToClient == connectionToClient}");
                if (pd.connectionToClient == connectionToClient)
                    _myPlayerDatas.Add(pd);
            }
            _myPlayerDatas.Sort((a, b) => a.FinalHeroPos.CompareTo(b.FinalHeroPos));
            myCharacterCount = _myPlayerDatas.Count;
            Debug.Log($"[PlayerAccount] ??PlayerData {myCharacterCount}ê°??˜ì§‘ ?„ë£Œ");

            myHeroPositions.Clear();
            myHeroCodes.Clear();
            foreach (var pd in _myPlayerDatas)
            {
                myHeroPositions.Add(pd.FinalHeroPos);
                myHeroCodes.Add(pd.FinalHeroCode);
            }

            GameObject newCharObj = Instantiate(myCharacterPrefabs[0]);
            CharacterUnit activeUnit = newCharObj.GetComponent<CharacterUnit>();
            if (activeUnit == null) return;

            // ??Spawn ?„ì— Setup ???¤í° ë©”ì‹œì§€??heroPos/heroCodeê°€ ?¬ë°”ë¥´ê²Œ ?¬í•¨??
            if (_myPlayerDatas.Count > 0)
            {
                int goldBeforeSetup = _myPlayerDatas[0].Info.Gold;
                activeUnit.SetupFromPlayerData(_myPlayerDatas[0]);
                currentGold = goldBeforeSetup;
                currentActiveIndex = 0;
                //savedCharacterData[0] = new CharacterSaveData { gold = currentGold };
                var initSave = new CharacterSaveData { gold = currentGold };
                foreach (var item in activeUnit.myInventory)
                    initSave.inventory.Add(item);

                // ¼Ò¸ğÇ° ½½·Ôµµ ÃÊ±â ÀúÀå¿¡ Æ÷ÇÔ
                var initSlot = activeUnit.equipmentSlot;
                if (initSlot != null)
                {
                    foreach (var item in initSlot.equippedConsumables)
                        initSave.equippedConsumables.Add(item);
                }

                savedCharacterData[0] = initSave;
                Debug.Log($"[PlayerAccount] PlayerData ê¸°ë°˜ ì´ˆê¸°?? code={_myPlayerDatas[0].FinalHeroCode}, pos={_myPlayerDatas[0].FinalHeroPos}");
            }
            else
            {
                if (myCharacterDataList == null || myCharacterDataList.Count == 0) return;
                Debug.LogWarning("[PlayerAccount] PlayerData ?†ìŒ ??CharacterData ?ì…‹?¼ë¡œ ?´ë°±?©ë‹ˆ??");
                activeUnit.SetupFromData(myCharacterDataList[0]);
                currentGold = myCharacterDataList[0].gold;
                savedCharacterData[0] = new CharacterSaveData { gold = currentGold };
            }

            NetworkServer.Spawn(newCharObj, connectionToClient);
            NetworkServer.ReplacePlayerForConnection(connectionToClient, newCharObj, ReplacePlayerOptions.KeepAuthority);

            currentSelectedCharacter = activeUnit;
            AutoUnlockLv1Nodes(activeUnit);
            TargetRpcRefreshUI(connectionToClient, currentActiveIndex);
        }

        // ?€?€?€ ìºë¦­???„í™˜ ?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€

        public void SelectCharacter(int profileIndex)
        {
            if (profileIndex < 0 || profileIndex >= myCharacterCount) return;
            if (profileIndex == currentActiveIndex) return;

            CmdRequestSwapCharacter(profileIndex);
        }
        [Command]
        public void CmdRequestSwapCharacter(int targetIndex)
        {
            bool hasPlayerDatas = _myPlayerDatas != null && _myPlayerDatas.Count > 0;
            int maxCount = hasPlayerDatas ? _myPlayerDatas.Count : myCharacterDataList.Count;

            if (targetIndex < 0 || targetIndex >= maxCount) return;
            if (currentActiveIndex == targetIndex) return;
            if (currentSelectedCharacter == null) return;

            // ÇöÀç Ä³¸¯ÅÍÀÇ »óÅÂ¸¦ ¼­¹ö ¸Ş¸ğ¸®¿¡ ¹é¾÷
            if (currentActiveIndex != -1)
            {
                CharacterSaveData backup = new CharacterSaveData();

                foreach (var item in currentSelectedCharacter.myInventory) backup.inventory.Add(item);
                backup.selectedWeaponId = currentSelectedCharacter.selectedWeaponId;
                backup.purchasedNodeCount = currentSelectedCharacter.purchasedNodeCount;
                foreach (var weaponLevel in currentSelectedCharacter.blacksmithWeaponLevels)
                    backup.blacksmithWeaponLevels[weaponLevel.Key] = weaponLevel.Value;
                foreach (var skill in currentSelectedCharacter.mySkills) backup.skills.Add(skill);
                foreach (var nodeId in currentSelectedCharacter.unlockedNodeIds) backup.unlockedNodeIds.Add(nodeId);
                backup.gold = currentGold;

                var slot = currentSelectedCharacter.equipmentSlot;
                if (slot != null)
                {
                    backup.equippedWeaponId = slot.equippedWeaponId;
                    backup.equippedArmorId = slot.equippedArmorId;
                    backup.equippedWeapon = slot.equippedWeapon;
                    backup.equippedArmor = slot.equippedArmor;
                    foreach (var item in slot.equippedConsumables) backup.equippedConsumables.Add(item);
                }

                savedCharacterData[currentActiveIndex] = backup;
            }

            // Ä³¸¯ÅÍ ¿ÀºêÁ§Æ® ÃÊ±âÈ­ (»õ µ¥ÀÌÅÍ¸¦ ¹Ş±â À§ÇØ ºñ¿ì±â)
            // ÀÌÀü Ä³¸¯ÅÍÀÇ µ¥ÀÌÅÍ°¡ ³²¾ÆÀÖÁö ¾Êµµ·Ï "¸ÕÀú" ºñ¿ó´Ï´Ù.
            currentSelectedCharacter.myInventory.Clear();
            currentSelectedCharacter.mySkills.Clear();
            currentSelectedCharacter.unlockedNodeIds.Clear();
            // [¼öÁ¤] Ä³¸¯ÅÍº° ´ëÀåÀåÀÌ ¹«±â °­È­ »óÅÂ°¡ ´Ù¸¥ Ä³¸¯ÅÍ·Î °øÀ¯µÇÁö ¾Êµµ·Ï ¸ÕÀú ºñ¿ó´Ï´Ù.
            currentSelectedCharacter.selectedWeaponId = "";
            currentSelectedCharacter.purchasedNodeCount = 0;
            currentSelectedCharacter.blacksmithWeaponLevels.Clear();

            var targetSlot = currentSelectedCharacter.equipmentSlot;
            if (targetSlot != null)
            {
                targetSlot.equippedWeaponId = "";
                targetSlot.equippedArmorId = "";
                targetSlot.equippedWeapon = default;
                targetSlot.equippedArmor = default;
                targetSlot.equippedConsumables.Clear();
            }

            // ÀÎµ¦½º ÀüÈ¯ ¹× µ¥ÀÌÅÍ ·Îµå
            currentActiveIndex = targetIndex;

            // ÀúÀåµÈ µ¥ÀÌÅÍ°¡ ÀÖ´ÂÁö ¸ÕÀú È®ÀÎÇÕ´Ï´Ù.
            if (savedCharacterData.TryGetValue(targetIndex, out CharacterSaveData saved))
            {
                // ÀÌ¹Ì ÇÃ·¹ÀÌÇÑ ÀûÀÌ ÀÖ´Â Ä³¸¯ÅÍ: ÀúÀåµÈ µ¥ÀÌÅÍ º¹¿ø
                // ÀÌ¶§´Â SetupFromData¸¦ È£ÃâÇÏÁö ¾Ê°Å³ª, È£Ãâ ÈÄ ÀúÀåµÈ µ¥ÀÌÅÍ·Î µ¤¾î¾¹´Ï´Ù.
                if (hasPlayerDatas) currentSelectedCharacter.SetupFromPlayerData(_myPlayerDatas[targetIndex]);
                else currentSelectedCharacter.SetupFromData(myCharacterDataList[targetIndex]);

                // Setup °úÁ¤¿¡¼­ µé¾î°£ ±âº» ¾ÆÀÌÅÛµéÀ» Áö¿ì°í ÀúÀåµÈ »óÅÂ·Î ±³Ã¼
                currentSelectedCharacter.myInventory.Clear();
                currentSelectedCharacter.mySkills.Clear();
                currentSelectedCharacter.unlockedNodeIds.Clear();

                currentGold = saved.gold;
                foreach (var item in saved.inventory) currentSelectedCharacter.myInventory.Add(item);
                currentSelectedCharacter.selectedWeaponId = saved.selectedWeaponId;
                currentSelectedCharacter.purchasedNodeCount = saved.purchasedNodeCount;
                currentSelectedCharacter.blacksmithWeaponLevels.Clear();
                foreach (var weaponLevel in saved.blacksmithWeaponLevels)
                    currentSelectedCharacter.blacksmithWeaponLevels[weaponLevel.Key] = weaponLevel.Value;
                foreach (var skill in saved.skills) currentSelectedCharacter.mySkills.Add(skill);
                foreach (var nodeId in saved.unlockedNodeIds) currentSelectedCharacter.unlockedNodeIds.Add(nodeId);

                if (targetSlot != null)
                {
                    targetSlot.equippedWeaponId = saved.equippedWeaponId;
                    targetSlot.equippedWeapon = saved.equippedWeapon;
                    targetSlot.equippedArmorId = saved.equippedArmorId;
                    targetSlot.equippedArmor = saved.equippedArmor;
                    // [¼öÁ¤] ÀúÀåµÈ È¨ ÀåÂø ¼Ò¸ğÇ°¸¸ º¹¿øÇÕ´Ï´Ù.
                    targetSlot.equippedConsumables.Clear();
                    foreach (var item in saved.equippedConsumables) targetSlot.equippedConsumables.Add(item);
                }
            }
            else
            {
                // Ã³À½ ¼±ÅÃÇÏ´Â Ä³¸¯ÅÍ: ±âº» µ¥ÀÌÅÍ(ÃÊ±â ¾ÆÀÌÅÛ µî) ·Îµå
                if (hasPlayerDatas)
                {
                    PlayerData targetPd = _myPlayerDatas[targetIndex];
                    int goldBeforeSetup = targetPd.Info.Gold;  
                    currentSelectedCharacter.SetupFromPlayerData(targetPd);
                    currentGold = goldBeforeSetup;
                }
                else
                {
                    CharacterData targetData = myCharacterDataList[targetIndex];
                    currentSelectedCharacter.SetupFromData(targetData);
                    currentGold = targetData.gold;
                }
                // SetupFromData ³»ºÎ¿¡¼­ ÀÎº¥Åä¸®¿Í ½ºÅ³ÀÌ Ã¤¿öÁö¹Ç·Î ±×´ë·Î µÓ´Ï´Ù.
            }

            //  ÃÖÁ¾ °»½Å ¹× UI Åëº¸
            AutoUnlockLv1Nodes(currentSelectedCharacter);
            TargetRpcRefreshUI(connectionToClient, targetIndex);
        }


        [TargetRpc]
        private void TargetRpcRefreshUI(NetworkConnection target, int newActiveIndex)
        {
            currentActiveIndex = newActiveIndex;
            Debug.Log($"<color=cyan>[?´ë¼?´ì–¸?? currentActiveIndex: {newActiveIndex}</color>");

            if (InventoryUI.Instance != null)
                InventoryUI.Instance.RefreshInventory();

            if (GoldUI.Instance != null)
                GoldUI.Instance.RefreshGold();

            OnCharacterSwitched?.Invoke();
        }

        // ?€?€?€ NPC ?í˜¸?‘ìš© ì»¤ë§¨??(ê³¨ë“œ ì²´í¬/ì°¨ê° ?´ë‹¹) ?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€

        [Command]
        public void CmdPurchaseTreeNode(string nodeId)
        {
            Debug.Log($"<color=yellow>[SkillTree] CmdPurchaseTreeNode ì§„ì… ??nodeId={nodeId}</color>");

            if (currentSelectedCharacter == null) { Debug.LogWarning("[SkillTree] ê±°ë?: currentSelectedCharacter null"); return; }
            if (string.IsNullOrEmpty(nodeId)) { Debug.LogWarning("[SkillTree] ê±°ë?: nodeId ë¹„ì–´?ˆìŒ"); return; }

            string characterCode = GetCurrentCharacterCode();
            if (string.IsNullOrEmpty(characterCode))
            {
                Debug.LogWarning("[SkillTree] ê±°ë?: characterCode ë¹„ì–´?ˆìŒ");
                return;
            }

            if (!SkillTreeRegistry.TryFind(characterCode, nodeId, out SkillTreeNodeSO node))
            {
                Debug.LogWarning($"[SkillTree] ê±°ë?: ?¸ë“œ ëª?ì°¾ìŒ ??character={characterCode}, node={nodeId}");
                return;
            }

            if (currentSelectedCharacter.unlockedNodeIds.Contains(nodeId))
            {
                Debug.Log($"[SkillTree] ê±°ë?: ?´ë? ë³´ìœ  ??{nodeId}");
                return;
            }

            if (node.prerequisite != null && !currentSelectedCharacter.unlockedNodeIds.Contains(node.prerequisite.nodeId))
            {
                Debug.Log($"[SkillTree] ê±°ë?: prereq ë¯¸ì¶©ì¡???{nodeId} requires {node.prerequisite.nodeId}");
                return;
            }

            foreach (string unlockedNodeId in currentSelectedCharacter.unlockedNodeIds)
            {
                SkillTreeNodeSO unlockedNode = SkillTreeRegistry.Find(characterCode, unlockedNodeId);
                if (unlockedNode == null) continue;

                bool sameBranchChoice =
                    unlockedNode.skillIndex == node.skillIndex &&
                    unlockedNode.level == node.level;

                if (sameBranchChoice)
                {
                    Debug.Log($"[SkillTree] ê±°ë?: ë¶„ê¸° ?‘ì?ì¼ ??{nodeId} vs ?´ë? ë³´ìœ  {unlockedNodeId}");
                    return;
                }
            }

            if (currentGold < node.unlockCost)
            {
                Debug.Log($"[SkillTree] ê±°ë?: ê³¨ë“œ ë¶€ì¡????„ìš” {node.unlockCost}, ë³´ìœ  {currentGold}");
                return;
            }

            currentGold -= node.unlockCost;

            currentSelectedCharacter.unlockedNodeIds.Add(nodeId);
            Debug.Log($"<color=green>[SkillTree] êµ¬ë§¤ ?±ê³µ ??{nodeId}, ?”ì•¡ {currentGold}G</color>");
        }

        private string GetCurrentCharacterCode()
        {
            if (currentSelectedCharacter == null) return string.Empty;
            if (!string.IsNullOrEmpty(currentSelectedCharacter.heroCode))
                return currentSelectedCharacter.heroCode;
            return currentSelectedCharacter.characterName;
        }

        /// <summary>
        /// ìºë¦­?°ì˜ Lv1 ?¸ë“œ 4ê°?ê¸°ë³¸ ?¤í‚¬)ë¥?unlockedNodeIds???ë™ ì¶”ê??©ë‹ˆ??
        /// ìºë¦­??ì´ˆê¸° ?¤í° ì§í›„, ê·¸ë¦¬ê³???ìºë¦­?°ë¡œ ?¤ì™‘??ì§í›„(ë°±ì—… ?†ì„ ?? ?¸ì¶œ?˜ì„¸??
        /// SyncHashSet.Add ??ë©±ë“±?˜ë?ë¡?ì¤‘ë³µ ?¸ì¶œ?˜ì–´???ˆì „?©ë‹ˆ??
        /// </summary>
        [Server]
        private void AutoUnlockLv1Nodes(CharacterUnit unit)
        {
            if (unit == null)
            {
                Debug.LogWarning("[SkillTree] AutoUnlockLv1Nodes: unit??null");
                return;
            }

            string code = !string.IsNullOrEmpty(unit.heroCode) ? unit.heroCode : unit.characterName;
            if (string.IsNullOrEmpty(code))
            {
                Debug.LogWarning("[SkillTree] AutoUnlockLv1Nodes: heroCode/characterName ëª¨ë‘ ë¹„ì–´?ˆìŒ ???¤í‚¬?¸ë¦¬ ë§¤ì¹­ ë¶ˆê?");
                return;
            }

            CharacterSkillTreeSO tree = SkillTreeRegistry.GetTree(code);
            if (tree == null || tree.allNodes == null)
            {
                Debug.LogWarning($"[SkillTree] AutoUnlockLv1Nodes: tree ?†ìŒ ??code={code}");
                return;
            }

            int added = 0;
            foreach (SkillTreeNodeSO node in tree.allNodes)
            {
                if (node == null) continue;
                if (node.level != 1) continue;
                if (string.IsNullOrEmpty(node.nodeId)) continue;
                if (unit.unlockedNodeIds.Add(node.nodeId)) added++;
            }
            Debug.Log($"<color=cyan>[SkillTree] AutoUnlock: code={code}, Lv1 ?¸ë“œ {added}ê°?ì¶”ê? (ì´?unlocked={unit.unlockedNodeIds.Count})</color>");
        }

        [Command]
        public void CmdBlacksmithUpgrade(string weaponId, int nodeIndex, int npcLevel, int price)
        {
            if (currentSelectedCharacter == null) return;
            if (currentGold < price) return;
            if (!currentSelectedCharacter.ApplyBlacksmithUpgrade(weaponId, nodeIndex, npcLevel)) return;
            currentGold -= price;
        }

        [Command]
        public void CmdSkillPurchase(string skillId, int npcLevel, int price, int requiredNpcLevel)
        {
            if (currentSelectedCharacter == null) return;
            if (currentGold < price) return;
            if (!currentSelectedCharacter.ApplySkillPurchase(skillId, npcLevel, requiredNpcLevel)) return;
            currentGold -= price;
        }

        [Command]
        public void CmdBartenderHeal()
        {
            if (currentSelectedCharacter == null) return;
            currentSelectedCharacter.ApplyBartenderHeal();
        }

        [Command]
        public void CmdInformantUpgrade(string targetSkillName, int npcLevel)
        {
            if (currentSelectedCharacter == null) return;
            currentSelectedCharacter.ApplyInformantUpgrade(targetSkillName, npcLevel);
        }

        // ?€?€?€ ?¬í¼ ?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€?€

        /// <summary>
        /// DontDestroyOnLoadë¡?? ì??˜ëŠ” PlayerData ì¤???connection???Œìœ ??ê²ƒì„ ë°˜í™˜?©ë‹ˆ??
        /// </summary>
        [Server]
        private PlayerData FindPlayerDataForConnection()
        {
            var allPlayerDatas = FindObjectsByType<PlayerData>(FindObjectsSortMode.None);
            foreach (var pd in allPlayerDatas)
            {
                if (pd.connectionToClient == connectionToClient)
                    return pd;
            }
            return null;
        }


        //ÀüÅõ¾ÀÀ¸·Î ³Ñ¾î°¥¶§ ÀÎº¥Åä¸® Á¤º¸ ¾÷µ¥ÀÌÆ®

        /// <summary>
        /// ¾ÆÀÌÅÛ ±¸¸Å/È¹µæ Á÷ÈÄ Áï½Ã È£Ãâ: myInventory ¡æ pd.Info.Items ¸¸ µ¿±âÈ­ÇÕ´Ï´Ù.
        /// ÀåÂø ½½·Ô(Weapon/Armor/Expendables)Àº °Çµå¸®Áö ¾Ê½À´Ï´Ù.
        /// </summary>
        [Server]
        public void SyncInventoryToPlayerData()
        {
            if (currentActiveIndex < 0 || currentSelectedCharacter == null) return;

            foreach (var pd in FindObjectsByType<PlayerData>(FindObjectsSortMode.None))
            {
                if (pd.connectionToClient != connectionToClient) continue;
                if (_myPlayerDatas.IndexOf(pd) != currentActiveIndex) continue;

                if (pd.Info != null)
                {
                    pd.Info.Items = new List<InventoryItem>(currentSelectedCharacter.myInventory);
                    pd.Info.Gold = currentGold;
                }
                Debug.Log($"[SyncInv] {pd.FinalHeroCode} Items={pd.Info?.Items?.Count}");
                break;
            }
        }

        public void SyncAllHideoutDataToBattleData()
        {
            // 1. ÇöÀç ¾À¿¡ ÀÖ´Â ¸ğµç PlayerDataPrefab(Clone) °´Ã¼µéÀ» Ã£½À´Ï´Ù.
            PlayerData[] allPlayerDatas = FindObjectsByType<PlayerData>(FindObjectsSortMode.None);
            if (allPlayerDatas != null) Debug.Log("Ã£±â¿Ï·á");
            // 2. Ã£Àº °´Ã¼µé Áß "³» °Í"¸¸ °ñ¶ó¼­ ¾÷µ¥ÀÌÆ®ÇÕ´Ï´Ù.
            foreach (var pd in allPlayerDatas)
            {
                // ³» connection¿¡ ¼ÓÇÑ PlayerDataÀÎÁö È®ÀÎ
                if (pd.connectionToClient != connectionToClient) continue;

                // ÀÌ pd°¡ ³» Ä³¸¯ÅÍ ¸®½ºÆ®(_myPlayerDatas)¿¡¼­ ¸î ¹øÂ° Ä³¸¯ÅÍÀÎÁö È®ÀÎ
                int charIndex = _myPlayerDatas.IndexOf(pd);
                if (charIndex == -1) continue;

                // 3. Á¤º¸ ÁÖÀÔ (ÇöÀç ²¨³»Á® ÀÖ´Â Ä³¸¯ÅÍ vs ÀúÀåµÈ µ¥ÀÌÅÍ)
                if (charIndex == currentActiveIndex && currentSelectedCharacter != null)
                {
                    // [ÇöÀç È°¼ºÈ­µÈ Ä³¸¯ÅÍ] ½Ç½Ã°£ EquipmentSlot¿¡¼­ °¡Á®¿À±â
                    var slot = currentSelectedCharacter.equipmentSlot;
                    if (slot != null && pd.Info != null)
                    {
                        // ÀåÂøµÈ ¹«±â/¹æ¾î±¸
                        pd.Info.Weapon = !string.IsNullOrEmpty(slot.equippedWeaponId) ? slot.equippedWeapon.EquipInfo : null;
                        pd.Info.Armor  = !string.IsNullOrEmpty(slot.equippedArmorId)  ? slot.equippedArmor.EquipInfo  : null;

                        // ÀåÂøµÈ ¼Ò¸ğÇ° ¡æ Expendables
                        pd.Info.Expendables = new List<ConsumableInfo>();
                        foreach (var item in slot.equippedConsumables)
                            if (item.ConsumInfo != null)
                            {
                                ConsumableInfo clone = item.ConsumInfo.Clone();
                                clone.amount = item.amount;  
                                pd.Info.Expendables.Add(clone);
                            }

                        // ¹ÌÀåÂø ÀÎº¥ ¡æ Items
                        pd.Info.Items = new List<InventoryItem>(currentSelectedCharacter.myInventory);
                        pd.Info.Gold = currentGold;

                        Debug.Log($"[Sync] ÇöÀç Ä³¸¯ÅÍ({pd.FinalHeroCode}) ½Ç½Ã°£ Á¤º¸ ¾÷µ¥ÀÌÆ® ¿Ï·á");
                    }
                }
                else if (savedCharacterData.TryGetValue(charIndex, out var saved))
                {
                    // [ºñÈ°¼ºÈ­µÈ Ä³¸¯ÅÍ] ÀÌÀü¿¡ ÀúÀåÇØµĞ savedCharacterData¿¡¼­ °¡Á®¿À±â
                    if (pd.Info != null)
                    {
                        pd.Info.Weapon = saved.equippedWeapon.EquipInfo;
                        pd.Info.Armor  = saved.equippedArmor.EquipInfo;

                        pd.Info.Expendables = new List<ConsumableInfo>();
                        foreach (var item in saved.equippedConsumables)
                            if (item.ConsumInfo != null)
                            {
                                ConsumableInfo clone = item.ConsumInfo.Clone();
                                clone.amount = item.amount;
                                pd.Info.Expendables.Add(clone);
                            }

                        pd.Info.Items = new List<InventoryItem>(saved.inventory ?? new List<InventoryItem>());

                        Debug.Log($"[Sync] ´ë±â ÁßÀÎ Ä³¸¯ÅÍ({pd.FinalHeroCode}) ÀúÀåµÈ Á¤º¸ ¾÷µ¥ÀÌÆ® ¿Ï·á");
                    }
                }
            }
        }

        // [¼öÁ¤] º´ÇÕ Àü È£ÃâºÎ(BattleStartBtn/ReadyOrStartButton/QuestVoteSystem)°¡ ±â´ëÇÏ´Â Á¤Àû µ¿±âÈ­ ÁøÀÔÁ¡À» º¹±¸ÇÕ´Ï´Ù.
        public static void SyncAllAccountsHideoutDataToBattleData()
        {
            foreach (var account in FindObjectsByType<PlayerAccount>(FindObjectsSortMode.None))
                account.SyncAllHideoutDataToBattleData();
        }

     
    }
}





