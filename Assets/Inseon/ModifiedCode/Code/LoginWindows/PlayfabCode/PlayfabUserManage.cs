using PlayFab;
using PlayFab.ClientModels;
using System;
using UnityEngine;

namespace inseon.Playfab.User
{
    public static class PlayfabUserManage
    {
        private static Player _player;

        public static void Login(
            string id, string pw, 
            Action<LoginResult> onOk,
            Action<PlayFabError> onError)
        {
            var request = new LoginWithPlayFabRequest
            {
                Username = id,
                Password = pw
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

            string nickname = result.InfoResultPayload?.PlayerProfile?.DisplayName;
            _player.initPlayerData(nickname);

            inseon.Playfab.Login.PlayFabUserProfile.InitAndGetProfile(
                onOk: data =>
                {
                    
                },
                onError: err => Debug.LogError(err.GenerateErrorReport())
            );


        }

        public static void FailureLogin(PlayFabError err)
        {
            Debug.LogError(err.GenerateErrorReport());
        }

        private static void LoadPlayer()
        {
            GameObject g = new GameObject("Player");
            _player = g.AddComponent<Player>();

            UnityEngine.Object.DontDestroyOnLoad(g);

        }
    }
}
