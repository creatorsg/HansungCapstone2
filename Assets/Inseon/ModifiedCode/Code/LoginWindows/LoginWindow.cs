using inseon.Playfab.User;
using PlayFab;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace inseon.LoginWindows.Login.Login
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

            PlayfabUserManage.Login(ID, PW,
                PlayfabUserManage.SuccessLogin,
                PlayfabUserManage.FailureLogin);
        }

        public void LoginWithGuest()
        {
            PlayfabUserManage.LoginAsGuest(PlayfabUserManage.SuccessLogin,
                PlayfabUserManage.FailureLogin);
        }
    }
}