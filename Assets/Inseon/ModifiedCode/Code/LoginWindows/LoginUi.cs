using UnityEngine;

namespace inseon.LoginWindows.Login.UI
{
    public class LoginUi : MonoBehaviour
    {
        [field: SerializeField] private GameObject _registerWindow;

        public void OpenRegisterWindow()
        {
            _registerWindow.SetActive(true);
        }

        public void CloseRegisterWindow()
        {
            _registerWindow.SetActive(false);
        }

    }
}
