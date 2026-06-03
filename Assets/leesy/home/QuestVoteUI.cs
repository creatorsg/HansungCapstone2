using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lsy
{
    public class QuestVoteUI : MonoBehaviour
    {
        [Header("UI Root")]
        [SerializeField] private GameObject votePanelRoot;
        [SerializeField] private TMP_Text voteStatusText;

        [Header("Buttons")]
        [SerializeField] private Button startVoteButton;
        [SerializeField] private Button acceptButton;
        [SerializeField] private Button rejectButton;

        [Header("Button Labels")]
        [SerializeField] private TMP_Text startVoteButtonText;
        [SerializeField] private TMP_Text acceptButtonText;
        [SerializeField] private TMP_Text rejectButtonText;

        [Header("Ready/Start Button (Existing)")]
        [SerializeField] private ReadyOrStartButton readyOrStartButton;

        [Header("Start Vote Settings")]
        [SerializeField] private DistrictType startVoteDistrict = DistrictType.Noble;

        [Header("Button Colors")]
        [SerializeField] private Color enabledColor = Color.white;
        [SerializeField] private Color disabledColor = new Color(0.3f, 0.3f, 0.3f, 1f);

        private bool _hasVotedThisRound;
        private int _lastRoundId = -1;
        // [수정] 퀘스트 창을 닫았다가 다시 열어도 현재 라운드의 로컬 투표 완료 상태를 유지합니다.
        private static int _locallyVotedRoundId = -1;

        private bool IsHost => NetworkServer.active && NetworkClient.active;

        private void Awake() { SetFixedButtonLabels(); }

        private void OnEnable()
        {
            QuestVoteSystem.OnVoteSnapshotChanged += OnVoteSnapshotChanged;
            QuestVoteSystem.OnVotePhaseChanged += OnVotePhaseChanged;
            ForceRefresh();
        }

        private void OnDisable()
        {
            QuestVoteSystem.OnVoteSnapshotChanged -= OnVoteSnapshotChanged;
            QuestVoteSystem.OnVotePhaseChanged -= OnVotePhaseChanged;
        }

        public void OnClickStartVote()
        {
            if (!IsHost) return;

            var system = QuestVoteSystem.Instance;
            if (system == null) return;
            if (!system.IsQuestSelected) return;
            if (system.IsVoteRunning || system.IsVoteFinished) return;

            system.CmdRequestStartVoteFromUI();
            LockButton(startVoteButton, true);
        }

        public void OnClickVoteAccept()
        {
            if (!SubmitVote(true)) return;
            _hasVotedThisRound = true;
            // [수정] UI가 비활성화/재활성화되어도 같은 투표 라운드에서는 투표 완료 상태를 복원합니다.
            _locallyVotedRoundId = _lastRoundId;
            LockButton(acceptButton, true);
            LockButton(rejectButton, true);
        }

        public void OnClickVoteReject()
        {
            if (!SubmitVote(false)) return;
            _hasVotedThisRound = true;
            // [수정] UI가 비활성화/재활성화되어도 같은 투표 라운드에서는 투표 완료 상태를 복원합니다.
            _locallyVotedRoundId = _lastRoundId;
            LockButton(acceptButton, true);
            LockButton(rejectButton, true);
        }

        private bool SubmitVote(bool accept)
        {
            if (!NetworkClient.active || IsHost) return false;
            var system = QuestVoteSystem.Instance;
            if (system == null || !system.IsVoteRunning || _hasVotedThisRound) return false;
            system.CmdSubmitVote(accept);
            return true;
        }

        private void OnVoteSnapshotChanged(QuestVoteSystem.VoteSnapshot snapshot)
        {
            if (_lastRoundId != snapshot.VoteRoundId)
            {
                _lastRoundId = snapshot.VoteRoundId;
                RestoreLocalVoteState(snapshot);
            }

            RefreshStatusText(snapshot);
            RefreshState(snapshot);
        }

        private void OnVotePhaseChanged(bool _, bool __)
        {
            var system = QuestVoteSystem.Instance;
            if (system != null) RefreshState(system.CurrentSnapshot);
        }

        private void ForceRefresh()
        {
            SetFixedButtonLabels();
            var system = QuestVoteSystem.Instance;
            if (system == null)
            {
                if (voteStatusText != null) voteStatusText.text = "투표 시스템 대기 중...";
                SetVisible(startVoteButton, false);
                SetVisible(acceptButton, false);
                SetVisible(rejectButton, false);
                SetReadyVisible(false);
                return;
            }

            var snapshot = system.CurrentSnapshot;
            _lastRoundId = snapshot.VoteRoundId;
            RestoreLocalVoteState(snapshot);
            RefreshStatusText(snapshot);
            RefreshState(snapshot);
        }

        private void RestoreLocalVoteState(QuestVoteSystem.VoteSnapshot snapshot)
        {
            // [수정] 새 네트워크 세션의 초기 상태에서는 이전 세션의 로컬 투표 기록을 제거합니다.
            if (snapshot.VoteRoundId == 0 && !snapshot.IsQuestSelected && !snapshot.IsVoteRunning && !snapshot.IsVoteFinished)
            {
                _locallyVotedRoundId = -1;
                _hasVotedThisRound = false;
                return;
            }

            // [수정] 창을 다시 열 때 투표 완료 상태를 false로 초기화하지 않고 현재 라운드 기록에서 복원합니다.
            _hasVotedThisRound = _locallyVotedRoundId == snapshot.VoteRoundId;
        }

        private void SetFixedButtonLabels()
        {
            if (startVoteButtonText != null) startVoteButtonText.text = "투표 시작";
            if (acceptButtonText != null) acceptButtonText.text = "수락";
            if (rejectButtonText != null) rejectButtonText.text = "거절";
        }

        private void RefreshState(QuestVoteSystem.VoteSnapshot snapshot)
        {
            if (votePanelRoot != null) votePanelRoot.SetActive(true);

            bool preVote = snapshot.IsQuestSelected && !snapshot.IsVoteRunning && !snapshot.IsVoteFinished;
            bool voting = snapshot.IsVoteRunning;
            bool approved = snapshot.IsVoteFinished && snapshot.IsVoteApproved;

            SetVisible(startVoteButton, preVote && IsHost);
            SetVisible(acceptButton, voting && !IsHost);
            SetVisible(rejectButton, voting && !IsHost);

            LockButton(startVoteButton, !(preVote && IsHost));
            LockButton(acceptButton, !(voting && !IsHost && !_hasVotedThisRound));
            LockButton(rejectButton, !(voting && !IsHost && !_hasVotedThisRound));

            SetReadyVisible(approved);
        }

        private void SetReadyVisible(bool visible)
        {
            if (readyOrStartButton != null)
                readyOrStartButton.gameObject.SetActive(visible);
        }

        private void SetVisible(Button button, bool visible)
        {
            if (button != null) button.gameObject.SetActive(visible);
        }

        private void LockButton(Button button, bool locked)
        {
            if (button == null) return;
            button.interactable = !locked;
            var image = button.GetComponent<Image>();
            if (image != null) image.color = locked ? disabledColor : enabledColor;
        }

        private void RefreshStatusText(QuestVoteSystem.VoteSnapshot snapshot)
        {
            if (voteStatusText == null) return;
            string stageName = string.IsNullOrWhiteSpace(snapshot.LockedStageName) ? "미정" : snapshot.LockedStageName;

            if (!snapshot.IsQuestSelected && !snapshot.IsVoteRunning && !snapshot.IsVoteFinished)
            {
                voteStatusText.text = string.IsNullOrEmpty(snapshot.VoteResultMessage)
                    ? "퀘스트를 먼저 선택하세요."
                    : snapshot.VoteResultMessage;
                return;
            }

            if (!snapshot.IsVoteRunning && !snapshot.IsVoteFinished)
            {
                voteStatusText.text = $"선택된 스테이지: {stageName}";
                return;
            }

            if (snapshot.IsVoteRunning)
            {
                voteStatusText.text = $"스테이지: {stageName} 남은 시간: {snapshot.RemainingSeconds}초 수락: {snapshot.AcceptCount} / 거절: {snapshot.RejectCount}";
                return;
            }
        }
    }
}
