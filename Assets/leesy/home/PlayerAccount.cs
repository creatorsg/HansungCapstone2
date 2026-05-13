using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace Lsy
{
    public class CharacterSaveData
    {
        public List<InventoryItem> inventory = new List<InventoryItem>();
        public string selectedWeaponId = "";
        public int purchasedNodeCount = 0;
        public List<PlayerSkill> skills = new List<PlayerSkill>();
        public List<string> unlockedNodeIds = new List<string>();
        public int gold = 0;
    }

    public class PlayerAccount : NetworkBehaviour
    {
        public static PlayerAccount LocalInstance;

        /// <summary>濡쒖뺄 PlayerAccount媛 以鍮꾨릱??????GoldUI ??珥덇린?붿슜</summary>
        public static event Action<PlayerAccount> OnLocalAccountReady;
        public static event Action OnCharacterSwitched;

        /// <summary>紐⑤뱺 PlayerAccount(濡쒖뺄+?먭꺽)媛 myHeroCodes媛 梨꾩썙吏???諛쒖깮 ??珥덉긽???꾩껜 濡쒕뱶??/summary>
        public static event Action<PlayerAccount> OnAnyAccountReady;

        // ??? 怨⑤뱶 (?뚮젅?댁뼱 洹?? ??????????????????????????????????????
        [SyncVar(hook = nameof(OnCurrentGoldChanged))]
        public int currentGold;

        public event Action<int> OnGoldChanged;

        private void OnCurrentGoldChanged(int oldVal, int newVal)
        {
            if (!isOwned) return;
            OnGoldChanged?.Invoke(newVal);
        }

        // ??? 罹먮┃??愿由???????????????????????????????????????????????
        public int currentActiveIndex { get; private set; } = -1;

        public CharacterUnit currentSelectedCharacter;

        public List<GameObject> myCharacterPrefabs = new List<GameObject>();
        public List<CharacterData> myCharacterDataList = new List<CharacterData>();

        [SyncVar] public int myCharacterCount = 0;

        /// <summary>?닿? 議곗쥌?섎뒗 罹먮┃?곗쓽 heroPos 紐⑸줉 ??珥덉긽???뚯쑀沅??먮떒??/summary>
        public readonly SyncList<int> myHeroPositions = new SyncList<int>();

        /// <summary>myHeroPositions? 1:1 ??묓븯??heroCode 紐⑸줉 ??珥덉긽???대?吏 ?쒖떆??/summary>
        public readonly SyncList<string> myHeroCodes = new SyncList<string>();

        // ?쒕쾭 ?꾩슜: ??connection???좏깮??PlayerData 紐⑸줉 (FinalHeroPos ???뺣젹)
        private List<PlayerData> _myPlayerDatas = new List<PlayerData>();

        private Dictionary<int, CharacterSaveData> savedCharacterData = new Dictionary<int, CharacterSaveData>();

        // ??? 珥덇린?????????????????????????????????????????????????????

        public override void OnStartClient()
        {
            base.OnStartClient();
            // myHeroCodes媛 ?섏쨷??梨꾩썙吏??뚮? ?鍮꾪빐 肄쒕갚 ?깅줉
            myHeroCodes.Callback += OnHeroCodesChanged;

            // ?쒕쾭?먯꽌 ?대? ?곗씠?곌? ?덉쑝硫?(???ъ쭊???? 利됱떆 諛쒕룞
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
            // 理쒖큹 ?곗씠?곌? ?꾩쟾???ㅼ뼱???쒖젏(myHeroPositions怨??ш린 ?쇱튂)???대깽??諛쒖깮
            if (myHeroCodes.Count > 0 && myHeroCodes.Count == myHeroPositions.Count)
                OnAnyAccountReady?.Invoke(this);
        }

        public override void OnStartLocalPlayer()
        {
            LocalInstance = this;
            Debug.Log("<color=green>[怨꾩젙] ?묒냽 ?깃났!</color>");
            OnLocalAccountReady?.Invoke(this);
            CmdRequestMyCharacters();
        }

        [Command]
        public void CmdRequestMyCharacters()
        {
            if (myCharacterPrefabs == null || myCharacterPrefabs.Count == 0) return;

            // ??connection???랁븳 紐⑤뱺 PlayerData瑜??섏쭛 (FinalHeroPos ???뺣젹)
            _myPlayerDatas.Clear();
            var allPlayerDatas = FindObjectsByType<PlayerData>(FindObjectsSortMode.None);
            Debug.Log($"[PlayerAccount] CmdRequestMyCharacters: ?꾩껜 PlayerData {allPlayerDatas.Length}媛?諛쒓껄, ??connectionToClient={connectionToClient}");
            foreach (var pd in allPlayerDatas)
            {
                Debug.Log($"[PlayerAccount] PlayerData 寃?? code={pd.FinalHeroCode}, pd.connectionToClient={pd.connectionToClient}, ?쇱튂={pd.connectionToClient == connectionToClient}");
                if (pd.connectionToClient == connectionToClient)
                    _myPlayerDatas.Add(pd);
            }
            _myPlayerDatas.Sort((a, b) => a.FinalHeroPos.CompareTo(b.FinalHeroPos));
            myCharacterCount = _myPlayerDatas.Count;
            Debug.Log($"[PlayerAccount] ??PlayerData {myCharacterCount}媛??섏쭛 ?꾨즺");

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

            // ??Spawn ?꾩뿉 Setup ???ㅽ룿 硫붿떆吏??heroPos/heroCode媛 ?щ컮瑜닿쾶 ?ы븿??
            if (_myPlayerDatas.Count > 0)
            {
                activeUnit.SetupFromPlayerData(_myPlayerDatas[0]);
                currentGold = 2000;
                currentActiveIndex = 0;
                savedCharacterData[0] = new CharacterSaveData { gold = currentGold };
                Debug.Log($"[PlayerAccount] PlayerData 湲곕컲 珥덇린?? code={_myPlayerDatas[0].FinalHeroCode}, pos={_myPlayerDatas[0].FinalHeroPos}");
            }
            else
            {
                if (myCharacterDataList == null || myCharacterDataList.Count == 0) return;
                Debug.LogWarning("[PlayerAccount] PlayerData ?놁쓬 ??CharacterData ?먯뀑?쇰줈 ?대갚?⑸땲??");
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

        // ??? 罹먮┃???꾪솚 ??????????????????????????????????????????????

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

            // ?꾩옱 罹먮┃???곹깭 ???
            if (currentActiveIndex != -1)
            {
                CharacterSaveData backup = new CharacterSaveData();
                foreach (var item in currentSelectedCharacter.myInventory)
                    backup.inventory.Add(item);
                backup.selectedWeaponId = currentSelectedCharacter.selectedWeaponId;
                backup.purchasedNodeCount = currentSelectedCharacter.purchasedNodeCount;
                foreach (var skill in currentSelectedCharacter.mySkills)
                    backup.skills.Add(skill);
                foreach (var nodeId in currentSelectedCharacter.unlockedNodeIds)
                    backup.unlockedNodeIds.Add(nodeId);
                backup.gold = currentGold;
                savedCharacterData[currentActiveIndex] = backup;
            }

            currentActiveIndex = targetIndex;

            // ??罹먮┃???곗씠?곕줈 珥덇린??(怨⑤뱶??嫄대뱶由ъ? ?딆쓬)
            if (hasPlayerDatas)
            {
                PlayerData targetPd = _myPlayerDatas[targetIndex];
                currentSelectedCharacter.SetupFromPlayerData(targetPd);
                Debug.Log($"<color=cyan>[?쒕쾭] {targetPd.FinalHeroCode}?쇰줈 ?ㅼ솑 ?꾨즺!</color>");
            }
            else
            {
                CharacterData targetData = myCharacterDataList[targetIndex];
                currentSelectedCharacter.SetupFromData(targetData);
                Debug.Log($"<color=cyan>[?쒕쾭] {targetData.charName}?쇰줈 ?ㅼ솑 ?꾨즺! (?대갚)</color>");
            }

            // ??λ맂 ?몃깽?좊━/?ㅽ궗 蹂듭썝
            currentSelectedCharacter.myInventory.Clear();
            currentSelectedCharacter.mySkills.Clear();
            currentSelectedCharacter.unlockedNodeIds.Clear();
            currentSelectedCharacter.selectedWeaponId = "";
            currentSelectedCharacter.purchasedNodeCount = 0;

            if (savedCharacterData.TryGetValue(targetIndex, out CharacterSaveData saved))
            {
                currentGold = saved.gold;

                foreach (var item in saved.inventory)
                    currentSelectedCharacter.myInventory.Add(item);
                currentSelectedCharacter.selectedWeaponId = saved.selectedWeaponId;
                currentSelectedCharacter.purchasedNodeCount = saved.purchasedNodeCount;
                foreach (var skill in saved.skills)
                    currentSelectedCharacter.mySkills.Add(skill);
                foreach (var nodeId in saved.unlockedNodeIds)
                    currentSelectedCharacter.unlockedNodeIds.Add(nodeId);
            }
            else
            {
                if (hasPlayerDatas) currentGold = 2000;
                else if (targetIndex >= 0 && targetIndex < myCharacterDataList.Count) currentGold = myCharacterDataList[targetIndex].gold;
            }

            AutoUnlockLv1Nodes(currentSelectedCharacter);
            TargetRpcRefreshUI(connectionToClient, targetIndex);
        }

        [TargetRpc]
        private void TargetRpcRefreshUI(NetworkConnection target, int newActiveIndex)
        {
            currentActiveIndex = newActiveIndex;
            Debug.Log($"<color=cyan>[?대씪?댁뼵?? currentActiveIndex: {newActiveIndex}</color>");

            if (InventoryUI.Instance != null)
                InventoryUI.Instance.RefreshInventory();

            if (GoldUI.Instance != null)
                GoldUI.Instance.RefreshGold();

            OnCharacterSwitched?.Invoke();
        }

        // ??? NPC ?곹샇?묒슜 而ㅻ㎤??(怨⑤뱶 泥댄겕/李④컧 ?대떦) ????????????????

        [Command]
        public void CmdPurchaseTreeNode(string nodeId)
        {
            Debug.Log($"<color=yellow>[SkillTree] CmdPurchaseTreeNode 吏꾩엯 ??nodeId={nodeId}</color>");

            if (currentSelectedCharacter == null) { Debug.LogWarning("[SkillTree] 嫄곕?: currentSelectedCharacter null"); return; }
            if (string.IsNullOrEmpty(nodeId)) { Debug.LogWarning("[SkillTree] 嫄곕?: nodeId 鍮꾩뼱?덉쓬"); return; }

            string characterCode = GetCurrentCharacterCode();
            if (string.IsNullOrEmpty(characterCode))
            {
                Debug.LogWarning("[SkillTree] 嫄곕?: characterCode 鍮꾩뼱?덉쓬");
                return;
            }

            if (!SkillTreeRegistry.TryFind(characterCode, nodeId, out SkillTreeNodeSO node))
            {
                Debug.LogWarning($"[SkillTree] 嫄곕?: ?몃뱶 紐?李얠쓬 ??character={characterCode}, node={nodeId}");
                return;
            }

            if (currentSelectedCharacter.unlockedNodeIds.Contains(nodeId))
            {
                Debug.Log($"[SkillTree] 嫄곕?: ?대? 蹂댁쑀 ??{nodeId}");
                return;
            }

            if (node.prerequisite != null && !currentSelectedCharacter.unlockedNodeIds.Contains(node.prerequisite.nodeId))
            {
                Debug.Log($"[SkillTree] 嫄곕?: prereq 誘몄땐議???{nodeId} requires {node.prerequisite.nodeId}");
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
                    Debug.Log($"[SkillTree] 嫄곕?: 遺꾧린 ?묒옄?앹씪 ??{nodeId} vs ?대? 蹂댁쑀 {unlockedNodeId}");
                    return;
                }
            }

            if (currentGold < node.unlockCost)
            {
                Debug.Log($"[SkillTree] 嫄곕?: 怨⑤뱶 遺議????꾩슂 {node.unlockCost}, 蹂댁쑀 {currentGold}");
                return;
            }

            currentGold -= node.unlockCost;
            currentSelectedCharacter.unlockedNodeIds.Add(nodeId);
            Debug.Log($"<color=green>[SkillTree] 援щℓ ?깃났 ??{nodeId}, ?붿븸 {currentGold}G</color>");
        }

        private string GetCurrentCharacterCode()
        {
            if (currentSelectedCharacter == null) return string.Empty;
            if (!string.IsNullOrEmpty(currentSelectedCharacter.heroCode))
                return currentSelectedCharacter.heroCode;
            return currentSelectedCharacter.characterName;
        }

        /// <summary>
        /// 罹먮┃?곗쓽 Lv1 ?몃뱶 4媛?湲곕낯 ?ㅽ궗)瑜?unlockedNodeIds???먮룞 異붽??⑸땲??
        /// 罹먮┃??珥덇린 ?ㅽ룿 吏곹썑, 洹몃━怨???罹먮┃?곕줈 ?ㅼ솑??吏곹썑(諛깆뾽 ?놁쓣 ?? ?몄텧?섏꽭??
        /// SyncHashSet.Add ??硫깅벑?섎?濡?以묐났 ?몄텧?섏뼱???덉쟾?⑸땲??
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
                Debug.LogWarning("[SkillTree] AutoUnlockLv1Nodes: heroCode/characterName 紐⑤몢 鍮꾩뼱?덉쓬 ???ㅽ궗?몃━ 留ㅼ묶 遺덇?");
                return;
            }

            CharacterSkillTreeSO tree = SkillTreeRegistry.GetTree(code);
            if (tree == null || tree.allNodes == null)
            {
                Debug.LogWarning($"[SkillTree] AutoUnlockLv1Nodes: tree ?놁쓬 ??code={code}");
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
            Debug.Log($"<color=cyan>[SkillTree] AutoUnlock: code={code}, Lv1 ?몃뱶 {added}媛?異붽? (珥?unlocked={unit.unlockedNodeIds.Count})</color>");
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

        // ??? ?ы띁 ?????????????????????????????????????????????????????

        /// <summary>
        /// DontDestroyOnLoad濡??좎??섎뒗 PlayerData 以???connection???뚯쑀??寃껋쓣 諛섑솚?⑸땲??
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
    }
}





