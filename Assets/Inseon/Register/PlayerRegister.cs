using PlayFab;
using PlayFab.ClientModels;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using inseon.Server.Playfab.UserRegister;

namespace inseon.Server.Playfab.UserRegister
{
    public class PlayFabRegister : MonoBehaviour
    {
        public InputField IdInput;
        public InputField PwInput;
        public InputField CheckPwInput;
        public InputField NameInput;

        private UserRegisterService _registerService;
        private void Awake()
        {
            _registerService = new UserRegisterService();
        }

        public void Register()
        {
            if(PwInput.text != CheckPwInput.text)
            {
                Debug.LogError("비밀번호가 일치하지 않습니다.");
                return;
            }

            _registerService.PlayfabUserRegister(IdInput.text, PwInput.text, NameInput.text);
        }
    }
}
