using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using insoen.Server.Playfab.Network;
using System;


namespace insoen.Server.Playfab.Login
{
    public class PlayFabAuthService : IAuthService
    {
        // 로그인 함수 [이메일(ID), 비밀번호, 성공 콜백, 실패 콜백]
        public void Login(
            string email,
            string password,
            Action<AuthResult> onSuccess,
            Action<string> onFailure)
        {
            var request = new LoginWithEmailAddressRequest
            {
                Email = email,
                Password = password
            };

            PlayFabClientAPI.LoginWithEmailAddress(
                request,
                _ => GetAccountInfo(email, onSuccess, onFailure),
                error => onFailure(error.ErrorMessage)
            );
        }


        public void GameLogout()
        {
            PlayFabClientAPI.ForgetAllCredentials();
        }

        public void Register(
            string email,
            string password,
            Action<AuthResult> onSuccess,
            Action<string> onFailure)
        {
            var request = new RegisterPlayFabUserRequest
            {
                Email = email,
                Password = password,
                RequireBothUsernameAndEmail = false
            };

            PlayFabClientAPI.RegisterPlayFabUser(
                request,
                _ => GetAccountInfo(email, onSuccess, onFailure),
                error => onFailure(error.ErrorMessage)
            );
        }

        private void GetAccountInfo(
            string email,
            Action<AuthResult> onSuccess,
            Action<string> onFailure)
        {
            PlayFabClientAPI.GetAccountInfo(
                new GetAccountInfoRequest(),
                result =>
                {
                    var info = result.AccountInfo;

                    string username =
                        string.IsNullOrEmpty(info.Username)
                            ? info.PrivateInfo?.Email ?? email
                            : info.Username;

                    string playFabId = info.PlayFabId;

                    onSuccess(new AuthResult(username, playFabId));
                },
                error => onFailure(error.ErrorMessage)
            );
        }
    }

}