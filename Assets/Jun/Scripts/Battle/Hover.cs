using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; 
namespace Jun
{
    public class Hover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Button _skillBTN; 
        [SerializeField] private GameObject _infoPanel;
        [SerializeField] private TextMeshProUGUI _nameText; 
        [SerializeField] private TextMeshProUGUI _infoText;

        private string _currentName;
        private string _currentDesc;
        private bool _hasSkill = false; 

        private void Start()
        {
            _infoPanel.SetActive(false);
        }

    
        public void SetInfo(string Name, string Desc)
        {
            _currentName = Name;
            _currentDesc = Desc;
            _hasSkill = !string.IsNullOrEmpty(Name); 
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_hasSkill) return; 

            _infoPanel.SetActive(true);
            _nameText.text = _currentName;
            _infoText.text = _currentDesc;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _infoPanel.SetActive(false);
            _infoText.text = "";
        }
    }
}