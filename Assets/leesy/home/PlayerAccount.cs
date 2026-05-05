using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace Lsy
{
    /// <summary>
    /// 캐릭터 전환 시 저장할 개인 데이터 묶음.
    /// 인벤토리, 무기 업그레이드, 스킬 전부 캐릭터별로 독립 보관.
    /// </summary>
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

        public CharacterUnit currentSelectedCharacter;

        public List<GameObject> myCharacterPrefabs = new List<GameObject>();
        public List<CharacterData> myCharacterDataList = new List<CharacterData>();

        private int currentActiveIndex = 0;

        // 인벤토리/무기/스킬을 캐릭터 인덱스별로 보관
        private Dictionary<int, CharacterSaveData> savedCharacterData = new Dictionary<int, CharacterSaveData>();

        // ==========================================
        // 접속 시
        // ==========================================
        public override void OnStartLocalPlayer()
        {
            LocalInstance = this;
            Debug.Log("<color=green>[계정] 접속 성공!</color>");
            CmdRequestMyCharacters();
        }

        // ==========================================
        // 첫 캐릭터 스폰
        // ==========================================
        [Command]
        public void CmdRequestMyCharacters()
        {
            if (myCharacterPrefabs == null || myCharacterPrefabs.Count == 0) return;

            GameObject newCharObj = Instantiate(myCharacterPrefabs[0]);
            CharacterUnit activeUnit = newCharObj.GetComponent<CharacterUnit>();

            if (activeUnit != null)
            {
                activeUnit.SetupFromData(myCharacterDataList[0]);
                NetworkServer.Spawn(newCharObj, connectionToClient);
                NetworkServer.ReplacePlayerForConnection(connectionToClient, newCharObj, ReplacePlayerOptions.KeepAuthority);

                currentSelectedCharacter = activeUnit;
                currentActiveIndex = 0;
            }
        }

        // ==========================================
        // UI 버튼에서 호출 (클라이언트)
        // ==========================================
        public void SelectCharacter(int profileIndex)
        {
            if (currentSelectedCharacter == null) return;
            if (profileIndex < 0 || profileIndex >= myCharacterDataList.Count) return;
            if (profileIndex == currentActiveIndex) return;

            CmdRequestSwapCharacter(profileIndex);
        }

        // ==========================================
        // 캐릭터 전환 (서버)
        // ==========================================
        [Command]
        public void CmdRequestSwapCharacter(int targetIndex)
        {
            if (targetIndex < 0 || targetIndex >= myCharacterDataList.Count) return;
            if (currentActiveIndex == targetIndex) return;

            // 1. 현재 캐릭터 데이터 전부 백업
            CharacterSaveData backup = new CharacterSaveData();

            foreach (var item in currentSelectedCharacter.myInventory)
                backup.inventory.Add(item);

            backup.selectedWeaponId = currentSelectedCharacter.selectedWeaponId;
            backup.purchasedNodeCount = currentSelectedCharacter.purchasedNodeCount;

            foreach (var skill in currentSelectedCharacter.mySkills)
                backup.skills.Add(skill);

            savedCharacterData[currentActiveIndex] = backup;

            // 2. 인덱스 교체
            currentActiveIndex = targetIndex;

            // 3. 새 스탯 주입
            CharacterData targetData = myCharacterDataList[targetIndex];
            currentSelectedCharacter.SetupFromData(targetData);

            // 4. 무기/스킬/인벤토리 초기화
            currentSelectedCharacter.myInventory.Clear();
            currentSelectedCharacter.mySkills.Clear();
            currentSelectedCharacter.selectedWeaponId = "";
            currentSelectedCharacter.purchasedNodeCount = 0;

            // 5. 이전에 저장해둔 데이터가 있으면 복원
            if (savedCharacterData.TryGetValue(targetIndex, out CharacterSaveData saved))
            {
                foreach (var item in saved.inventory)
                    currentSelectedCharacter.myInventory.Add(item);

                currentSelectedCharacter.selectedWeaponId = saved.selectedWeaponId;
                currentSelectedCharacter.purchasedNodeCount = saved.purchasedNodeCount;

                foreach (var skill in saved.skills)
                    currentSelectedCharacter.mySkills.Add(skill);
            }

            TargetRpcRefreshUI(connectionToClient);
            Debug.Log($"<color=cyan>[서버] {targetData.charName}으로 스왑 완료!</color>");
        }

        [TargetRpc]
        private void TargetRpcRefreshUI(NetworkConnection target)
        {
            if (InventoryUI.Instance != null)
                InventoryUI.Instance.RefreshInventory();
        }
    }
}