using TMPro;
using UnityEngine;

namespace Lsy
{
    public class UpdateInformation : MonoBehaviour
    {
        [SerializeField] private TMP_Text information;
        public void ChangeText(string newString)
        {
            information.text = newString;
        }
    }
}
