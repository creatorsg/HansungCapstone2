using Jun;
using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEngine.InputSystem;
#endif

namespace Lsy
{
    public class CharacterSaveData
    {
        public List<InventoryItem> inventory = new List<InventoryItem>();
        public string selectedWeaponId = "";
        public int purchasedNodeCount = 0;
        public Dictionary<string, int> blacksmithWeaponLevels = new Dictionary<string, int>();
        public List<PlayerSkill> skills = new List<PlayerSkill>();
        public List<string> unlockedNodeIds = new List<string>();
        public int gold = 0;

            //--- ĳͺ ʵ ߰ ---
        public string equippedWeaponId = "";
        public string equippedArmorId = "";
        public InventoryItem equippedWeapon; //  ü  
        public InventoryItem equippedArmor;  //  ü  
        public List<InventoryItem> equippedConsumables = new List<InventoryItem>(); // Һ ü Ʈ 
        public int uniqueTraitLevel = 0;
    }

    public class PlayerAccount : NetworkBehaviour
    {
        public static PlayerAccount LocalInstance;

 /// <summary>로컬 PlayerAccount가 준비됐GoldUI 초기용</summary>
        public static event Action<PlayerAccount> OnLocalAccountReady;
        public static event Action OnCharacterSwitched;

 /// <summary>모든 PlayerAccount(로컬+격)가 myHeroCodes가 채워발생 초상체 로드/summary>
        public static event Action<PlayerAccount> OnAnyAccountReady;

            //골드 (레어 귀 ?
        [SyncVar(hook = nameof(OnCurrentGoldChanged))]
        public int currentGold;

        public event Action<int> OnGoldChanged;

        private void OnCurrentGoldChanged(int oldVal, int newVal)
        {
            if (!isOwned) return;
            OnGoldChanged?.Invoke(newVal);
        }

        // ───────── [임시 치트: 테스트용 골드 지급] 배포 전 삭제 ─────────
        // 에디터에서만 동작. G키 = 골드 +10000, Shift+G = +100000
#if UNITY_EDITOR
        private void Update()
        {
            if (!isOwned) return;
            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.gKey.wasPressedThisFrame)
            {
                int amount = (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed) ? 100000 : 10000;
                CmdCheatAddGold(amount);
            }
        }
#endif

        [Command]
        public void CmdCheatAddGold(int amount)
        {
            currentGold += amount;
            Debug.Log($"<color=magenta>[CHEAT] 골드 +{amount} → 현재 {currentGold}G</color>");
        }
        // ───────── [임시 치트 끝] ─────────

            //캐릭관
        public int currentActiveIndex { get; private set; } = -1;

        public CharacterUnit currentSelectedCharacter;

        public List<GameObject> myCharacterPrefabs = new List<GameObject>();
        public List<CharacterData> myCharacterDataList = new List<CharacterData>();

        [SyncVar] public int myCharacterCount = 0;

 /// <summary> 조종는 캐릭의 heroPos 목록 초상유단/summary>
        public readonly SyncList<int> myHeroPositions = new SyncList<int>();

 /// <summary>myHeroPositions 1:1 하heroCode 목록 초상지 시/summary>
        public readonly SyncList<string> myHeroCodes = new SyncList<string>();

            //버 용: ?connection택PlayerData 목록 (FinalHeroPos 렬)
        private List<PlayerData> _myPlayerDatas = new List<PlayerData>();

        private Dictionary<int, CharacterSaveData> savedCharacterData = new Dictionary<int, CharacterSaveData>(); public Dictionary<int, CharacterSaveData> SavedCharacterData => savedCharacterData;

            //초기

        public override void OnStartClient()
        {
            base.OnStartClient();
            //myHeroCodes가 중채워 비해 콜백 록
            myHeroCodes.Callback += OnHeroCodesChanged;

            //버서 이 으(진 즉시 발동
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
            //최초 이 전어점(myHeroPositions기 치)벤발생
            if (myHeroCodes.Count > 0 && myHeroCodes.Count == myHeroPositions.Count)
                OnAnyAccountReady?.Invoke(this);
        }

        public override void OnStartLocalPlayer()
        {
            LocalInstance = this;
 Debug.Log("<color=green>[계정] 속 공!</color>");
            OnLocalAccountReady?.Invoke(this);
            CmdRequestMyCharacters();
        }

