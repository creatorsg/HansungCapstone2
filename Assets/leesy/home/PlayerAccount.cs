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

        public static event Action<PlayerAccount> OnLocalAccountReady;
        public static event Action OnCharacterSwitched;
        public static event Action<PlayerAccount> OnAnyAccountReady;

        [SyncVar(hook = nameof(OnCurrentGoldChanged))]
        public int currentGold;

        public event Action<int> OnGoldChanged;

        private void OnCurrentGoldChanged(int oldVal, int newVal)
        {
            if (!isOwned) return;
            OnGoldChanged?.Invoke(newVal);
        }
        public int currentActiveIndex { get; private set; } = -1;

        public CharacterUnit currentSelectedCharacter;

        public List<GameObject> myCharacterPrefabs = new List<GameObject>();
        public List<CharacterData> myCharacterDataList = new List<CharacterData>();

        [SyncVar] public int myCharacterCount = 0;

        public readonly SyncList<int> myHeroPositions = new SyncList<int>();

        public readonly SyncList<string> myHeroCodes = new SyncList<string>();

        private List<PlayerData> _myPlayerDatas = new List<PlayerData>();

        private Dictionary<int, CharacterSaveData> savedCharacterData = new Dictionary<int, CharacterSaveData>();


        public override void OnStartClient()
        {
            base.OnStartClient();
            myHeroCodes.Callback += OnHeroCodesChanged;

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
            if (myHeroCodes.Count > 0 && myHeroCodes.Count == myHeroPositions.Count)
                OnAnyAccountReady?.Invoke(this);
        }

        public override void OnStartLocalPlayer()
        {
            LocalInstance = this;
            Debug.Log("<color=green>[PlayerAccount] 로컬 계정 연결 완료</color>");
            OnLocalAccountReady?.Invoke(this);
            CmdRequestMyCharacters();
        }

        [Command]
        public void CmdRequestMyCharacters()
        {
            if (myCharacterPrefabs == null || myCharacterPrefabs.Count == 0) return;

            _myPlayerDatas.Clear();
            var allPlayerDatas = FindObjectsByType<PlayerData>(FindObjectsSortMode.None);
            Debug.Log($"[PlayerAccount] CmdRequestMyCharacters: 전체 PlayerData {allPlayerDatas.Length}개 발견, 요청 connection={connectionToClient}");
            foreach (var pd in allPlayerDatas)
            {
                Debug.Log($"[PlayerAccount] PlayerData 검사: code={pd.FinalHeroCode}, pd.connectionToClient={pd.connectionToClient}, 일치={pd.connectionToClient == connectionToClient}");
                if (pd.connectionToClient == connectionToClient)
                    _myPlayerDatas.Add(pd);
            }
            _myPlayerDatas.Sort((a, b) => a.FinalHeroPos.CompareTo(b.FinalHeroPos));
            myCharacterCount = _myPlayerDatas.Count;
            Debug.Log($"[PlayerAccount] 내 PlayerData {myCharacterCount}개 수집 완료");

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

            if (_myPlayerDatas.Count > 0)
            {
                activeUnit.SetupFromPlayerData(_myPlayerDatas[0]);
                currentGold = 2000;
                currentActiveIndex = 0;
                savedCharacterData[0] = new CharacterSaveData { gold = currentGold };
                Debug.Log($"[PlayerAccount] PlayerData 기반 초기화: code={_myPlayerDatas[0].FinalHeroCode}, pos={_myPlayerDatas[0].FinalHeroPos}");
            }
            else
            {
                if (myCharacterDataList == null || myCharacterDataList.Count == 0) return;
                Debug.LogWarning("[PlayerAccount] PlayerData가 없어 CharacterData 에셋으로 fallback합니다.");
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

            if (hasPlayerDatas)
            {
                PlayerData targetPd = _myPlayerDatas[targetIndex];
                currentSelectedCharacter.SetupFromPlayerData(targetPd);
                Debug.Log($"<color=cyan>[Server] {targetPd.FinalHeroCode}로 전환 완료!</color>");
            }
            else
            {
                CharacterData targetData = myCharacterDataList[targetIndex];
                currentSelectedCharacter.SetupFromData(targetData);
                Debug.Log($"<color=cyan>[Server] {targetData.charName}로 전환 완료! (fallback)</color>");
            }

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
            Debug.Log($"<color=cyan>[Client] currentActiveIndex 변경: {newActiveIndex}</color>");

            if (InventoryUI.Instance != null)
                InventoryUI.Instance.RefreshInventory();

            if (GoldUI.Instance != null)
                GoldUI.Instance.RefreshGold();

            OnCharacterSwitched?.Invoke();
        }


        [Command]
        public void CmdPurchaseTreeNode(string nodeId)
        {
            Debug.Log($"<color=yellow>[SkillTree] CmdPurchaseTreeNode 진입 - nodeId={nodeId}</color>");

            if (currentSelectedCharacter == null) { Debug.LogWarning("[SkillTree] 거부: currentSelectedCharacter가 null입니다."); return; }
            if (string.IsNullOrEmpty(nodeId)) { Debug.LogWarning("[SkillTree] 거부: nodeId가 비어 있습니다."); return; }

            string characterCode = GetCurrentCharacterCode();
            if (string.IsNullOrEmpty(characterCode))
            {
                Debug.LogWarning("[SkillTree] 거부: characterCode가 비어 있습니다.");
                return;
            }

            if (!SkillTreeRegistry.TryFind(characterCode, nodeId, out SkillTreeNodeSO node))
            {
                Debug.LogWarning($"[SkillTree] 거부: 노드를 찾지 못했습니다. character={characterCode}, node={nodeId}");
                return;
            }

            if (currentSelectedCharacter.unlockedNodeIds.Contains(nodeId))
            {
                Debug.Log($"[SkillTree] 거부: 이미 보유한 노드입니다. node={nodeId}");
                return;
            }

            if (node.prerequisite != null && !currentSelectedCharacter.unlockedNodeIds.Contains(node.prerequisite.nodeId))
            {
                Debug.Log($"[SkillTree] 거부: 선행 노드가 없습니다. node={nodeId}, required={node.prerequisite.nodeId}");
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
                    Debug.Log($"[SkillTree] 거부: 같은 레벨의 다른 분기를 이미 선택했습니다. requested={nodeId}, owned={unlockedNodeId}");
                    return;
                }
            }

            if (currentGold < node.unlockCost)
            {
                Debug.Log($"[SkillTree] 거부: 골드 부족. 필요={node.unlockCost}, 보유={currentGold}");
                return;
            }

            currentGold -= node.unlockCost;
            currentSelectedCharacter.unlockedNodeIds.Add(nodeId);
            Debug.Log($"<color=green>[SkillTree] 구매 성공 - node={nodeId}, 잔액={currentGold}G</color>");
        }

        private string GetCurrentCharacterCode()
        {
            if (currentSelectedCharacter == null) return string.Empty;
            if (!string.IsNullOrEmpty(currentSelectedCharacter.heroCode))
                return currentSelectedCharacter.heroCode;
            return currentSelectedCharacter.characterName;
        }

        [Server]
        private void AutoUnlockLv1Nodes(CharacterUnit unit)
        {
            if (unit == null)
            {
                Debug.LogWarning("[SkillTree] AutoUnlockLv1Nodes: unit이 null입니다.");
                return;
            }

            string code = !string.IsNullOrEmpty(unit.heroCode) ? unit.heroCode : unit.characterName;
            if (string.IsNullOrEmpty(code))
            {
                Debug.LogWarning("[SkillTree] AutoUnlockLv1Nodes: heroCode와 characterName이 모두 비어 있어 스킬트리 매칭이 불가능합니다.");
                return;
            }

            CharacterSkillTreeSO tree = SkillTreeRegistry.GetTree(code);
            if (tree == null || tree.allNodes == null)
            {
                Debug.LogWarning($"[SkillTree] AutoUnlockLv1Nodes: 스킬트리를 찾지 못했습니다. code={code}");
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
            Debug.Log($"<color=cyan>[SkillTree] AutoUnlock: code={code}, Lv1 노드 {added}개 추가 (총 unlocked={unit.unlockedNodeIds.Count})</color>");
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

        /// <summary>
        /// Home → Battle 씬 전환 직전에 호출.
        /// 각 캐릭터의 unlockedNodeIds를 대응하는 PlayerData에 스냅샷 복사합니다.
        /// </summary>
        [Server]
        public void SnapshotUpgradesToPlayerData()
        {
            if (_myPlayerDatas == null || _myPlayerDatas.Count == 0)
            {
                Debug.LogWarning("[PlayerAccount] SnapshotUpgrades: _myPlayerDatas 비어있음");
                return;
            }

            for (int i = 0; i < _myPlayerDatas.Count; i++)
            {
                PlayerData pd = _myPlayerDatas[i];
                if (pd == null) continue;

                pd.unlockedNodeIds.Clear();
                pd.SelectedWeaponId = "";
                pd.PurchasedWeaponNodeCount = 0;

                if (i == currentActiveIndex && currentSelectedCharacter != null)
                {
                    foreach (string nodeId in currentSelectedCharacter.unlockedNodeIds)
                        pd.unlockedNodeIds.Add(nodeId);

                    pd.SelectedWeaponId = currentSelectedCharacter.selectedWeaponId;
                    pd.PurchasedWeaponNodeCount = currentSelectedCharacter.purchasedNodeCount;
                }
                else if (savedCharacterData.TryGetValue(i, out CharacterSaveData saved))
                {
                    foreach (string nodeId in saved.unlockedNodeIds)
                        pd.unlockedNodeIds.Add(nodeId);

                    pd.SelectedWeaponId = saved.selectedWeaponId;
                    pd.PurchasedWeaponNodeCount = saved.purchasedNodeCount;
                }

                Debug.Log($"[PlayerAccount] Snapshot: PlayerData[{i}] code={pd.FinalHeroCode}, nodes={pd.unlockedNodeIds.Count}개, weapon={pd.SelectedWeaponId}, weaponNodes={pd.PurchasedWeaponNodeCount}");
            }
        }

        /// <summary>
        /// 모든 PlayerAccount에서 SnapshotUpgradesToPlayerData를 호출합니다.
        /// GameRoomManager.OnServerChangeScene에서 사용합니다.
        /// </summary>
        [Server]
        public static void SnapshotAllUpgrades()
        {
            var accounts = FindObjectsByType<PlayerAccount>(FindObjectsSortMode.None);
            foreach (var account in accounts)
                account.SnapshotUpgradesToPlayerData();
        }
    }
}





