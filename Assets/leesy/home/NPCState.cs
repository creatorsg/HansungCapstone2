using Mirror;
using UnityEngine;

namespace Lsy
{
    public class NPCState : NetworkBehaviour
    {
        public NPCData npcData;

        public event System.Action OnStateChanged;

        [SyncVar(hook = nameof(OnStateChangedHook))]
        public int currentLevel = 1;

        [SyncVar(hook = nameof(OnStateChangedHook))]
        public int currentInvestedGold = 0;

        // [수정] Home 씬 재진입으로 NPC가 새로 생성되면 현재 방에 저장된 강화 상태를 복원합니다.
        public override void OnStartServer()
        {
            base.OnStartServer();

            if (npcData == null || string.IsNullOrWhiteSpace(npcData.npcName))
            {
                Debug.LogWarning("[NPCState] npcData 또는 npcName이 없어 진행도를 복원할 수 없습니다.");
                return;
            }

            var roomManager = NetworkManager.singleton as Jun.GameRoomManager;
            if (roomManager == null) return;

            if (roomManager.TryGetNpcProgress(npcData.npcName, out int level, out int investedGold))
            {
                currentLevel = Mathf.Clamp(level, 1, npcData.maxLevel);
                currentInvestedGold = Mathf.Max(0, investedGold);
                Debug.Log($"<color=cyan>[서버] {npcData.npcName} 진행도 복원: Lv.{currentLevel}, 누적 {currentInvestedGold}G</color>");
            }
        }

        private void OnStateChangedHook(int oldValue, int newValue)
        {
            // 서버에서 받아온다
            OnStateChanged?.Invoke();
        }

        [Server]
        public void ReceiveInvestment(int amount)
        {
            if (npcData == null) return;
            if (currentLevel >= npcData.maxLevel) return;

            currentInvestedGold += amount;
            CheckLevelUp();

            // [수정] 전투 후 Home 씬을 다시 로드해도 유지되도록 투자 처리 결과를 현재 방에 저장합니다.
            var roomManager = NetworkManager.singleton as Jun.GameRoomManager;
            roomManager?.SaveNpcProgress(npcData.npcName, currentLevel, currentInvestedGold);

            Debug.Log($"<color=yellow>[서버] {npcData.npcName} 투자 수신: {amount}G. 현재 누적: {currentInvestedGold}G</color>");
        }

        [Server]
        private void CheckLevelUp()
        {
            if (npcData == null || npcData.upgradeTargetGold == null) return;
            if (currentLevel >= npcData.maxLevel) return;

            int idx = currentLevel - 1;
            if (idx < 0 || idx >= npcData.upgradeTargetGold.Count) return;

            int targetGold = npcData.upgradeTargetGold[idx];
            if (currentInvestedGold >= targetGold)
            {
                currentInvestedGold -= targetGold;
                currentLevel++;

                Debug.Log($"<color=green>[서버] {npcData.npcName} 레벨업! 현재 Lv.{currentLevel}</color>");

                if (currentLevel < npcData.maxLevel)
                {
                    CheckLevelUp();
                }
            }
        }

        [Server]
        public void ExecutePurchase(string itemName, int price)
        {
            Debug.Log($"<color=white>[서버 구매 기록] 아이템: {itemName}, 가격: {price}</color>");
        }
    }
}