        [Command]
        public void CmdRequestMyCharacters()
        {
            if (myCharacterPrefabs == null || myCharacterPrefabs.Count == 0) return;

            //?connection한 모든 PlayerData집 (FinalHeroPos 렬)
            _myPlayerDatas.Clear();
            var allPlayerDatas = FindObjectsByType<PlayerData>(FindObjectsSortMode.None);
 Debug.Log($"[PlayerAccount] CmdRequestMyCharacters: 체 PlayerData {allPlayerDatas.Length}발견, connectionToClient={connectionToClient}");
            foreach (var pd in allPlayerDatas)
            {
 Debug.Log($"[PlayerAccount] PlayerData 검 code={pd.FinalHeroCode}, pd.connectionToClient={pd.connectionToClient}, 치={pd.connectionToClient == connectionToClient}");
                if (pd.connectionToClient == connectionToClient)
                    _myPlayerDatas.Add(pd);
            }
            _myPlayerDatas.Sort((a, b) => a.FinalHeroPos.CompareTo(b.FinalHeroPos));
            myCharacterCount = _myPlayerDatas.Count;
 Debug.Log($"[PlayerAccount] PlayerData {myCharacterCount}집 료");

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

            //?Spawn 에 Setup 폰 메시지heroPos/heroCode가 바르게 함
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

            //Ҹǰ Ե ʱ 忡 
                var initSlot = activeUnit.equipmentSlot;
                if (initSlot != null)
                {
                    foreach (var item in initSlot.equippedConsumables)
                        initSave.equippedConsumables.Add(item);
                }

                savedCharacterData[0] = initSave;
                Debug.Log($"[PlayerAccount] PlayerData 기반 초기 code={_myPlayerDatas[0].FinalHeroCode}, pos={_myPlayerDatas[0].FinalHeroPos}");
            }
            else
            {
                if (myCharacterDataList == null || myCharacterDataList.Count == 0) return;
 Debug.LogWarning("[PlayerAccount] PlayerData 음 CharacterData 셋로 백니");
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

            //캐릭환 ?

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

            //ĳ ¸ ޸𸮿 
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
                    backup.uniqueTraitLevel = currentSelectedCharacter.uniqueTraitLevel;
                }

