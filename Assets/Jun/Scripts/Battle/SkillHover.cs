using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // UI 호버링 이벤트를 위해 반드시 추가해야 합니다.
namespace Jun
{
    public class SkillHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Button _skillBTN; //스킬 버튼 
        [SerializeField] private GameObject _infoPanel; //스킬 정보 창
        [SerializeField] private TextMeshProUGUI _nameText; //스킬 이름 텍스트
        [SerializeField] private TextMeshProUGUI _infoText; //스킬 정보 텍스트

        private string _currentSkillName;
        private string _currentSkillDesc;
        private bool _hasSkill = false; // 빈 슬롯인지 확인용

        private void Start()
        {
            _infoPanel.SetActive(false);
        }

        // BattleManager에서 이 함수를 불러서 새로운 정보를 주입
        public void SetSkillInfo(string skillName, string skillDesc)
        {
            _currentSkillName = skillName;
            _currentSkillDesc = skillDesc;
            _hasSkill = !string.IsNullOrEmpty(skillName); // 이름이 비어있지 않다면 스킬이 있는 것으로 판정
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_hasSkill) return; // 정보가 없으면 창을 띄우지 않음

            _infoPanel.SetActive(true);
            _nameText.text = _currentSkillName;
            _infoText.text = _currentSkillDesc;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _infoPanel.SetActive(false);
            _infoText.text = "";
        }
    }
}