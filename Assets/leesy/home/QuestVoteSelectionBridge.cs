using System;
using Jun;
using Mirror;
using UnityEngine;

namespace Lsy
{
    // [수정] 퀘스트 버튼 클릭 상태를 투표 게이트로 전달하는 브리지.
    public class QuestVoteSelectionBridge : MonoBehaviour
    {
        public static QuestVoteSelectionBridge Instance { get; private set; }
        public static event Action OnSelectionChanged;

        [SerializeField] private QuestData[] allQuests;

        public bool HasSelection => SelectedQuest.Current != null;
        public QuestData CurrentQuest => SelectedQuest.Current;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                return;
            }

            if (Instance != this)
                enabled = false;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void OnEnable()
        {
            DistrictHover.OnDistrictClicked += OnDistrictClicked;
            OnSelectionChanged?.Invoke();
        }

        private void OnDisable()
        {
            DistrictHover.OnDistrictClicked -= OnDistrictClicked;
        }

        private void OnDistrictClicked(DistrictType type)
        {
            // [수정] 투표 진행 중에는 지역 변경 잠금
            if (QuestVoteSystem.Instance != null && QuestVoteSystem.Instance.VoteInProgress)
                return;

            QuestData quest = FindQuest(type);
            if (quest == null)
            {
                Debug.LogWarning($"[QuestVoteSelectionBridge] No QuestData for {type}.");
                return;
            }

            SelectedQuest.Current = quest;

            if (NetworkServer.active)
            {
                var rm = NetworkManager.singleton as GameRoomManager;
                if (rm != null && !string.IsNullOrWhiteSpace(quest.battleSceneName))
                    rm.GameplayScene = quest.battleSceneName;
            }

            OnSelectionChanged?.Invoke();
        }

        private QuestData FindQuest(DistrictType type)
        {
            if (allQuests == null) return null;

            for (int i = 0; i < allQuests.Length; i++)
            {
                QuestData q = allQuests[i];
                if (q != null && q.districtType == type)
                    return q;
            }

            return null;
        }
    }
}
