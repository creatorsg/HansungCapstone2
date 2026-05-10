using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;
using UnityEngine.UI;

namespace inseon.Server.Playfab.Login
{
    public class PlayerLogin : MonoBehaviour
    {
        [SerializeField] private InputField id;
        [SerializeField] private InputField pw;

        public void LoginWithPlayFab()
        {
            var ID = id.text?.Trim();
            var PW = pw.text;

            inseon.Playfab.Login.PlayfabAuth.LoginWithPlayFab(ID, PW, OnLoginSuccess, OnError);
        }

        private void OnLoginSuccess(LoginResult result)
        {
            Debug.Log("Login OK. SessionTicket: " + result.SessionTicket);

            inseon.Playfab.Login.PlayFabUserProfile.InitAndGetProfile(
                onOk: data =>
                {
                    Debug.Log("Profile OK: " + (data?.profile?.Rank ?? "null"));
                    // TODO: UI 반영
                },
                onError: err => Debug.LogError(err.GenerateErrorReport())
            );
        }

        private void OnError(PlayFabError err)
        {
            Debug.LogError(err.GenerateErrorReport());
        }
    }
}