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

        public int currentActiveIndex { get; private set; } = -1;

        public static event Action OnCharacterSwitched;

        public CharacterUnit currentSelectedCharacter;

        public List<GameObject> myCharacterPrefabs = new List<GameObject>();
        public List<CharacterData> myCharacterDataList = new List<CharacterData>();

        private Dictionary<int, CharacterSaveData> savedCharacterData = new Dictionary<int, CharacterSaveData>();

        public override void OnStartLocalPlayer()
        {
            LocalInstance = this;
            Debug.Log("<color=green>[계정] 접속 성공!</color>");
            CmdRequestMyCharacters();
        }

        [Command]
        public void CmdRequestMyCharacters()
        {
            if (myCharacterPrefabs == null || myCharacterPrefabs.Count == 0) return;

            GameObject newCharObj = Instantiate(myCharacterPrefabs[0]);
            CharacterUnit activeUnit = newCharObj.GetComponent<CharacterUnit>();

            if (activeUnit != null)
            {
                NetworkServer.Spawn(newCharObj, connectionToClient);
                NetworkServer.ReplacePlayerForConnection(connectionToClient, newCharObj, ReplacePlayerOptions.KeepAuthority);
                activeUnit.SetupFromData(myCharacterDataList[0]);

                currentSelectedCharacter = activeUnit;
                TargetRpcRefreshUI(connectionToClient, -1);
            }
        }

        public void SelectCharacter(int profileIndex)
        {
            if (profileIndex < 0 || profileIndex >= myCharacterDataList.Count) return;
            if (profileIndex == currentActiveIndex) return;

            CmdRequestSwapCharacter(profileIndex);
        }

        [Command]
        public void CmdRequestSwapCharacter(int targetIndex)
        {
            if (targetIndex < 0 || targetIndex >= myCharacterDataList.Count) return;
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
                savedCharacterData[currentActiveIndex] = backup;
            }

            currentActiveIndex = targetIndex;

            CharacterData targetData = myCharacterDataList[targetIndex];
            currentSelectedCharacter.SetupFromData(targetData);

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
            Debug.Log($"<color=cyan>[서버] {targetData.charName}으로 스왑 완료!</color>");
        }

        [TargetRpc]
        private void TargetRpcRefreshUI(NetworkConnection target, int newActiveIndex)
        {
            currentActiveIndex = newActiveIndex;
            Debug.Log($"<color=cyan>[클라이언트] currentActiveIndex: {newActiveIndex}</color>");

            if (InventoryUI.Instance != null)
                InventoryUI.Instance.RefreshInventory();

            if (GoldUI.Instance != null)
                GoldUI.Instance.SetTrackedUnit(currentSelectedCharacter);

            OnCharacterSwitched?.Invoke();
        }
    }
}