                savedCharacterData[currentActiveIndex] = backup;
            }

            currentSelectedCharacter.myInventory.Clear();
            currentSelectedCharacter.mySkills.Clear();
            currentSelectedCharacter.unlockedNodeIds.Clear();
            currentSelectedCharacter.selectedWeaponId = "";
            currentSelectedCharacter.purchasedNodeCount = 0;
            currentSelectedCharacter.blacksmithWeaponLevels.Clear();
            currentSelectedCharacter.uniqueTraitLevel = 0;

            var targetSlot = currentSelectedCharacter.equipmentSlot;
            if (targetSlot != null)
            {
                targetSlot.equippedWeaponId = "";
                targetSlot.equippedArmorId = "";
                targetSlot.equippedWeapon = default;
                targetSlot.equippedArmor = default;
                targetSlot.equippedConsumables.Clear();
            }

            //ε ȯ ε
            currentActiveIndex = targetIndex;

            if (savedCharacterData.TryGetValue(targetIndex, out CharacterSaveData saved))
            {
            //̹ ÷ ִ ĳ: 
                if (hasPlayerDatas) currentSelectedCharacter.SetupFromPlayerData(_myPlayerDatas[targetIndex]);
                else currentSelectedCharacter.SetupFromData(myCharacterDataList[targetIndex]);

            //Setup  ⺻ ۵ · ü
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
                currentSelectedCharacter.uniqueTraitLevel = saved.uniqueTraitLevel;

                if (targetSlot != null)
                {
                    targetSlot.equippedWeaponId = saved.equippedWeaponId;
                    targetSlot.equippedWeapon = saved.equippedWeapon;
                    targetSlot.equippedArmorId = saved.equippedArmorId;
                    targetSlot.equippedArmor = saved.equippedArmor;
                    targetSlot.equippedConsumables.Clear();
                    foreach (var item in saved.equippedConsumables) targetSlot.equippedConsumables.Add(item);
                }
            }
            else
            {
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
            }

            //UI 뺸
            AutoUnlockLv1Nodes(currentSelectedCharacter);
            TargetRpcRefreshUI(connectionToClient, targetIndex);
        }


        [TargetRpc]
        private void TargetRpcRefreshUI(NetworkConnection target, int newActiveIndex)
        {
            currentActiveIndex = newActiveIndex;
            Debug.Log($"<color=cyan>[로컬 currentActiveIndex: {newActiveIndex}</color>");

            if (InventoryUI.Instance != null)
                InventoryUI.Instance.RefreshInventory();

            if (GoldUI.Instance != null)
                GoldUI.Instance.RefreshGold();

            // myHeroPositions가 채워진 뒤에 OnCharacterSwitched 발화
            // CmdRequestMyCharacters에서 보낸 SyncList 메시지가 TargetRpc보다
            // 늦게 도착하는 경우 초상화가 어둡게 초기화되는 타이밍 버그 방지
            StartCoroutine(WaitForHeroDataThenRefresh());
        }

        private System.Collections.IEnumerator WaitForHeroDataThenRefresh()
        {
            float elapsed = 0f;
            const float maxWait = 5f;

            // myHeroPositions가 채워질 때까지 대기 (최대 5초)
            while (myHeroPositions.Count == 0 && elapsed < maxWait)
            {
                yield return null;
                elapsed += Time.deltaTime;
            }

            if (myHeroPositions.Count == 0)
                Debug.LogWarning("[PlayerAccount] WaitForHeroDataThenRefresh: myHeroPositions 타임아웃 — 강제 갱신 진행");

            OnCharacterSwitched?.Invoke();
        }

            //NPC 호용 커맨(골드 체크/차감 당) ?

        [Command]
        public void CmdPurchaseTreeNode(string nodeId)
        {
            Debug.Log($"<color=yellow>[SkillTree] CmdPurchaseTreeNode 진입 nodeId={nodeId}</color>");

 if (currentSelectedCharacter == null) { Debug.LogWarning("[SkillTree] 거: currentSelectedCharacter null"); return; }
 if (string.IsNullOrEmpty(nodeId)) { Debug.LogWarning("[SkillTree] 거: nodeId 비어음"); return; }

            string characterCode = GetCurrentCharacterCode();
            if (string.IsNullOrEmpty(characterCode))
            {
 Debug.LogWarning("[SkillTree] 거: characterCode 비어음");
                return;
            }

            if (!SkillTreeRegistry.TryFind(characterCode, nodeId, out SkillTreeNodeSO node))
            {
 Debug.LogWarning($"[SkillTree] 거: 드 찾음 character={characterCode}, node={nodeId}");
                return;
            }

            if (currentSelectedCharacter.unlockedNodeIds.Contains(nodeId))
            {
 Debug.Log($"[SkillTree] 거: 보유 {nodeId}");
                return;
            }

            if (node.prerequisite != null && !currentSelectedCharacter.unlockedNodeIds.Contains(node.prerequisite.nodeId))
            {
 Debug.Log($"[SkillTree] 거: prereq 미충{nodeId} requires {node.prerequisite.nodeId}");
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
 Debug.Log($"[SkillTree] 거: 분기 자일 {nodeId} vs 보유 {unlockedNodeId}");
                    return;
                }
            }

            if (currentGold < node.unlockCost)
            {
 Debug.Log($"[SkillTree] 거: 골드 부요 {node.unlockCost}, 보유 {currentGold}");
                return;
            }

            currentGold -= node.unlockCost;

            currentSelectedCharacter.unlockedNodeIds.Add(nodeId);
 Debug.Log($"<color=green>[SkillTree] 구매 공 {nodeId}, 액 {currentGold}G</color>");
        }

        private string GetCurrentCharacterCode()
        {
            if (currentSelectedCharacter == null) return string.Empty;
            if (!string.IsNullOrEmpty(currentSelectedCharacter.heroCode))
                return currentSelectedCharacter.heroCode;
            return currentSelectedCharacter.characterName;
        }

        /// <summary>
 /// 캐릭의 Lv1 드 4기본 킬)?unlockedNodeIds동 추니
 /// 캐릭초기 폰 직후, 그리캐릭로 왑직후(백업 을 출세
 /// SyncHashSet.Add 멱등중복 출어전니
        /// </summary>
        [Server]
        private void AutoUnlockLv1Nodes(CharacterUnit unit)
        {
            if (unit == null)
            {
                Debug.LogWarning("[SkillTree] AutoUnlockLv1Nodes: unitnull");
                return;
            }

            string code = !string.IsNullOrEmpty(unit.heroCode) ? unit.heroCode : unit.characterName;
            if (string.IsNullOrEmpty(code))
            {
 Debug.LogWarning("[SkillTree] AutoUnlockLv1Nodes: heroCode/characterName 모두 비어음 킬리 매칭 불");
                return;
            }

            CharacterSkillTreeSO tree = SkillTreeRegistry.GetTree(code);
            if (tree == null || tree.allNodes == null)
            {
 Debug.LogWarning($"[SkillTree] AutoUnlockLv1Nodes: tree 음 code={code}");
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
 Debug.Log($"<color=cyan>[SkillTree] AutoUnlock: code={code}, Lv1 드 {added}추 (unlocked={unit.unlockedNodeIds.Count})</color>");
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

            //퍼 ?

        /// <summary>
 /// DontDestroyOnLoad는 PlayerData ?connection유것을 반환니
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


            //Ѿ κ丮 Ʈ

        /// <summary>
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
                Debug.Log($"[SyncInv] {pd.FinalHeroCode} Items={pd.Info.Items.Count}");
                break;
            }
        }

        public void SyncAllHideoutDataToBattleData()
        {
            PlayerData[] allPlayerDatas = FindObjectsByType<PlayerData>(FindObjectsSortMode.None);
            if (allPlayerDatas != null) Debug.Log("[SyncAllHideout] found PlayerDatas");
            foreach (var pd in allPlayerDatas)
            {
            //connection PlayerData Ȯ
                if (pd.connectionToClient != connectionToClient) continue;

            //pd ĳ Ʈ(_myPlayerDatas) ° ĳ Ȯ
                int charIndex = _myPlayerDatas.IndexOf(pd);
                if (charIndex == -1) continue;

            //3. ( ִ ĳ vs )
                if (charIndex == currentActiveIndex && currentSelectedCharacter != null)
                {
                    var slot = currentSelectedCharacter.equipmentSlot;
                    if (slot != null && pd.Info != null)
                    {
            ///
                        pd.Info.Weapon = !string.IsNullOrEmpty(slot.equippedWeaponId) ? slot.equippedWeapon.EquipInfo : null;
                        pd.Info.Armor  = !string.IsNullOrEmpty(slot.equippedArmorId)  ? slot.equippedArmor.EquipInfo  : null;

            //Ҹǰ Expendables
                        pd.Info.Expendables = new List<ConsumableInfo>();
                        foreach (var item in slot.equippedConsumables)
                            if (item.ConsumInfo != null)
                            {
                                ConsumableInfo clone = item.ConsumInfo.Clone();
                                clone.amount = item.amount;  
                                pd.Info.Expendables.Add(clone);
                            }

            //κ Items
                        pd.Info.Items = new List<InventoryItem>(currentSelectedCharacter.myInventory);
                        pd.Info.Gold = currentGold;

                        SyncSkillUpgradesToPlayerData(pd, currentSelectedCharacter.mySkills, "active");

 Debug.Log($"[Sync] ĳ({pd.FinalHeroCode}) ǽð Ʈ Ϸ");
                    }
                }
                else if (savedCharacterData.TryGetValue(charIndex, out var saved))
                {
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
                        SyncSkillUpgradesToPlayerData(pd, saved.skills, "saved");

 Debug.Log($"[Sync] ĳ({pd.FinalHeroCode}) Ʈ Ϸ");
                    }
                }
            }
        }

        [Server]
        public bool SyncCurrentCharacterSkillUpgradesToPlayerData()
        {
            if (currentActiveIndex < 0 || currentSelectedCharacter == null)
            {
                Debug.LogWarning("[SkillBridge] immediate sync skipped: current character is not ready.");
                return false;
            }

            foreach (var pd in FindObjectsByType<PlayerData>(FindObjectsSortMode.None))
            {
                if (pd.connectionToClient != connectionToClient) continue;

                int charIndex = ResolveCharacterIndex(pd);
                if (charIndex != currentActiveIndex) continue;

                return SyncSkillUpgradesToPlayerData(pd, currentSelectedCharacter.mySkills, "immediate");
            }

            Debug.LogWarning($"[SkillBridge] immediate sync failed: PlayerData not found. index={currentActiveIndex}, hero={currentSelectedCharacter.heroCode}");
            return false;
        }

        public static bool SyncSkillUpgradesToPlayerData(PlayerData pd, IEnumerable<PlayerSkill> skills, string source)
        {
            if (pd == null || pd.Info == null) return false;

            List<Jun.SkillInfo> upgradedSkills = BuildUpgradedSkills(pd.FinalHeroCode, skills);
            if (upgradedSkills == null) return false;

            Jun.PlayerInfo info = ClonePlayerInfo(pd.Info);
            info.Skills = upgradedSkills;
            pd.Info = info;

            int skillCount = 0;
            if (skills != null)
                foreach (var _ in skills) skillCount++;

            Debug.Log($"[SkillBridge] {source}: hero={pd.FinalHeroCode}, savedSkills={skillCount}, applied={pd.Info.Skills.Count}");
            for (int i = 0; i < pd.Info.Skills.Count; i++)
            {
                Jun.SkillInfo skill = pd.Info.Skills[i];
                int level = GetSkillLevel(skills, i);
                Debug.Log($"[SkillBridge]   idx={i} Lv={level} name={skill?.Name} " +
                          $"Dmg={skill?.DamageRate} Heal={skill?.HealRate} " +
                          $"Eff={skill?.EffectValue}/{skill?.EffectDuration} Target={skill?.Target} EffType={skill?.EffectType}");
            }
            return true;
        }

        [Server]
        private int ResolveCharacterIndex(PlayerData pd)
        {
            if (pd == null) return -1;

            if (_myPlayerDatas != null)
            {
                int index = _myPlayerDatas.IndexOf(pd);
                if (index >= 0) return index;
            }

            for (int i = 0; i < myHeroPositions.Count; i++)
            {
                if (myHeroPositions[i] == pd.FinalHeroPos)
                    return i;
            }

            return -1;
        }

        // ───────── [정보상 일원화 브릿지] 강화 스킬 빌드 (순수함수) ─────────

        /// <summary>
        /// base 스킬(CharacterRegistry, 불변 원본)을 요소별 deep clone한 뒤,
        /// mySkills의 강화 레벨에 따라 SkillUpgradeData 값으로 덮어쓴 "새 리스트"를 반환한다.
        /// - 순수함수: Entry.Skills 원본을 절대 오염시키지 않음.
        /// - idempotent: 항상 base에서 재출발하므로 여러 번 호출해도 결과 동일.
        /// - base를 못 찾으면 null 반환 → 호출부는 기존 Info.Skills를 유지.
        /// 사용 enum/수치 필드는 Jun.SkillInfo(GameData.cs:203)만 대상으로 한다.
        /// </summary>
        public static List<Jun.SkillInfo> BuildUpgradedSkills(string heroCode, IEnumerable<PlayerSkill> mySkills)
        {
            if (!CharacterRegistry.TryGet(heroCode, out var entry) || entry.Skills == null)
            {
                Debug.LogWarning($"[SkillBridge] CharacterRegistry has no base skills for {heroCode} -> upgrade skipped");
                return null;
            }

            var result = new List<Jun.SkillInfo>(entry.Skills.Count);
            for (int i = 0; i < entry.Skills.Count; i++)
            {
                Jun.SkillInfo clone = CloneSkillInfo(entry.Skills[i]);
                int level = GetSkillLevel(mySkills, i); // skillIndex = i (Info.Skills 순서와 1:1)
                if (level >= 2 && clone != null)
                    ApplyUpgradeToSkill(clone, heroCode, i, level);
                result.Add(clone);
            }
            return result;
        }

        private static int GetSkillLevel(IEnumerable<PlayerSkill> mySkills, int skillIndex)
        {
            if (mySkills == null) return 1;
            foreach (var ps in mySkills)
                if (ps.skillIndex == skillIndex)
                    return ps.currentLevel;
            return 1; // mySkills에 없으면 Lv1
        }

        /// <summary>
        /// Lv2부터 현재 레벨까지 누적 적용. 각 필드는 "절대값"이라 상위 레벨이 같은 필드를 다시 지정하면 교체.
        /// 수치 0=무시(직전 상태 유지), enum은 use*Override=true 일 때만 덮어쓴다.
        /// </summary>
        private static void ApplyUpgradeToSkill(Jun.SkillInfo skill, string heroCode, int skillIndex, int level)
        {
            for (int L = 2; L <= level; L++)
            {
                if (!SkillUpgradeRegistry.TryGet(heroCode, skillIndex, L, out var node))
                {
                    Debug.LogWarning($"[SkillBridge] 강화 노드 없음: code={heroCode}, idx={skillIndex}, lv={L} → 해당 레벨 스킵");
                    continue;
                }

                if (node.DamageRate     != 0f) skill.DamageRate     = node.DamageRate;
                if (node.HealRate       != 0f) skill.HealRate       = node.HealRate;
                if (node.EffectValue    != 0f) skill.EffectValue    = node.EffectValue;
                if (node.EffectDuration != 0)  skill.EffectDuration = node.EffectDuration;
                if (!string.IsNullOrWhiteSpace(node.skillName)) skill.Name = node.skillName;
                if (!string.IsNullOrWhiteSpace(node.skillDescription)) skill.description = node.skillDescription;

                if (node.useTargetOverride) skill.Target     = node.Target;
                if (node.useEffectOverride) skill.EffectType = node.EffectType;
            }
        }

        public static Jun.PlayerInfo ClonePlayerInfo(Jun.PlayerInfo source)
        {
            if (source == null) return null;

            return new Jun.PlayerInfo
            {
                Id = source.Id,
                Name = source.Name,
                Type = source.Type,
                Skills = CloneSkillList(source.Skills),
                Items = source.Items != null ? new List<InventoryItem>(source.Items) : null,
                Expendables = CloneConsumableList(source.Expendables),
                Lvl = source.Lvl,
                Exp = source.Exp,
                Gold = source.Gold,
                Hp = source.Hp,
                MaxHp = source.MaxHp,
                San = source.San,
                MaxSan = source.MaxSan,
                Atk = source.Atk,
                Def = source.Def,
                Spd = source.Spd,
                Crit = source.Crit,
                Ctm = source.Ctm,
                Dodge = source.Dodge,
                Acc = source.Acc,
                Res = source.Res,
                Weapon = source.Weapon,
                Armor = source.Armor,
                Trk1 = source.Trk1,
                Trk2 = source.Trk2,
                UniqueTraitLv = source.UniqueTraitLv,
                Statuses = CloneStatusList(source.Statuses),
            };
        }

        private static List<Jun.SkillInfo> CloneSkillList(List<Jun.SkillInfo> source)
        {
            if (source == null) return null;

            var result = new List<Jun.SkillInfo>(source.Count);
            foreach (var skill in source)
                result.Add(CloneSkillInfo(skill));
            return result;
        }

        private static List<ConsumableInfo> CloneConsumableList(List<ConsumableInfo> source)
        {
            if (source == null) return null;

            var result = new List<ConsumableInfo>(source.Count);
            foreach (var item in source)
                result.Add(item != null ? item.Clone() : null);
            return result;
        }

        private static List<Jun.ActiveStatus> CloneStatusList(List<Jun.ActiveStatus> source)
        {
            if (source == null) return null;

            var result = new List<Jun.ActiveStatus>(source.Count);
            foreach (var status in source)
            {
                if (status == null)
                {
                    result.Add(null);
                    continue;
                }

                result.Add(new Jun.ActiveStatus
                {
                    Type = status.Type,
                    RemainingTurns = status.RemainingTurns,
                    Value = status.Value,
                });
            }
            return result;
        }

        /// <summary>Jun.SkillInfo 요소별 복제. 값/문자열은 복사, 참조 필드(icon/StatusEffects)는 브릿지가 안 건드리므로 참조 공유.</summary>
        private static Jun.SkillInfo CloneSkillInfo(Jun.SkillInfo s)
        {
            if (s == null) return null;
            return new Jun.SkillInfo
            {
                Name             = s.Name,
                User             = s.User,
                Type             = s.Type,
                anim             = s.anim,
                TagetNum         = s.TagetNum,
                icon             = s.icon,
                description      = s.description,
                DamageRate       = s.DamageRate,
                HealRate         = s.HealRate,
                EffectType       = s.EffectType,
                EffectValue      = s.EffectValue,
                EffectDuration   = s.EffectDuration,
                Target           = s.Target,
                DamageMultiplier = s.DamageMultiplier,
                HealAmount       = s.HealAmount,
                StatusEffects    = s.StatusEffects,
            };
        }

        public static void SyncAllAccountsHideoutDataToBattleData()
        {
            foreach (var account in FindObjectsByType<PlayerAccount>(FindObjectsSortMode.None))
                account.SyncAllHideoutDataToBattleData();
        }

     
    }
}





