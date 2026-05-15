using Mirror;
using TMPro;
using UnityEngine;

namespace Lsy
{
    // [수정] 퀘스트창 내부 표시 전용 UI. 버튼/루트 활성 제어는 QuestVoteController가 담당.
    public class QuestVoteUI : MonoBehaviour
    {
        [Header("UI text")]
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI countText;
        [SerializeField] private TextMeshProUGUI resultText;

        private void OnEnable()
        {
            QuestVoteSystem.OnVoteStateChanged += RefreshText;
            QuestVoteSelectionBridge.OnSelectionChanged += RefreshText;
            RefreshText();
        }

        private void OnDisable()
        {
            QuestVoteSystem.OnVoteStateChanged -= RefreshText;
            QuestVoteSelectionBridge.OnSelectionChanged -= RefreshText;
        }

        private void Update()
        {
            var vote = QuestVoteSystem.Instance;
            if (vote == null || !vote.VoteInProgress)
                return;

            double remain = vote.VoteEndTime - NetworkTime.time;
            int sec = Mathf.Max(0, Mathf.CeilToInt((float)remain));
            if (timerText != null)
                timerText.text = $"Vote time left: {sec}s";
        }

        private void RefreshText()
        {
            var vote = QuestVoteSystem.Instance;
            if (vote == null)
            {
                if (countText != null) countText.gameObject.SetActive(false);
                if (countText != null) countText.text = string.Empty;
                if (timerText != null) timerText.text = string.Empty;
                if (resultText != null) resultText.text = string.Empty;
                return;
            }

            if (countText != null) countText.gameObject.SetActive(vote.VoteInProgress);
            if (countText != null)
                countText.text = $"Accept {vote.AcceptCount} / Reject {vote.RejectCount}";

            if (!vote.VoteInProgress && timerText != null)
                timerText.text = string.Empty;

            // [수정] 투표 중에만 고정된 districtType 표시
            if (resultText != null)
                resultText.text = vote.VoteInProgress ? vote.SelectedDistrictType : string.Empty;
        }
    }
}
