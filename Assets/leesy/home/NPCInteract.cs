using Lsy;
using UnityEngine;

namespace Lsy {
    public class NPCInteract : MonoBehaviour
    {
        [SerializeField] private NPCState _npcState;
        [SerializeField] private NPCPopupUI _npcUI; // UI 데이터를 그려주는 스크립트 참조

        // 콜라이더 클릭 혹은 상호작용 키(F) 입력 시 호출
        public void ClickNPC()
        {
            if (_npcState == null)
            {
                Debug.LogError("서버 데이터가 연결되지 않았습니다!");
                return;
            }
            if (_npcUI == null)
                _npcUI = FindPopupUIForState(_npcState);
            if (_npcUI == null)
            {
                Debug.LogError("[NPCInteract] NPCPopupUI가 연결되지 않았습니다!");
                return;
            }
            // 1. UI에 현재 클릭한 NPC의 상태(데이터)를 주입
            _npcUI.InitializeUI(_npcState);
        }
        private NPCPopupUI FindPopupUIForState(NPCState state)
        {
            // [수정] 씬에서 NPCInteract._npcUI 연결이 빠져도 NPC 종류에 맞는 팝업을 찾아 InitializeUI가 실행되도록 보정합니다.
            if (state == null || state.npcData == null) return null;

            NPCPopupUI[] popups = FindObjectsByType<NPCPopupUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var popup in popups)
            {
                if (popup == null) continue;

                bool isInformant = popup.informantUI != null;
                bool isBlacksmith = popup.blacksmithUI != null;
                bool isShop = popup.itemSlotContainer != null;
                string npcName = state.npcData.npcName;

                if (isInformant && npcName.Contains("Infor")) return popup;
                if (isBlacksmith && npcName.Contains("Black")) return popup;
                if (isShop && !isInformant && !isBlacksmith) return popup;
            }

            return null;
        }
    }
}