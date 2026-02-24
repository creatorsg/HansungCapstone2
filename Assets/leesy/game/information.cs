using TMPro;
using UnityEngine;

public class UpdateInformation : MonoBehaviour
{
    [SerializeField] private TMP_Text information;
    public void ChangeText(string newString)
    {
        information.text = newString;
    }
}
