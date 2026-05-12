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

    [Header("Scene")]
    [SerializeField] private string battleSceneName = "GamePlay";

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
        SelectedQuest.Current = _currentQuest;

        // TODO: 추후 SceneManager.LoadScene(battleSceneName)으로 교체
        Debug.Log($"[QuestInfoPanel] Start → {_currentQuest.districtType} / {_currentQuest.stageName} (씬 전환 예정: {battleSceneName})");
    }
}
