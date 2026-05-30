using System.Collections.Generic;
using UnityEngine;

namespace Lsy
{
    public abstract class BaseUpgradeUI : MonoBehaviour
    {
        public NPCState NpcState { get; private set; }

        public void InitializeUI(NPCState npcState)
        {
            // ���� ���� ����
            if (NpcState != null)
                NpcState.OnStateChanged -= OnNpcStateChanged;

            NpcState = npcState;

            // NPC ����/��� ���� �� �ڵ����� ��� ����
            if (NpcState != null)
                NpcState.OnStateChanged += OnNpcStateChanged;

            Debug.Log($"<color=cyan>[UpgradeUI] InitializeUI - NPC:{(npcState != null ? npcState.name : "NULL")}</color>");
            RefreshAllRows();
        }

        // NPC ���� ���� �� (������ ��) �ڵ� ȣ��
        private void OnNpcStateChanged()
        {
            Debug.Log($"<color=cyan>[UpgradeUI] NPC ���� ���� ���� - Lv:{NpcState.currentLevel} �� ��� ����</color>");
            RefreshAllRows();
        }

        public virtual void RefreshAllRows()
        {
            if (NpcState == null)
            {
                Debug.LogWarning("[UpgradeUI] RefreshAllRows - NpcState�� NULL");
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

        private void OnEnable()
        {
            // 캐릭터 강화/스킬 상태가 바뀌면 자동으로 행 갱신
            CharacterUnit.OnLocalUpgradeStateChanged += RefreshAllRows;
            // [수정] 캐릭터를 바꿨을 때 열린 대장장이 UI가 새 캐릭터의 구매 상태로 다시 그려지도록 갱신합니다.
            PlayerAccount.OnCharacterSwitched += RefreshAllRows;
        }

        private void OnDisable()
        {
            if (NpcState != null)
                NpcState.OnStateChanged -= OnNpcStateChanged;

            CharacterUnit.OnLocalUpgradeStateChanged -= RefreshAllRows;
            PlayerAccount.OnCharacterSwitched -= RefreshAllRows;
        }

        protected abstract IEnumerable<BaseUpgradeRow> GetRows();
    }
}
