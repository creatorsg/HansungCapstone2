using Mirror;
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
                ReadyOrStartButton ready = FindObjectOfType<ReadyOrStartButton>(true);
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

            // [수정] 참조 실수(부모-자식) 경고
            if (hostVoteStartButtonRoot != null && clientVotePanelRoot != null)
            {
                if (clientVotePanelRoot.transform.IsChildOf(hostVoteStartButtonRoot.transform) ||
                    hostVoteStartButtonRoot.transform.IsChildOf(clientVotePanelRoot.transform))
                {
                    Debug.LogWarning("[QuestVoteController] hostVoteStartButtonRoot/clientVotePanelRoot is parent-child. Split them.");
                }
            }
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
            RefreshState();
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
            SetActiveSafe(readyOrStartButtonRoot, lockReadyStartUntilVotePass ? false : true);

            if (IsHost)
            {
                SetActiveSafe(hostVoteStartButtonRoot, true);
                SetActiveSafe(clientVotePanelRoot, false);
                SetHostVoteStartInteractable(HasQuestSelection);
            }
            else
            {
                SetActiveSafe(hostVoteStartButtonRoot, false);
                SetActiveSafe(clientVotePanelRoot, true);
                SetClientVoteInteractable(false);
            }

            _submitted = false;
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
        }

        private void ApplySuccessState()
        {
            SetActiveSafe(readyOrStartButtonRoot, true);
            SetActiveSafe(hostVoteStartButtonRoot, false);
            SetActiveSafe(clientVotePanelRoot, false);
            SetClientVoteInteractable(false);
            _submitted = false;

            // [수정] 투표 성공 직후 게임 시작 버튼 상태 재평가 강제
            if (readyOrStartButtonRoot != null)
            {
                ReadyOrStartButton ready = readyOrStartButtonRoot.GetComponent<ReadyOrStartButton>();
                if (ready != null)
                    ready.RefreshHostInteractableNow();
            }
        }

        private void ApplyFailState()
        {
            SetActiveSafe(readyOrStartButtonRoot, false);

            if (IsHost)
            {
                SetActiveSafe(hostVoteStartButtonRoot, true);
                SetHostVoteStartInteractable(HasQuestSelection);
                SetActiveSafe(clientVotePanelRoot, false);
            }
            else
            {
                SetActiveSafe(hostVoteStartButtonRoot, false);
                SetActiveSafe(clientVotePanelRoot, true);
                SetClientVoteInteractable(false);
            }

            _submitted = false;
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
    }
}
