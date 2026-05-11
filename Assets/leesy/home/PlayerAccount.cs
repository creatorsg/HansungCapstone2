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
    }

    public class PlayerAccount : NetworkBehaviour
    {
        public static PlayerAccount LocalInstance;

        /// <summary>로컬 PlayerAccount가 준비됐을 때 — GoldUI 등 초기화용</summary>
        public static event Action<PlayerAccount> OnLocalAccountReady;
        public static event Action OnCharacterSwitched;

        /// <summary>모든 PlayerAccount(로컬+원격)가 myHeroCodes가 채워진 후 발생 — 초상화 전체 로드용</summary>
        public static event Action<PlayerAccount> OnAnyAccountReady;

        // ─── 골드 (플레이어 귀속) ──────────────────────────────────────
        [SyncVar(hook = nameof(OnCurrentGoldChanged))]
        public int currentGold;

        public event Action<int> OnGoldChanged;

        private void OnCurrentGoldChanged(int oldVal, int newVal)
        {
            if (!isOwned) return;
            OnGoldChanged?.Invoke(newVal);
        }

        // ─── 캐릭터 관리 ──────────────────────────────────────────────
        public int currentActiveIndex { get; private set; } = -1;

        public CharacterUnit currentSelectedCharacter;

        public List<GameObject> myCharacterPrefabs = new List<GameObject>();
        public List<CharacterData> myCharacterDataList = new List<CharacterData>();

        [SyncVar] public int myCharacterCount = 0;

        /// <summary>내가 조종하는 캐릭터의 heroPos 목록 — 초상화 소유권 판단용</summary>
        public readonly SyncList<int> myHeroPositions = new SyncList<int>();

        /// <summary>myHeroPositions와 1:1 대응하는 heroCode 목록 — 초상화 이미지 표시용</summary>
        public readonly SyncList<string> myHeroCodes = new SyncList<string>();

        // 서버 전용: 이 connection이 선택한 PlayerData 목록 (FinalHeroPos 순 정렬)
        private List<PlayerData> _myPlayerDatas = new List<PlayerData>();

        private Dictionary<int, CharacterSaveData> savedCharacterData = new Dictionary<int, CharacterSaveData>();

        // ─── 초기화 ───────────────────────────────────────────────────

        public override void OnStartClient()
        {
            base.OnStartClient();
            // myHeroCodes가 나중에 채워질 때를 대비해 콜백 등록
            myHeroCodes.Callback += OnHeroCodesChanged;

            // 서버에서 이미 데이터가 있으면 (씬 재진입 등) 즉시 발동
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
            // 최초 데이터가 완전히 들어온 시점(myHeroPositions과 크기 일치)에 이벤트 발생
            if (myHeroCodes.Count > 0 && myHeroCodes.Count == myHeroPositions.Count)
                OnAnyAccountReady?.Invoke(this);
        }

        public override void OnStartLocalPlayer()
        {
            LocalInstance = this;
            Debug.Log("<color=green>[계정] 접속 성공!</color>");
            OnLocalAccountReady?.Invoke(this);
            CmdRequestMyCharacters();
        }

        [Command]
        public void CmdRequestMyCharacters()
        {
            if (myCharacterPrefabs == null || myCharacterPrefabs.Count == 0) return;

            // 이 connection에 속한 모든 PlayerData를 수집 (FinalHeroPos 순 정렬)
            _myPlayerDatas.Clear();
            var allPlayerDatas = FindObjectsByType<PlayerData>(FindObjectsSortMode.None);
            Debug.Log($"[PlayerAccount] CmdRequestMyCharacters: 전체 PlayerData {allPlayerDatas.Length}개 발견, 내 connectionToClient={connectionToClient}");
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

            // ★ Spawn 전에 Setup → 스폰 메시지에 heroPos/heroCode가 올바르게 포함됨
            if (_myPlayerDatas.Count > 0)
            {
                activeUnit.SetupFromPlayerData(_myPlayerDatas[0]);
                currentGold = 2000;
                currentActiveIndex = 0;
                Debug.Log($"[PlayerAccount] PlayerData 기반 초기화: code={_myPlayerDatas[0].FinalHeroCode}, pos={_myPlayerDatas[0].FinalHeroPos}");
            }
            else
            {
                if (myCharacterDataList == null || myCharacterDataList.Count == 0) return;
                Debug.LogWarning("[PlayerAccount] PlayerData 없음 — CharacterData 에셋으로 폴백합니다.");
                activeUnit.SetupFromData(myCharacterDataList[0]);
                currentGold = myCharacterDataList[0].gold;
            }

            NetworkServer.Spawn(newCharObj, connectionToClient);
            NetworkServer.ReplacePlayerForConnection(connectionToClient, newCharObj, ReplacePlayerOptions.KeepAuthority);

            currentSelectedCharacter = activeUnit;
            TargetRpcRefreshUI(connectionToClient, currentActiveIndex);
        }

        // ─── 캐릭터 전환 ──────────────────────────────────────────────

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

            // 현재 캐릭터 상태 저장
            if (currentActiveIndex != -1)
            {
                CharacterSaveData backup = new CharacterSaveData();
                foreach (var item in currentSelectedCharacter.myInventory)
                    backup.inventory.Add(item);
                backup.selectedWeaponId = currentSelectedCharacter.selectedWeaponId;
                backup.purchasedNodeCount = currentSelectedCharacter.purchasedNodeCount;
                foreach (var skill in currentSelectedCharacter.mySkills)
                    backup.skills.Add(skill);
                savedCharacterData[currentActiveIndex] = backup;
            }

            currentActiveIndex = targetIndex;

            // 새 캐릭터 데이터로 초기화 (골드는 건드리지 않음)
            if (hasPlayerDatas)
            {
                PlayerData targetPd = _myPlayerDatas[targetIndex];
                currentSelectedCharacter.SetupFromPlayerData(targetPd);
                Debug.Log($"<color=cyan>[서버] {targetPd.FinalHeroCode}으로 스왑 완료!</color>");
            }
            else
            {
                CharacterData targetData = myCharacterDataList[targetIndex];
                currentSelectedCharacter.SetupFromData(targetData);
                Debug.Log($"<color=cyan>[서버] {targetData.charName}으로 스왑 완료! (폴백)</color>");
            }

            // 저장된 인벤토리/스킬 복원
            currentSelectedCharacter.myInventory.Clear();
            currentSelectedCharacter.mySkills.Clear();
            currentSelectedCharacter.selectedWeaponId = "";
            currentSelectedCharacter.purchasedNodeCount = 0;

            if (savedCharacterData.TryGetValue(targetIndex, out CharacterSaveData saved))
            {
                foreach (var item in saved.inventory)
                    currentSelectedCharacter.myInventory.Add(item);
                currentSelectedCharacter.selectedWeaponId = saved.selectedWeaponId;
                currentSelectedCharacter.purchasedNodeCount = saved.purchasedNodeCount;
                foreach (var skill in saved.skills)
                    currentSelectedCharacter.mySkills.Add(skill);
            }

            TargetRpcRefreshUI(connectionToClient, targetIndex);
        }

        [TargetRpc]
        private void TargetRpcRefreshUI(NetworkConnection target, int newActiveIndex)
        {
            currentActiveIndex = newActiveIndex;
            Debug.Log($"<color=cyan>[클라이언트] currentActiveIndex: {newActiveIndex}</color>");

            if (InventoryUI.Instance != null)
                InventoryUI.Instance.RefreshInventory();

            if (GoldUI.Instance != null)
                GoldUI.Instance.RefreshGold();

            OnCharacterSwitched?.Invoke();
        }

        // ─── NPC 상호작용 커맨드 (골드 체크/차감 담당) ────────────────

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

        // ─── 헬퍼 ─────────────────────────────────────────────────────

        /// <summary>
        /// DontDestroyOnLoad로 유지되는 PlayerData 중 이 connection이 소유한 것을 반환합니다.
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
