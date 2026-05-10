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
            // 1. UI에 현재 클릭한 NPC의 상태(데이터)를 주입
            _npcUI.InitializeUI(_npcState);
        }
    }
}
