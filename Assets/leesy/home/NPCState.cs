using Mirror;
using UnityEngine;
using System;

namespace Lsy
{
    public class NPCState : NetworkBehaviour
    {
        public NPCData npcData;

        public event Action OnStateChanged;

        [SyncVar(hook = nameof(OnStateChangedHook))]
        public int currentLevel = 1;

        [SyncVar(hook = nameof(OnStateChangedHook))]
        public int currentInvestedGold = 0;

        private void OnStateChangedHook(int oldValue, int newValue)
        {
            OnStateChanged?.Invoke();
        }

        [Server]
        public void ReceiveInvestment(int amount)
        {
            if (npcData == null) return;
            if (currentLevel >= npcData.maxLevel) return;

            currentInvestedGold += amount;
            CheckLevelUp();

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

                // 다음 레벨 목표치도 바로 달성했는지 재귀 체크
                if (currentLevel < npcData.maxLevel)
                {
                    CheckLevelUp();
                }
            }
        }

        // 구매 로직 처리
        [Server]
        public void ExecutePurchase(string itemName, int price)
        {
            // 서버에 구매한 아이템 전송
            Debug.Log($"<color=white>[서버 구매 기록] 아이템: {itemName}, 가격: {price}</color>");
        }
    }
}