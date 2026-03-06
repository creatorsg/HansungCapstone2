using PlayFab;
using UnityEngine;
using UnityEngine.UI;

namespace inseon.Playfab.User.Login
{
    public class LoginWindow : MonoBehaviour
    {
        [SerializeField] private InputField id;
        [SerializeField] private InputField pw;

        public void LoginWithPlayFab()
        {
            var ID = id.text?.Trim();
            var PW = pw.text;

            PlayfabUserManage.Login(ID, PW, 
                PlayfabUserManage.SuccessLogin, 
                PlayfabUserManage.FailureLogin);
        }
    }
}