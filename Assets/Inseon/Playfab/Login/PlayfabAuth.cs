using System;
using PlayFab;
using PlayFab.ClientModels;

namespace inseon.Playfab.Login
{
    public static class PlayfabAuth
    {
        public static void LoginWithPlayFab(
            string id,
            string pw,
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

        public static bool IsLoggedIn()
        {
            return PlayFabClientAPI.IsClientLoggedIn();
        }
    }
}
