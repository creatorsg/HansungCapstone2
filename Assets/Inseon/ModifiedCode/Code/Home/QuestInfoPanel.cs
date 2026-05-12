using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Home 씬 우측 패널. DistrictHover의 정적 이벤트를 구독해
/// 클릭된 영지의 QuestData를 UI에 갱신하고, Start 버튼으로 전투 씬 전환.
/// </summary>
public class QuestInfoPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject contentRoot;
    [SerializeField] private TMP_Text stageNameText;
    [SerializeField] private TMP_Text questDescText;
    [SerializeField] private TMP_Text clearConditionText;
    [SerializeField] private TMP_Text recommendConditionText;
    [SerializeField] private Image[] enemyPortraitSlots;
    [SerializeField] private Button startButton;

    [Header("Quest Database (5개)")]
    [SerializeField] private QuestData[] allQuests;

    private QuestData _currentQuest;

    void OnEnable()
    {
        DistrictHover.OnDistrictClicked += HandleDistrictClicked;
        if (startButton != null)
            startButton.onClick.AddListener(OnStartPressed);
        if (contentRoot != null)
            contentRoot.SetActive(false);
    }

    void OnDisable()
    {
        DistrictHover.OnDistrictClicked -= HandleDistrictClicked;
        if (startButton != null)
            startButton.onClick.RemoveListener(OnStartPressed);
    }

    void HandleDistrictClicked(DistrictType type)
    {
        var quest = FindQuest(type);
        if (quest == null)
        {
            Debug.LogWarning($"[QuestInfoPanel] {type}에 대응하는 QuestData가 allQuests에 없음");
            return;
        }

        _currentQuest = quest;
        UpdateUI(quest);
        if (contentRoot != null) contentRoot.SetActive(true);
    }

    QuestData FindQuest(DistrictType type)
    {
        if (allQuests == null) return null;
        foreach (var q in allQuests)
            if (q != null && q.districtType == type) return q;
        return null;
    }

    void UpdateUI(QuestData quest)
    {
        if (stageNameText != null)         stageNameText.text = quest.stageName;
        if (questDescText != null)         questDescText.text = quest.questDescription;
        if (clearConditionText != null)    clearConditionText.text = quest.clearCondition;
        if (recommendConditionText != null) recommendConditionText.text = quest.recommendCondition;

        if (enemyPortraitSlots == null) return;
        for (int i = 0; i < enemyPortraitSlots.Length; i++)
        {
            var slot = enemyPortraitSlots[i];
            if (slot == null) continue;

            bool has = quest.enemyPortraits != null
                       && i < quest.enemyPortraits.Length
                       && quest.enemyPortraits[i] != null;

            slot.enabled = has;
            if (has) slot.sprite = quest.enemyPortraits[i];
        }
    }

    void OnStartPressed()
    {
        if (_currentQuest == null) return;

        if (string.IsNullOrEmpty(_currentQuest.battleSceneName))
        {
            Debug.LogWarning($"[QuestInfoPanel] {_currentQuest.districtType} 의 battleSceneName 이 비어있음");
            return;
        }

        SelectedQuest.Current = _currentQuest;

        // 호스트/서버 : Mirror 정식 흐름으로 모든 클라이언트 씬 동기 전환
        if (NetworkServer.active)
        {
            NetworkManager.singleton.ServerChangeScene(_currentQuest.battleSceneName);
            return;
        }

        // 클라이언트 : 호스트만 시작 가능 (leesy의 Ready 시스템과 동일 규칙)
        if (NetworkClient.isConnected)
        {
            Debug.LogWarning("[QuestInfoPanel] 클라이언트는 직접 씬 전환 불가 — 호스트가 시작해야 함");
            return;
        }

        // 네트워크 미연결 : 오프라인 테스트 폴백
        Debug.Log($"[QuestInfoPanel] 오프라인 모드 — SceneManager.LoadScene({_currentQuest.battleSceneName})");
        SceneManager.LoadScene(_currentQuest.battleSceneName);
    }
}
