using Mirror;
using TMPro;
using UnityEngine;

namespace Lsy
{
    public class QuestVoteUI : MonoBehaviour
    {
        [Header("UI text")]
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI countText;
        [SerializeField] private TextMeshProUGUI stageNameText;

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
                if (stageNameText != null) stageNameText.text = string.Empty;
                return;
            }

            if (countText != null) countText.gameObject.SetActive(vote.VoteInProgress);
            if (countText != null)
                countText.text = $"Accept {vote.AcceptCount} / Reject {vote.RejectCount}";

            if (!vote.VoteInProgress && timerText != null)
                timerText.text = string.Empty;

            if (stageNameText != null)
                stageNameText.text = vote.VoteInProgress ? vote.SelectedStageName : string.Empty;
        }
    }
}
