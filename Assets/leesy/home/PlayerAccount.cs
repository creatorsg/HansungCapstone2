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
        public List<PlayerSkill> skills = new List<PlayerSkill>();
        public List<string> unlockedNodeIds = new List<string>();
        public int gold = 0;

        // --- 캐릭터별 장착 상태 저장을 위한 필드 추가 ---
        public string equippedWeaponId = "";
        public string equippedArmorId = "";
        public InventoryItem equippedWeapon; // 무기 객체 직접 백업
        public InventoryItem equippedArmor;  // 방어구 객체 직접 백업
        public List<InventoryItem> equippedConsumables = new List<InventoryItem>(); // 소비템 객체 리스트 백업
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

        private Dictionary<int, CharacterSaveData> savedCharacterData = new Dictionary<int, CharacterSaveData>(); public Dictionary<int, CharacterSaveData> SavedCharacterData => savedCharacterData;

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

            // 현재 캐릭터의 상태를 서버 메모리에 백업
            if (currentActiveIndex != -1)
            {
                CharacterSaveData backup = new CharacterSaveData();

                foreach (var item in currentSelectedCharacter.myInventory) backup.inventory.Add(item);
                backup.selectedWeaponId = currentSelectedCharacter.selectedWeaponId;
                backup.purchasedNodeCount = currentSelectedCharacter.purchasedNodeCount;
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

            // 캐릭터 오브젝트 초기화 (새 데이터를 받기 위해 비우기)
            // 이전 캐릭터의 데이터가 남아있지 않도록 "먼저" 비웁니다.
            currentSelectedCharacter.myInventory.Clear();
            currentSelectedCharacter.mySkills.Clear();
            currentSelectedCharacter.unlockedNodeIds.Clear();

            var targetSlot = currentSelectedCharacter.equipmentSlot;
            if (targetSlot != null)
            {
                targetSlot.equippedWeaponId = "";
                targetSlot.equippedArmorId = "";
                targetSlot.equippedWeapon = default;
                targetSlot.equippedArmor = default;
                targetSlot.equippedConsumables.Clear();
            }

            // 인덱스 전환 및 데이터 로드
            currentActiveIndex = targetIndex;

            // 저장된 데이터가 있는지 먼저 확인합니다.
            if (savedCharacterData.TryGetValue(targetIndex, out CharacterSaveData saved))
            {
                // 이미 플레이한 적이 있는 캐릭터: 저장된 데이터 복원
                // 이때는 SetupFromData를 호출하지 않거나, 호출 후 저장된 데이터로 덮어씁니다.
                if (hasPlayerDatas) currentSelectedCharacter.SetupFromPlayerData(_myPlayerDatas[targetIndex]);
                else currentSelectedCharacter.SetupFromData(myCharacterDataList[targetIndex]);

                // Setup 과정에서 들어간 기본 아이템들을 지우고 저장된 상태로 교체
                currentSelectedCharacter.myInventory.Clear();
                currentSelectedCharacter.mySkills.Clear();
                currentSelectedCharacter.unlockedNodeIds.Clear();

                currentGold = saved.gold;
                foreach (var item in saved.inventory) currentSelectedCharacter.myInventory.Add(item);
                currentSelectedCharacter.selectedWeaponId = saved.selectedWeaponId;
                currentSelectedCharacter.purchasedNodeCount = saved.purchasedNodeCount;
                foreach (var skill in saved.skills) currentSelectedCharacter.mySkills.Add(skill);
                foreach (var nodeId in saved.unlockedNodeIds) currentSelectedCharacter.unlockedNodeIds.Add(nodeId);

                if (targetSlot != null)
                {
                    targetSlot.equippedWeaponId = saved.equippedWeaponId;
                    targetSlot.equippedWeapon = saved.equippedWeapon;
                    targetSlot.equippedArmorId = saved.equippedArmorId;
                    targetSlot.equippedArmor = saved.equippedArmor;
                    // [수정] 저장된 홈 장착 소모품만 복원합니다.
                    targetSlot.equippedConsumables.Clear();
                    foreach (var item in saved.equippedConsumables) targetSlot.equippedConsumables.Add(item);
                }
            }
            else
            {
                // 처음 선택하는 캐릭터: 기본 데이터(초기 아이템 등) 로드
                if (hasPlayerDatas)
                {
                    PlayerData targetPd = _myPlayerDatas[targetIndex];
                    currentSelectedCharacter.SetupFromPlayerData(targetPd);
                    currentGold = 2000;
                }
                else
                {
                    CharacterData targetData = myCharacterDataList[targetIndex];
                    currentSelectedCharacter.SetupFromData(targetData);
                    currentGold = targetData.gold;
                }
                // SetupFromData 내부에서 인벤토리와 스킬이 채워지므로 그대로 둡니다.
            }

            //  최종 갱신 및 UI 통보
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


        //전투씬으로 넘어갈때 인벤토리 정보 업데이트

        /// <summary>
        /// 아이템 구매/획득 직후 즉시 호출: myInventory → pd.Info.Items 만 동기화합니다.
        /// 장착 슬롯(Weapon/Armor/Expendables)은 건드리지 않습니다.
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
                    pd.Info.Items = new List<InventoryItem>(currentSelectedCharacter.myInventory);

                Debug.Log($"[SyncInv] {pd.FinalHeroCode} Items={pd.Info?.Items?.Count}");
                break;
            }
        }

        public void SyncAllHideoutDataToBattleData()
        {
            // 1. 현재 씬에 있는 모든 PlayerDataPrefab(Clone) 객체들을 찾습니다.
            PlayerData[] allPlayerDatas = FindObjectsByType<PlayerData>(FindObjectsSortMode.None);
            if (allPlayerDatas != null) Debug.Log("찾기완료");
            // 2. 찾은 객체들 중 "내 것"만 골라서 업데이트합니다.
            foreach (var pd in allPlayerDatas)
            {
                // 내 connection에 속한 PlayerData인지 확인
                if (pd.connectionToClient != connectionToClient) continue;

                // 이 pd가 내 캐릭터 리스트(_myPlayerDatas)에서 몇 번째 캐릭터인지 확인
                int charIndex = _myPlayerDatas.IndexOf(pd);
                if (charIndex == -1) continue;

                // 3. 정보 주입 (현재 꺼내져 있는 캐릭터 vs 저장된 데이터)
                if (charIndex == currentActiveIndex && currentSelectedCharacter != null)
                {
                    // [현재 활성화된 캐릭터] 실시간 EquipmentSlot에서 가져오기
                    var slot = currentSelectedCharacter.equipmentSlot;
                    if (slot != null && pd.Info != null)
                    {
                        // 장착된 무기/방어구
                        pd.Info.Weapon = !string.IsNullOrEmpty(slot.equippedWeaponId) ? slot.equippedWeapon.EquipInfo : null;
                        pd.Info.Armor  = !string.IsNullOrEmpty(slot.equippedArmorId)  ? slot.equippedArmor.EquipInfo  : null;

                        // 장착된 소모품 → Expendables
                        pd.Info.Expendables = new List<ConsumableInfo>();
                        foreach (var item in slot.equippedConsumables)
                            if (item.ConsumInfo != null)
                                for (int i = 0; i < item.amount; i++) pd.Info.Expendables.Add(item.ConsumInfo);

                        // 미장착 인벤 → Items
                        pd.Info.Items = new List<InventoryItem>(currentSelectedCharacter.myInventory);

                        Debug.Log($"[Sync] 현재 캐릭터({pd.FinalHeroCode}) 실시간 정보 업데이트 완료");
                    }
                }
                else if (savedCharacterData.TryGetValue(charIndex, out var saved))
                {
                    // [비활성화된 캐릭터] 이전에 저장해둔 savedCharacterData에서 가져오기
                    if (pd.Info != null)
                    {
                        pd.Info.Weapon = saved.equippedWeapon.EquipInfo;
                        pd.Info.Armor  = saved.equippedArmor.EquipInfo;

                        pd.Info.Expendables = new List<ConsumableInfo>();
                        foreach (var item in saved.equippedConsumables)
                            if (item.ConsumInfo != null)
                                for (int i = 0; i < item.amount; i++) pd.Info.Expendables.Add(item.ConsumInfo);

                        pd.Info.Items = new List<InventoryItem>(saved.inventory ?? new List<InventoryItem>());

                        Debug.Log($"[Sync] 대기 중인 캐릭터({pd.FinalHeroCode}) 저장된 정보 업데이트 완료");
                    }
                }
            }
        }

        // [수정] 병합 전 호출부(BattleStartBtn/ReadyOrStartButton/QuestVoteSystem)가 기대하는 정적 동기화 진입점을 복구합니다.
        public static void SyncAllAccountsHideoutDataToBattleData()
        {
            foreach (var account in FindObjectsByType<PlayerAccount>(FindObjectsSortMode.None))
                account.SyncAllHideoutDataToBattleData();
        }
    }
}





