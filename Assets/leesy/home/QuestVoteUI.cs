using TMPro;
using UnityEngine;

namespace Lsy
{
    public class QuestVoteUI : MonoBehaviour
    {
        [Header("Legacy refs (unused)")]
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI countText;
        [SerializeField] private TextMeshProUGUI stageNameText;

        // [수정] QuestVoteController가 단독으로 텍스트를 갱신하도록 본 컴포넌트는 비활성 동작
        // 인스펙터 직렬화 호환을 위해 필드만 유지합니다.
        private void OnEnable() { }
        private void OnDisable() { }
    }
}
