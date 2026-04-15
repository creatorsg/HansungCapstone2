using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;

namespace Jun
{
    /// <summary>
    /// 아지트 스킬 강화 시설 UI.
    /// 캐릭터 SO + 현재 세이브를 읽어 스킬트리를 표시하고,
    /// 분기 버튼 클릭 시 SkillSaveSync에 Cmd 전송.
    /// </summary>
    public class SkillTreeUI : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject _panel;

        [Header("Character SO")]
        [SerializeField] private CharacterSkillSetSO _skillSet;

        [Header("Skill Info (4개 스킬)")]
        [SerializeField] private TextMeshProUGUI[] _skillNameTexts = new TextMeshProUGUI[4];
        [SerializeField] private TextMeshProUGUI[] _skillDescTexts = new TextMeshProUGUI[4];
        [SerializeField] private TextMeshProUGUI[] _skillCostTexts = new TextMeshProUGUI[4];
        [SerializeField] private Image[] _skillIcons = new Image[4];

        [Header("Tier2 버튼 (skillIdx * 2 + choice)")]
        [SerializeField] private Button[] _tier2Buttons = new Button[8]; // 0A,0B,1A,1B,2A,2B,3A,3B

        [Header("Tier3 버튼 (skillIdx * 2 + choice)")]
        [SerializeField] private Button[] _tier3Buttons = new Button[8];

        public void Open(CharacterSkillSetSO skillSet)
        {
            _skillSet = skillSet;
            _panel.SetActive(true);
            Refresh();
        }

        public void Close() => _panel.SetActive(false);

        public void Refresh()
        {
            if (_skillSet == null || SkillSaveSync.Instance == null) return;

            var save = SkillSaveSync.Instance.Find(_skillSet.characterName);
            if (save == null) save = new SkillTreeSaveData { characterName = _skillSet.characterName };

            for (int i = 0; i < 4; i++)
            {
                var current = SkillManager.GetCurrentSkill(_skillSet, save, i);
                if (_skillNameTexts[i] != null) _skillNameTexts[i].text = current.skillName;
                if (_skillDescTexts[i] != null) _skillDescTexts[i].text = current.description;
                if (_skillIcons[i] != null) _skillIcons[i].sprite = _skillSet.skills[i].icon;

                var branch = _skillSet.skills[i].branch;
                int t2 = save.tier2Choice[i];
                int t3 = save.tier3Choice[i];

                // Tier2 버튼 활성화 (미선택일 때만)
                _tier2Buttons[i * 2 + 0].interactable = (t2 == -1);
                _tier2Buttons[i * 2 + 1].interactable = (t2 == -1);

                // Tier3 버튼 활성화 (Tier2 선택됨 + Tier3 미선택)
                bool t3Available = (t2 != -1 && t3 == -1);
                _tier3Buttons[i * 2 + 0].interactable = t3Available;
                _tier3Buttons[i * 2 + 1].interactable = t3Available;

                // 비용 텍스트: 다음 강화 단계 비용
                if (_skillCostTexts[i] != null)
                {
                    if (t2 == -1)
                        _skillCostTexts[i].text = $"{branch.optionA.goldCost}G";
                    else if (t3 == -1)
                    {
                        var nextA = (t2 == 0) ? branch.optionA_A : branch.optionB_A;
                        _skillCostTexts[i].text = $"{nextA.goldCost}G";
                    }
                    else
                        _skillCostTexts[i].text = "MAX";
                }
            }
        }

        // 인스펙터에서 Button.onClick에 바인딩 (파라미터 지원용)
        public void OnClickTier2(int flatIndex) // skillIdx*2 + choice
        {
            if (_skillSet == null || SkillSaveSync.Instance == null) return;
            int skillIdx = flatIndex / 2;
            int choice = flatIndex % 2;
            SkillSaveSync.Instance.CmdUpgradeTier2(_skillSet.characterName, skillIdx, choice);
        }

        public void OnClickTier3(int flatIndex)
        {
            if (_skillSet == null || SkillSaveSync.Instance == null) return;
            int skillIdx = flatIndex / 2;
            int choice = flatIndex % 2;
            SkillSaveSync.Instance.CmdUpgradeTier3(_skillSet.characterName, skillIdx, choice);
        }

        private void OnEnable()
        {
            if (SkillSaveSync.Instance != null)
                SkillSaveSync.Instance.saves.OnChange += OnSavesChanged;
        }

        private void OnDisable()
        {
            if (SkillSaveSync.Instance != null)
                SkillSaveSync.Instance.saves.OnChange -= OnSavesChanged;
        }

        private void OnSavesChanged(SyncList<SkillTreeSaveData>.Operation op, int index, SkillTreeSaveData item)
        {
            if (_panel.activeSelf) Refresh();
        }
    }
}
