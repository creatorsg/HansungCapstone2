using PlayFab;
using UnityEngine;
using UnityEngine.UI;

namespace inseon.Playfab.User.Login
{
    public class LoginWindow : MonoBehaviour
    {
        [SerializeField] private InputField _id;
        [SerializeField] private InputField _pw;
        [SerializeField] private GameObject _registerWindow;
        public void LoginWithPlayFab()
        {
            var ID = _id.text?.Trim();
            var PW = _pw.text;

            if (string.IsNullOrEmpty(ID) || string.IsNullOrEmpty(PW))
            {
                Debug.Log("Invalid Login Input");
                return;
            }

            PlayfabUserManage.Login(ID, PW,
                PlayfabUserManage.SuccessLogin,
                PlayfabUserManage.FailureLogin);
        }

        public void LoginWithGuest()
        {
            PlayfabUserManage.LoginAsGuest(PlayfabUserManage.SuccessLogin,
                PlayfabUserManage.FailureLogin);
        }

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