using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lsy
{
    // [수정] 항상 활성 오브젝트에 붙여서 투표 버튼/준비버튼 상태를 관리.
    public class QuestVoteController : MonoBehaviour
    {
        [Header("Vote / existing button refs")]
        [SerializeField] private GameObject readyOrStartButtonRoot;
        [SerializeField] private GameObject hostVoteStartButtonRoot;
        [SerializeField] private GameObject clientVotePanelRoot;

        [Header("Vote buttons")]
        [SerializeField] private Button hostVoteStartButton;
        [SerializeField] private Button acceptButton;
        [SerializeField] private Button rejectButton;

        [Header("Vote texts")]
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI countText;
        [SerializeField] private TextMeshProUGUI stageNameText;

        [Header("Colors")]
        [SerializeField] private Color enabledColor = Color.white;
        [SerializeField] private Color disabledColor = new Color(0.35f, 0.35f, 0.35f, 1f);
        [SerializeField] private Color clientVoteEnabledColor = Color.white;
        [SerializeField] private Color clientVoteDisabledColor = new Color(0.35f, 0.35f, 0.35f, 1f);
        [SerializeField] private bool lockReadyStartUntilVotePass = true;

        private Image _hostVoteStartImage;
        private Image _acceptImage;
        private Image _rejectImage;
        private bool _submitted;
        private bool _loggedTextRefConflict;

        private bool IsHost => NetworkServer.active && NetworkClient.active;

        private bool HasQuestSelection
        {
            get
            {
                if (QuestVoteSelectionBridge.Instance != null)
                    return QuestVoteSelectionBridge.Instance.HasSelection;
                return SelectedQuest.Current != null;
            }
        }

        private void Awake()
        {
            if (readyOrStartButtonRoot == null)
            {
                ReadyOrStartButton ready = FindFirstObjectByType<ReadyOrStartButton>(FindObjectsInactive.Include);
                if (ready != null) readyOrStartButtonRoot = ready.gameObject;
            }

            if (hostVoteStartButton != null)
            {
                _hostVoteStartImage = hostVoteStartButton.GetComponent<Image>();
                if (_hostVoteStartImage == null && hostVoteStartButton.targetGraphic != null)
                    _hostVoteStartImage = hostVoteStartButton.targetGraphic as Image;
                hostVoteStartButton.transition = Selectable.Transition.None;
            }

            if (acceptButton != null)
            {
                _acceptImage = acceptButton.GetComponent<Image>();
                if (_acceptImage == null && acceptButton.targetGraphic != null)
                    _acceptImage = acceptButton.targetGraphic as Image;
                acceptButton.transition = Selectable.Transition.None;
            }

            if (rejectButton != null)
            {
                _rejectImage = rejectButton.GetComponent<Image>();
                if (_rejectImage == null && rejectButton.targetGraphic != null)
                    _rejectImage = rejectButton.targetGraphic as Image;
                rejectButton.transition = Selectable.Transition.None;
            }

            // [수정] QuestVoteUI가 비활성 오브젝트여도 텍스트를 갱신할 수 있게 참조 확보
            ResolveVoteTextRefs();
        }

        private void OnEnable()
        {
            QuestVoteSystem.OnVoteStateChanged += RefreshState;
            QuestVoteSelectionBridge.OnSelectionChanged += RefreshState;
            HookButtons(true);
            RefreshState();
        }

        private void OnDisable()
        {
            QuestVoteSystem.OnVoteStateChanged -= RefreshState;
            QuestVoteSelectionBridge.OnSelectionChanged -= RefreshState;
            HookButtons(false);
        }

        private void Update()
        {
            // [수정] 이벤트 누락 대비 폴링 동기화
            RefreshState();
            RefreshVoteTextsRuntime();
        }

        private void OnClickHostVoteStart()
        {
            var vote = QuestVoteSystem.Instance;
            if (vote == null || !IsHost || !HasQuestSelection) return;

            _submitted = false;
            vote.CmdHostStartVote();
        }

        private void OnClickAccept()
        {
            var vote = QuestVoteSystem.Instance;
            if (vote == null || IsHost || _submitted || !vote.VoteInProgress) return;

            _submitted = true;
            vote.CmdSubmitVote(true);
            RefreshState();
        }

        private void OnClickReject()
        {
            var vote = QuestVoteSystem.Instance;
            if (vote == null || IsHost || _submitted || !vote.VoteInProgress) return;

            _submitted = true;
            vote.CmdSubmitVote(false);
            RefreshState();
        }

        private void RefreshState()
        {
            var vote = QuestVoteSystem.Instance;
            if (vote == null)
            {
                ApplyIdleState();
                return;
            }

            if (vote.VoteInProgress)
            {
                ApplyVotingState();
                return;
            }

            if (vote.VoteResolved)
            {
                if (vote.VotePassed) ApplySuccessState();
                else ApplyFailState();
                return;
            }

            ApplyIdleState();
        }

        private void ApplyIdleState()
        {
            bool singlePlayerHost = IsSinglePlayerHost();
            SetActiveSafe(readyOrStartButtonRoot, singlePlayerHost || !lockReadyStartUntilVotePass);

            if (IsHost)
            {
                SetActiveSafe(hostVoteStartButtonRoot, !singlePlayerHost);
                SetActiveSafe(clientVotePanelRoot, false);
                SetHostVoteStartInteractable(!singlePlayerHost && HasQuestSelection);
            }
            else
            {
                SetActiveSafe(hostVoteStartButtonRoot, false);
                SetActiveSafe(clientVotePanelRoot, true);
                SetClientVoteInteractable(false);
            }

            _submitted = false;
            HideVoteTexts();
        }

        private void ApplyVotingState()
        {
            SetActiveSafe(readyOrStartButtonRoot, false);

            if (IsHost)
            {
                SetActiveSafe(hostVoteStartButtonRoot, true);
                SetActiveSafe(clientVotePanelRoot, false);
                SetHostVoteStartInteractable(false);
            }
            else
            {
                SetActiveSafe(hostVoteStartButtonRoot, false);
                SetActiveSafe(clientVotePanelRoot, true);
                SetClientVoteInteractable(!_submitted);
            }

            ShowVoteTexts();
        }

        private void ApplySuccessState()
        {
            SetActiveSafe(readyOrStartButtonRoot, true);
            SetActiveSafe(hostVoteStartButtonRoot, false);
            SetActiveSafe(clientVotePanelRoot, false);
            SetClientVoteInteractable(false);
            _submitted = false;
            HideVoteTexts();
        }

        private void ApplyFailState()
        {
            bool singlePlayerHost = IsSinglePlayerHost();
            SetActiveSafe(readyOrStartButtonRoot, singlePlayerHost);

            if (IsHost)
            {
                SetActiveSafe(hostVoteStartButtonRoot, !singlePlayerHost);
                SetHostVoteStartInteractable(!singlePlayerHost && HasQuestSelection);
                SetActiveSafe(clientVotePanelRoot, false);
            }
            else
            {
                SetActiveSafe(hostVoteStartButtonRoot, false);
                SetActiveSafe(clientVotePanelRoot, true);
                SetClientVoteInteractable(false);
            }

            _submitted = false;
            HideVoteTexts();
        }

        private void HookButtons(bool bind)
        {
            if (hostVoteStartButton != null)
            {
                hostVoteStartButton.onClick.RemoveListener(OnClickHostVoteStart);
                if (bind) hostVoteStartButton.onClick.AddListener(OnClickHostVoteStart);
            }

            if (acceptButton != null)
            {
                acceptButton.onClick.RemoveListener(OnClickAccept);
                if (bind) acceptButton.onClick.AddListener(OnClickAccept);
            }

            if (rejectButton != null)
            {
                rejectButton.onClick.RemoveListener(OnClickReject);
                if (bind) rejectButton.onClick.AddListener(OnClickReject);
            }
        }

        private void SetHostVoteStartInteractable(bool value)
        {
            if (hostVoteStartButton != null)
                hostVoteStartButton.interactable = value;

            if (_hostVoteStartImage != null)
            {
                _hostVoteStartImage.color = value ? enabledColor : disabledColor;
                if (hostVoteStartButton != null && hostVoteStartButton.targetGraphic != null)
                    hostVoteStartButton.targetGraphic.color = _hostVoteStartImage.color;
            }
        }

        private void SetClientVoteInteractable(bool value)
        {
            if (acceptButton != null) acceptButton.interactable = value;
            if (rejectButton != null) rejectButton.interactable = value;

            if (_acceptImage != null)
            {
                _acceptImage.color = value ? clientVoteEnabledColor : clientVoteDisabledColor;
                if (acceptButton != null && acceptButton.targetGraphic != null)
                    acceptButton.targetGraphic.color = _acceptImage.color;
            }

            if (_rejectImage != null)
            {
                _rejectImage.color = value ? clientVoteEnabledColor : clientVoteDisabledColor;
                if (rejectButton != null && rejectButton.targetGraphic != null)
                    rejectButton.targetGraphic.color = _rejectImage.color;
            }
        }

        private static void SetActiveSafe(GameObject go, bool value)
        {
            if (go != null && go.activeSelf != value)
                go.SetActive(value);
        }

        private static bool IsSinglePlayerHost()
        {
            if (!NetworkServer.active || !NetworkClient.active)
                return false;

            foreach (var conn in NetworkServer.connections.Values)
            {
                if (conn == null) continue;
                if (conn.connectionId != 0) return false;
            }

            return true;
        }

        // [수정] 인스펙터 참조가 없을 때만 안전하게 탐색
        private void ResolveVoteTextRefs()
        {
            if (timerText != null && countText != null && stageNameText != null)
                return;

            TextMeshProUGUI[] allTexts = Resources.FindObjectsOfTypeAll<TextMeshProUGUI>();
            for (int i = 0; i < allTexts.Length; i++)
            {
                var t = allTexts[i];
                if (t == null) continue;
                if (!t.gameObject.scene.IsValid()) continue;
                string n = t.gameObject.name;

                if (timerText == null && n.Contains("타이머")) timerText = t;
                else if (countText == null && n.Contains("카운트")) countText = t;
                else if (stageNameText == null && n.Contains("투표중인 퀘스트")) stageNameText = t;
            }
        }

        // [수정] 투표 중 실시간 텍스트 갱신
        private void RefreshVoteTextsRuntime()
        {
            var vote = QuestVoteSystem.Instance;
            if (vote == null || !vote.VoteInProgress)
                return;

            ResolveVoteTextRefs();
            ValidateVoteTextRefs();

            if (timerText != null)
            {
                double remain = vote.VoteEndTime - NetworkTime.time;
                int sec = Mathf.Max(0, Mathf.CeilToInt((float)remain));
                timerText.text = $"Vote time left: {sec}s";
            }

            if (countText != null && countText != stageNameText)
                countText.text = $"Accept {vote.AcceptCount} / Reject {vote.RejectCount}";

            if (stageNameText != null)
                stageNameText.text = vote.SelectedStageName;
        }

        // [수정] 투표 중 텍스트 표시
        private void ShowVoteTexts()
        {
            ResolveVoteTextRefs();
            ValidateVoteTextRefs();

            if (timerText != null)
                timerText.gameObject.SetActive(true);

            if (countText != null && countText != stageNameText)
                countText.gameObject.SetActive(true);

            if (stageNameText != null)
            {
                stageNameText.gameObject.SetActive(true);
                var vote = QuestVoteSystem.Instance;
                if (vote != null)
                    stageNameText.text = vote.SelectedStageName;
            }
        }

        // [수정] 투표 전/후 텍스트 비표시
        private void HideVoteTexts()
        {
            ResolveVoteTextRefs();
            ValidateVoteTextRefs();

            if (timerText != null)
            {
                timerText.text = string.Empty;
                timerText.gameObject.SetActive(false);
            }

            if (countText != null && countText != stageNameText)
            {
                countText.text = string.Empty;
                countText.gameObject.SetActive(false);
            }

            if (stageNameText != null)
            {
                stageNameText.text = string.Empty;
                stageNameText.gameObject.SetActive(false);
            }
        }

        // [수정] stage/count가 같은 TMP를 참조하면 카운트 출력으로 스테이지 텍스트가 덮이는 문제 방지
        private void ValidateVoteTextRefs()
        {
            if (countText != null && stageNameText != null && countText == stageNameText)
            {
                if (!_loggedTextRefConflict)
                {
                    _loggedTextRefConflict = true;
                    Debug.LogError("[QuestVoteController] countText and stageNameText reference the same TMP. Assign different objects in Inspector.");
                }
            }
        }
    }
}
