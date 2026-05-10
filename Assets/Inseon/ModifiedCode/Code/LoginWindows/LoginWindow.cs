using inseon.Playfab.User;
using PlayFab;
using TMPro;
using UnityEngine;

namespace inseon.LoginWindows.Login
{
    public class LoginWindow : MonoBehaviour
    {
        [SerializeField] private TMP_InputField _id;
        [SerializeField] private TMP_InputField _pw;

        public void LoginWithPlayFab()
        {
            var ID = _id.text?.Trim();
            var PW = _pw.text;

            if (string.IsNullOrEmpty(ID) || string.IsNullOrEmpty(PW))
            {
                Debug.Log("Invalid Login Input");
                return;
            }

            if (!ButtonGuard.TryLock()) return;

            PlayfabUserManage.Login(ID, PW,
                onOk: PlayfabUserManage.SuccessLogin,
                onError: error =>
                {
                    PlayfabUserManage.FailureLogin(error);
                    ButtonGuard.Unlock();   
                });
        }

        public void LoginWithGuest()
        {
            if (!ButtonGuard.TryLock()) return;

            PlayfabUserManage.LoginAsGuest(
                onOk: PlayfabUserManage.SuccessLogin,
                onError: err =>
                {
                    PlayfabUserManage.FailureLogin(err);
                    ButtonGuard.Unlock();   
                });
        }
    }
}