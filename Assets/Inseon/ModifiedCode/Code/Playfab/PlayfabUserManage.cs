using PlayFab;
using PlayFab.ClientModels;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace inseon.Playfab.User
{
    public static class PlayfabUserManage
    {
        private static Player _player;
        public static Player Player => _player;

        public static void Login(
            string id, string pw, 
            Action<LoginResult> onOk,
            Action<PlayFabError> onError)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(pw))
            {
                Debug.Log("ID / PW empty");
                return;
            }

            var request = new LoginWithPlayFabRequest
            {
                Username = id,
                Password = pw,
                InfoRequestParameters = new GetPlayerCombinedInfoRequestParams
                {
                    GetPlayerProfile = true
                }
            };

            PlayFabClientAPI.LoginWithPlayFab(request, onOk, onError);
        }

        public static void Register(
            string id, string pw, string nickname,
            Action<RegisterPlayFabUserResult> onOk,
            Action<PlayFabError> onError)
        {
            var request = new RegisterPlayFabUserRequest
            {
                Username = id,
                Password = pw,
                DisplayName = nickname,
                RequireBothUsernameAndEmail = false
            };

            PlayFabClientAPI.RegisterPlayFabUser(request, onOk, onError);
        }

        public static void SuccessLogin(LoginResult result)
        {
            LoadPlayer();

            var nickname = result.InfoResultPayload?.PlayerProfile?.DisplayName;

            if (string.IsNullOrEmpty(nickname))
                nickname = result.PlayFabId;

            _player.initPlayerData(nickname, result.PlayFabId);

            PlayfabCommand.InitAndGetProfile(profile =>
            {
                PlayfabCommand.CheckPlayerCharacterData(chars =>
                {
                    Debug.Log("Login Complete");
                });
            });

            SceneManager.LoadScene("Lobby");
        }

        public static void FailureLogin(PlayFabError err)
        {
            Debug.LogError("Login Failed : " + err.ErrorMessage);
        }

        private static void LoadPlayer()
        {
            if (_player != null)
                return;

            GameObject g = new GameObject("Player");
            _player = g.AddComponent<Player>();

            UnityEngine.Object.DontDestroyOnLoad(g);
        }
    }
}
