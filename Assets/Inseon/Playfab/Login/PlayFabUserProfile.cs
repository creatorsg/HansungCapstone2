using System;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.Json;

namespace inseon.Playfab.Login
{
    public static class PlayFabUserProfile
    {
        public static void InitAndGetProfile(Action<InitProfileResponse> onOk, Action<PlayFabError> onError)
        {
            var req = new ExecuteCloudScriptRequest
            {
                FunctionName = "InitAndGetProfile",
                FunctionParameter = new { },         
                GeneratePlayStreamEvent = true
            };

            PlayFabClientAPI.ExecuteCloudScript(req,
                result =>
                {
                    var json = PlayFabSimpleJson.SerializeObject(result.FunctionResult);
                    var data = PlayFabSimpleJson.DeserializeObject<InitProfileResponse>(json);

                    if (data == null || data.ok == false)
                    {
                        Debug.LogWarning("InitAndGetProfile returned invalid result: " + json);
                    }

                    onOk?.Invoke(data);
                },
                onError
            );
        }

        [Serializable]
        public class InitProfileResponse
        {
            public bool ok;
            public bool alreadyInit;
            public bool initialized;
            public int revision;
            public Profile profile;
        }

        [Serializable]
        public class Profile
        {
            public string Rank;
            public string PlayCount;
            public string ClearCount;
        }
    }
}
