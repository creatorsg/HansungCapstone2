using System.Collections.Generic;
using UnityEngine;

namespace Lsy
{
    public abstract class BaseUpgradeUI : MonoBehaviour
    {
        public NPCState NpcState { get; private set; }

        public void InitializeUI(NPCState npcState)
        {
            if (NpcState != null)
                NpcState.OnStateChanged -= OnNpcStateChanged;

            NpcState = npcState;

            if (NpcState != null)
                NpcState.OnStateChanged += OnNpcStateChanged;

            Debug.Log($"<color=cyan>[UpgradeUI] 초기화 - NPC:{(npcState != null ? npcState.name : "NULL")}</color>");
            RefreshAllRows();
        }

        private void OnNpcStateChanged()
        {
            Debug.Log($"<color=cyan>[UpgradeUI] NPC 상태 변경 - Lv:{NpcState?.currentLevel}, 강화 UI 갱신</color>");
            RefreshAllRows();
        }

        public void RefreshAllRows()
        {
            if (NpcState == null)
            {
                Debug.LogWarning("[UpgradeUI] RefreshAllRows 실패 - NpcState가 NULL입니다.");
                return;
            }

            CharacterUnit unit = PlayerAccount.LocalInstance?.currentSelectedCharacter;
            Debug.Log($"<color=cyan>[UpgradeUI] 전체 강화 줄 갱신 - NPC Lv:{NpcState.currentLevel}, Unit:{(unit != null ? unit.characterName : "NULL")}</color>");

            foreach (var row in GetRows())
            {
                if (row != null)
                    row.RefreshNodes(NpcState.currentLevel, unit);
            }
        }

        private void OnEnable()
        {
            CharacterUnit.OnLocalUpgradeStateChanged += RefreshAllRows;
        }

        private void OnDisable()
        {
            if (NpcState != null)
                NpcState.OnStateChanged -= OnNpcStateChanged;

            CharacterUnit.OnLocalUpgradeStateChanged -= RefreshAllRows;
        }

        protected abstract IEnumerable<BaseUpgradeRow> GetRows();
    }
}
