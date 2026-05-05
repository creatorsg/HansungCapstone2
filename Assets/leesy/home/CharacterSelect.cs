using UnityEngine;

namespace Lsy
{
    public class UI_CharacterSelect : MonoBehaviour
    {
        public void OnClickSwapButton(int index)
        {
            if (PlayerAccount.LocalInstance != null)
            {
                PlayerAccount.LocalInstance.SelectCharacter(index);
            }
            else
            {
                Debug.LogWarning("¿À·ù");
            }
        }
    }
}