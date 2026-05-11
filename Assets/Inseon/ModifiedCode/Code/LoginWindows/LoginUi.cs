using inseon.Playfab.User;
using PlayFab;
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

        public void OnQuitGame()
        {
            if (PlayFabClientAPI.IsClientLoggedIn())
                PlayfabUserManage.Logout();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
        }
    }
}
