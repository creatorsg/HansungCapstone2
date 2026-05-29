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
        // [����] �������� ���� ��ȭ �ܰ踦 ĳ���ͺ�/���⺰�� �����մϴ�.
        public Dictionary<string, int> blacksmithWeaponLevels = new Dictionary<string, int>();
        public List<PlayerSkill> skills = new List<PlayerSkill>();
        public List<string> unlockedNodeIds = new List<string>();
        public int gold = 0;

        // --- ĳ���ͺ� ���� ���� ������ ���� �ʵ� �߰� ---
        public string equippedWeaponId = "";
        public string equippedArmorId = "";
        public InventoryItem equippedWeapon; // ���� ��ü ���� ���
        public InventoryItem equippedArmor;  // �� ��ü ���� ���
        public List<InventoryItem> equippedConsumables = new List<InventoryItem>(); // �Һ��� ��ü ����Ʈ ���
    }

    public class PlayerAccount : NetworkBehaviour
    {
        public static PlayerAccount LocalInstance;

        /// <summary>로컬 PlayerAccount가 준비됐??????GoldUI ??초기?�용</summary>
        public static event Action<PlayerAccount> OnLocalAccountReady;
        public static event Action OnCharacterSwitched;

        /// <summary>모든 PlayerAccount(로컬+?�격)가 myHeroCodes가 채워�???발생 ??초상???�체 로드??/summary>
        public static event Action<PlayerAccount> OnAnyAccountReady;

        // ?�?�?� 골드 (?�레?�어 귀?? ?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�
        [SyncVar(hook = nameof(OnCurrentGoldChanged))]
        public int currentGold;

        public event Action<int> OnGoldChanged;

        private void OnCurrentGoldChanged(int oldVal, int newVal)
        {
            if (!isOwned) return;
            OnGoldChanged?.Invoke(newVal);
        }

        // ?�?�?� 캐릭??관�??�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�
        public int currentActiveIndex { get; private set; } = -1;

        public CharacterUnit currentSelectedCharacter;

        public List<GameObject> myCharacterPrefabs = new List<GameObject>();
        public List<CharacterData> myCharacterDataList = new List<CharacterData>();

        [SyncVar] public int myCharacterCount = 0;

        /// <summary>?��? 조종?�는 캐릭?�의 heroPos 목록 ??초상???�유�??�단??/summary>
        public readonly SyncList<int> myHeroPositions = new SyncList<int>();

        /// <summary>myHeroPositions?� 1:1 ?�?�하??heroCode 목록 ??초상???��?지 ?�시??/summary>
        public readonly SyncList<string> myHeroCodes = new SyncList<string>();

        // ?�버 ?�용: ??connection???�택??PlayerData 목록 (FinalHeroPos ???�렬)
        private List<PlayerData> _myPlayerDatas = new List<PlayerData>();

        private Dictionary<int, CharacterSaveData> savedCharacterData = new Dictionary<int, CharacterSaveData>(); public Dictionary<int, CharacterSaveData> SavedCharacterData => savedCharacterData;

        // ?�?�?� 초기???�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�

        public override void OnStartClient()
        {
            base.OnStartClient();
            // myHeroCodes가 ?�중??채워�??��? ?�비해 콜백 ?�록
            myHeroCodes.Callback += OnHeroCodesChanged;

            // ?�버?�서 ?��? ?�이?��? ?�으�?(???�진???? 즉시 발동
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
            // 최초 ?�이?��? ?�전???�어???�점(myHeroPositions�??�기 ?�치)???�벤??발생
            if (myHeroCodes.Count > 0 && myHeroCodes.Count == myHeroPositions.Count)
                OnAnyAccountReady?.Invoke(this);
        }

        public override void OnStartLocalPlayer()
        {
            LocalInstance = this;
            Debug.Log("<color=green>[계정] ?�속 ?�공!</color>");
            OnLocalAccountReady?.Invoke(this);
            CmdRequestMyCharacters();
        }

        [Command]
        public void CmdRequestMyCharacters()
        {
            if (myCharacterPrefabs == null || myCharacterPrefabs.Count == 0) return;

            // ??connection???�한 모든 PlayerData�??�집 (FinalHeroPos ???�렬)
            _myPlayerDatas.Clear();
            var allPlayerDatas = FindObjectsByType<PlayerData>(FindObjectsSortMode.None);
            Debug.Log($"[PlayerAccount] CmdRequestMyCharacters: ?�체 PlayerData {allPlayerDatas.Length}�?발견, ??connectionToClient={connectionToClient}");
            foreach (var pd in allPlayerDatas)
            {
                Debug.Log($"[PlayerAccount] PlayerData 검?? code={pd.FinalHeroCode}, pd.connectionToClient={pd.connectionToClient}, ?�치={pd.connectionToClient == connectionToClient}");
                if (pd.connectionToClient == connectionToClient)
                    _myPlayerDatas.Add(pd);
            }
            _myPlayerDatas.Sort((a, b) => a.FinalHeroPos.CompareTo(b.FinalHeroPos));
            myCharacterCount = _myPlayerDatas.Count;
            Debug.Log($"[PlayerAccount] ??PlayerData {myCharacterCount}�??�집 ?�료");

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

            // ??Spawn ?�에 Setup ???�폰 메시지??heroPos/heroCode가 ?�바르게 ?�함??
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
                savedCharacterData[0] = initSave;
                Debug.Log($"[PlayerAccount] PlayerData 기반 초기?? code={_myPlayerDatas[0].FinalHeroCode}, pos={_myPlayerDatas[0].FinalHeroPos}");
            }
            else
            {
                if (myCharacterDataList == null || myCharacterDataList.Count == 0) return;
                Debug.LogWarning("[PlayerAccount] PlayerData ?�음 ??CharacterData ?�셋?�로 ?�백?�니??");
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

        // ?�?�?� 캐릭???�환 ?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�

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

            // ���� ĳ������ ���¸� ���� �޸𸮿� ���
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

            // ĳ���� ������Ʈ �ʱ�ȭ (�� �����͸� �ޱ� ���� ����)
            // ���� ĳ������ �����Ͱ� �������� �ʵ��� "����" ���ϴ�.
            currentSelectedCharacter.myInventory.Clear();
            currentSelectedCharacter.mySkills.Clear();
            currentSelectedCharacter.unlockedNodeIds.Clear();
            // [����] ĳ���ͺ� �������� ���� ��ȭ ���°� �ٸ� ĳ���ͷ� �������� �ʵ��� ���� ���ϴ�.
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

            // �ε��� ��ȯ �� ������ �ε�
            currentActiveIndex = targetIndex;

            // ����� �����Ͱ� �ִ��� ���� Ȯ���մϴ�.
            if (savedCharacterData.TryGetValue(targetIndex, out CharacterSaveData saved))
            {
                // �̹� �÷����� ���� �ִ� ĳ����: ����� ������ ����
                // �̶��� SetupFromData�� ȣ������ �ʰų�, ȣ�� �� ����� �����ͷ� ����ϴ�.
                if (hasPlayerDatas) currentSelectedCharacter.SetupFromPlayerData(_myPlayerDatas[targetIndex]);
                else currentSelectedCharacter.SetupFromData(myCharacterDataList[targetIndex]);

                // Setup �������� �� �⺻ �����۵��� ����� ����� ���·� ��ü
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
                    // [����] ����� Ȩ ���� �Ҹ�ǰ�� �����մϴ�.
                    targetSlot.equippedConsumables.Clear();
                    foreach (var item in saved.equippedConsumables) targetSlot.equippedConsumables.Add(item);
                }
            }
            else
            {
                // ó�� �����ϴ� ĳ����: �⺻ ������(�ʱ� ������ ��) �ε�
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
                // SetupFromData ���ο��� �κ��丮�� ��ų�� ä�����Ƿ� �״�� �Ӵϴ�.
            }

            //  ���� ���� �� UI �뺸
            AutoUnlockLv1Nodes(currentSelectedCharacter);
            TargetRpcRefreshUI(connectionToClient, targetIndex);
        }


        [TargetRpc]
        private void TargetRpcRefreshUI(NetworkConnection target, int newActiveIndex)
        {
            currentActiveIndex = newActiveIndex;
            Debug.Log($"<color=cyan>[?�라?�언?? currentActiveIndex: {newActiveIndex}</color>");

            if (InventoryUI.Instance != null)
                InventoryUI.Instance.RefreshInventory();

            if (GoldUI.Instance != null)
                GoldUI.Instance.RefreshGold();

            OnCharacterSwitched?.Invoke();
        }

        // ?�?�?� NPC ?�호?�용 커맨??(골드 체크/차감 ?�당) ?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�

        [Command]
        public void CmdPurchaseTreeNode(string nodeId)
        {
            Debug.Log($"<color=yellow>[SkillTree] CmdPurchaseTreeNode 진입 ??nodeId={nodeId}</color>");

            if (currentSelectedCharacter == null) { Debug.LogWarning("[SkillTree] 거�?: currentSelectedCharacter null"); return; }
            if (string.IsNullOrEmpty(nodeId)) { Debug.LogWarning("[SkillTree] 거�?: nodeId 비어?�음"); return; }

            string characterCode = GetCurrentCharacterCode();
            if (string.IsNullOrEmpty(characterCode))
            {
                Debug.LogWarning("[SkillTree] 거�?: characterCode 비어?�음");
                return;
            }

            if (!SkillTreeRegistry.TryFind(characterCode, nodeId, out SkillTreeNodeSO node))
            {
                Debug.LogWarning($"[SkillTree] 거�?: ?�드 �?찾음 ??character={characterCode}, node={nodeId}");
                return;
            }

            if (currentSelectedCharacter.unlockedNodeIds.Contains(nodeId))
            {
                Debug.Log($"[SkillTree] 거�?: ?��? 보유 ??{nodeId}");
                return;
            }

            if (node.prerequisite != null && !currentSelectedCharacter.unlockedNodeIds.Contains(node.prerequisite.nodeId))
            {
                Debug.Log($"[SkillTree] 거�?: prereq 미충�???{nodeId} requires {node.prerequisite.nodeId}");
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
                    Debug.Log($"[SkillTree] 거�?: 분기 ?�자?�일 ??{nodeId} vs ?��? 보유 {unlockedNodeId}");
                    return;
                }
            }

            if (currentGold < node.unlockCost)
            {
                Debug.Log($"[SkillTree] 거�?: 골드 부�????�요 {node.unlockCost}, 보유 {currentGold}");
                return;
            }

            currentGold -= node.unlockCost;

            currentSelectedCharacter.unlockedNodeIds.Add(nodeId);
            Debug.Log($"<color=green>[SkillTree] 구매 ?�공 ??{nodeId}, ?�액 {currentGold}G</color>");
        }

        private string GetCurrentCharacterCode()
        {
            if (currentSelectedCharacter == null) return string.Empty;
            if (!string.IsNullOrEmpty(currentSelectedCharacter.heroCode))
                return currentSelectedCharacter.heroCode;
            return currentSelectedCharacter.characterName;
        }

        /// <summary>
        /// 캐릭?�의 Lv1 ?�드 4�?기본 ?�킬)�?unlockedNodeIds???�동 추�??�니??
        /// 캐릭??초기 ?�폰 직후, 그리�???캐릭?�로 ?�왑??직후(백업 ?�을 ?? ?�출?�세??
        /// SyncHashSet.Add ??멱등?��?�?중복 ?�출?�어???�전?�니??
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
                Debug.LogWarning("[SkillTree] AutoUnlockLv1Nodes: heroCode/characterName 모두 비어?�음 ???�킬?�리 매칭 불�?");
                return;
            }

            CharacterSkillTreeSO tree = SkillTreeRegistry.GetTree(code);
            if (tree == null || tree.allNodes == null)
            {
                Debug.LogWarning($"[SkillTree] AutoUnlockLv1Nodes: tree ?�음 ??code={code}");
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
            Debug.Log($"<color=cyan>[SkillTree] AutoUnlock: code={code}, Lv1 ?�드 {added}�?추�? (�?unlocked={unit.unlockedNodeIds.Count})</color>");
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

        // ?�?�?� ?�퍼 ?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�

        /// <summary>
        /// DontDestroyOnLoad�??��??�는 PlayerData �???connection???�유??것을 반환?�니??
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


        //���������� �Ѿ�� �κ��丮 ���� ������Ʈ

        /// <summary>
        /// ������ ����/ȹ�� ���� ��� ȣ��: myInventory �� pd.Info.Items �� ����ȭ�մϴ�.
        /// ���� ����(Weapon/Armor/Expendables)�� �ǵ帮�� �ʽ��ϴ�.
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
            // 1. ���� ���� �ִ� ��� PlayerDataPrefab(Clone) ��ü���� ã���ϴ�.
            PlayerData[] allPlayerDatas = FindObjectsByType<PlayerData>(FindObjectsSortMode.None);
            if (allPlayerDatas != null) Debug.Log("ã��Ϸ�");
            // 2. ã�� ��ü�� �� "�� ��"�� ��� ������Ʈ�մϴ�.
            foreach (var pd in allPlayerDatas)
            {
                // �� connection�� ���� PlayerData���� Ȯ��
                if (pd.connectionToClient != connectionToClient) continue;

                // �� pd�� �� ĳ���� ����Ʈ(_myPlayerDatas)���� �� ��° ĳ�������� Ȯ��
                int charIndex = _myPlayerDatas.IndexOf(pd);
                if (charIndex == -1) continue;

                // 3. ���� ���� (���� ������ �ִ� ĳ���� vs ����� ������)
                if (charIndex == currentActiveIndex && currentSelectedCharacter != null)
                {
                    // [���� Ȱ��ȭ�� ĳ����] �ǽð� EquipmentSlot���� ��������
                    var slot = currentSelectedCharacter.equipmentSlot;
                    if (slot != null && pd.Info != null)
                    {
                        // ������ ����/��
                        pd.Info.Weapon = !string.IsNullOrEmpty(slot.equippedWeaponId) ? slot.equippedWeapon.EquipInfo : null;
                        pd.Info.Armor  = !string.IsNullOrEmpty(slot.equippedArmorId)  ? slot.equippedArmor.EquipInfo  : null;

                        // ������ �Ҹ�ǰ �� Expendables
                        pd.Info.Expendables = new List<ConsumableInfo>();
                        foreach (var item in slot.equippedConsumables)
                            if (item.ConsumInfo != null)
                                for (int i = 0; i < item.amount; i++) pd.Info.Expendables.Add(item.ConsumInfo);

                        // ������ �κ� �� Items
                        pd.Info.Items = new List<InventoryItem>(currentSelectedCharacter.myInventory);
                        pd.Info.Gold = currentGold;

                        // [정보상 일원화 브릿지] 강화 스킬을 base에서 재구성해 주입 (active 캐릭터).
                        // 항상 base에서 다시 빌드 → 여러 번 불려도 누적 없음(idempotent). null이면 base 유지.
                        var __upgradedSkills = BuildUpgradedSkills(pd.FinalHeroCode, currentSelectedCharacter.mySkills);
                        if (__upgradedSkills != null) pd.Info.Skills = __upgradedSkills;

                        Debug.Log($"[Sync] ���� ĳ����({pd.FinalHeroCode}) �ǽð� ���� ������Ʈ �Ϸ�");
                    }
                }
                else if (savedCharacterData.TryGetValue(charIndex, out var saved))
                {
                    // [��Ȱ��ȭ�� ĳ����] ������ �����ص� savedCharacterData���� ��������
                    if (pd.Info != null)
                    {
                        pd.Info.Weapon = saved.equippedWeapon.EquipInfo;
                        pd.Info.Armor  = saved.equippedArmor.EquipInfo;

                        pd.Info.Expendables = new List<ConsumableInfo>();
                        foreach (var item in saved.equippedConsumables)
                            if (item.ConsumInfo != null)
                                for (int i = 0; i < item.amount; i++) pd.Info.Expendables.Add(item.ConsumInfo);

                        pd.Info.Items = new List<InventoryItem>(saved.inventory ?? new List<InventoryItem>());

                        Debug.Log($"[Sync] ��� ���� ĳ����({pd.FinalHeroCode}) ����� ���� ������Ʈ �Ϸ�");
                    }
                }
            }
        }

        // [����] ���� �� ȣ���(BattleStartBtn/ReadyOrStartButton/QuestVoteSystem)�� ����ϴ� ���� ����ȭ �������� �����մϴ�.
        // ───────── [정보상 일원화 브릿지] 강화 스킬 빌드 (순수함수) ─────────

        /// <summary>
        /// base 스킬(CharacterRegistry, 불변 원본)을 요소별 deep clone한 뒤,
        /// mySkills의 강화 레벨에 따라 SkillUpgradeData 값으로 덮어쓴 "새 리스트"를 반환한다.
        /// - 순수함수: Entry.Skills 원본을 절대 오염시키지 않음.
        /// - idempotent: 항상 base에서 재출발하므로 여러 번 호출해도 결과 동일.
        /// - base를 못 찾으면 null 반환 → 호출부는 기존 Info.Skills를 유지.
        /// 사용 enum/수치 필드는 Jun.SkillInfo(GameData.cs:203)만 대상으로 한다.
        /// </summary>
        public static List<Jun.SkillInfo> BuildUpgradedSkills(string heroCode, SyncList<PlayerSkill> mySkills)
        {
            if (!CharacterRegistry.TryGet(heroCode, out var entry) || entry.Skills == null)
            {
                Debug.LogWarning($"[SkillBridge] CharacterRegistry에서 '{heroCode}' base 스킬을 못 찾음 → 강화 미적용");
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

        private static int GetSkillLevel(SyncList<PlayerSkill> mySkills, int skillIndex)
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

                if (node.useTargetOverride) skill.Target     = node.Target;
                if (node.useEffectOverride) skill.EffectType = node.EffectType;
            }
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





