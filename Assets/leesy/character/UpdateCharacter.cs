using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MyProject.UI.CharacterSelect
{
    public class UpdateCharacter : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image _characterImage;
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _descriptionText;
        [SerializeField] private TextMeshProUGUI _skillText;

        [Header("Confirmation UI")]
        public GameObject confirmButton;

        public void UpdateCharacterUI(/* 정보는 이 함수에서 받습니다 */)
        {
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            if (confirmButton != null)
                confirmButton.SetActive(true);

            _nameText.text = "캐릭터";

            Debug.Log("새로운 캐릭터 정보로 UI 교체 완료");
        }
    }
}
