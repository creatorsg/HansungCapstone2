using System.Collections.Generic;
using UnityEngine;

namespace Lsy
{
    public abstract class BaseUpgradeUI : MonoBehaviour
    {
        public NPCState NpcState { get; private set; }

        public void InitializeUI(NPCState npcState)
        {
            // 기존 구독 해제
            if (NpcState != null)
                NpcState.OnStateChanged -= OnNpcStateChanged;

            NpcState = npcState;

            // NPC 레벨/골드 변경 시 자동으로 노드 갱신
            if (NpcState != null)
                NpcState.OnStateChanged += OnNpcStateChanged;

            Debug.Log($"<color=cyan>[UpgradeUI] InitializeUI - NPC:{(npcState != null ? npcState.name : "NULL")}</color>");
            RefreshAllRows();
        }

        // NPC 상태 변경 시 (레벨업 등) 자동 호출
        private void OnNpcStateChanged()
        {
            Debug.Log($"<color=cyan>[UpgradeUI] NPC 상태 변경 감지 - Lv:{NpcState?.currentLevel} → 노드 갱신</color>");
            RefreshAllRows();
        }

        public void RefreshAllRows()
        {
            if (NpcState == null)
            {
                Debug.LogWarning("[UpgradeUI] RefreshAllRows - NpcState가 NULL");
                return;
            }

            CharacterUnit unit = PlayerAccount.LocalInstance?.currentSelectedCharacter;
            Debug.Log($"<color=cyan>[UpgradeUI] RefreshAllRows - NPC Lv:{NpcState.currentLevel}, Unit:{(unit != null ? unit.characterName : "NULL")}</color>");

            foreach (var row in GetRows())
            {
                if (row != null)
                    row.RefreshNodes(NpcState.currentLevel, unit);
            }
        }

        private void OnDisable()
        {
            // 창이 닫힐 때 구독 해제
            if (NpcState != null)
                NpcState.OnStateChanged -= OnNpcStateChanged;
        }

        protected abstract IEnumerable<BaseUpgradeRow> GetRows();
    }
}