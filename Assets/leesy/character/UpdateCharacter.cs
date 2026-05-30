using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Lsy
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

        public void UpdateCharacterUI(/* ������ �� �Լ����� �޽��ϴ� */)
        {
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            if (confirmButton != null)
                confirmButton.SetActive(true);

            _nameText.text = "ĳ����";

            Debug.Log("���ο� ĳ���� ������ UI ��ü �Ϸ�");
        }
    }
}
