using System;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.Json;

namespace inseon.Playfab.Register.Authentication
{
    public static class PlayfabRegister
    {
        [Serializable]
        public class CloudScriptFailure
        {
            public string error;    
            public string message;  
            public string stack;    
        }

        public static void RegisterPlayFabUser(
            string id, 
            string pw, 
            string nickname,
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

        public static void RegisterPlayerNickname(
            string nickname, 
            Action<UpdateUserTitleDisplayNameResult> onOk, 
            Action<PlayFabError> onError)
        {
            var request = new UpdateUserTitleDisplayNameRequest
            {
                DisplayName = nickname
            };

            PlayFabClientAPI.UpdateUserTitleDisplayName(request, onOk, onError);
        }

        public static void InitializePlayerData(
            Action<string> onOkJson,
            Action<CloudScriptFailure> onCloudScriptError,
            Action<PlayFabError> onTransportError)
        {
            var req = new ExecuteCloudScriptRequest
            {
                FunctionName = "InitPlayerData",
                FunctionParameter = new { },
                GeneratePlayStreamEvent = true
            };

            PlayFabClientAPI.ExecuteCloudScript(req,
                result =>
                {
                    if (result.Error != null)
                    {
                        var failure = new CloudScriptFailure
                        {
                            error = result.Error.Error,
                            message = result.Error.Message,
                            stack = result.Error.StackTrace
                        };

                        Debug.LogError($"CloudScript error: {failure.error} - {failure.message}");
                        onCloudScriptError?.Invoke(failure);
                        return;
                    }

                    var json = result.FunctionResult != null
                        ? PlayFabSimpleJson.SerializeObject(result.FunctionResult)
                        : "{}";

                    onOkJson?.Invoke(json);
                },
                onTransportError
            );
        }
    }
}
