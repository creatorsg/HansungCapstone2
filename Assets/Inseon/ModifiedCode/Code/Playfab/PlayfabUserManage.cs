using inseon.Core;
using Mirror;
using Newtonsoft.Json.Linq;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.Json;
using System;
using System.Collections.Generic;
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

        [Serializable]
        public class CloudScriptFailure
        {
            public string error;
            public string message;
            public string stack;
        }

        public static void InitializePlayerData(
            Action<string> onOkJson,
            Action<CloudScriptFailure> onCloudScriptError,
            Action<PlayFabError> onTransportError)
        {
            var req = new ExecuteCloudScriptRequest
            {
                FunctionName = "PlayerProfileLoad",
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

        public static void RegisterAndLogin(
            string id, string pw, string nickname,
            Action<string> onStateChange,
            Action<string> onFail)
        {
            Register(id, pw, nickname,
                onOk: _ =>
                {
                    onStateChange?.Invoke("정보 처리 중...");
                    InitializePlayerData(
                        onOkJson: json =>
                        {
                            onStateChange?.Invoke("로그인 중...");
                            Login(id, pw, SuccessLogin,
                                err =>
                                {
                                    Debug.LogError(err.GenerateErrorReport());
                                    onFail?.Invoke("자동 로그인 실패. 다시 로그인해주세요.");
                                });
                        },
                        onCloudScriptError: csErr =>
                        {
                            onFail?.Invoke($"서버 초기화 실패: {csErr.message}");
                        },
                        onTransportError: pfErr =>
                        {
                            Debug.LogError(pfErr.GenerateErrorReport());
                            onFail?.Invoke("네트워크/인증 오류로 초기화 실패");
                        }
                    );
                },
                onError: e =>
                {
                    Debug.LogError(e.GenerateErrorReport());
                    onFail?.Invoke("가입 실패");
                }
            );
        }

        public static void LoginAsGuest(
            Action<LoginResult> onOk,
            Action<PlayFabError> onError)
        {
            string customId = SystemInfo.deviceUniqueIdentifier; 

            var request = new LoginWithCustomIDRequest
            {
                CustomId = customId,
                CreateAccount = true,
                InfoRequestParameters = new GetPlayerCombinedInfoRequestParams
                {
                    GetPlayerProfile = true
                }
            };

            PlayFabClientAPI.LoginWithCustomID(request, result =>
            {
                if (result.NewlyCreated)
                {
                    SetGuestDisplayName(result, onOk, onError);
                }
                else
                {
                    onOk?.Invoke(result);
                }
            }, onError);
        }

        private static void SetGuestDisplayName(
            LoginResult loginResult,
            Action<LoginResult> onOk,
            Action<PlayFabError> onError)
        {
            string randomNum = UnityEngine.Random.Range(0, 1000).ToString("D3"); 
            string displayName = $"Guest{randomNum}";

            var request = new UpdateUserTitleDisplayNameRequest
            {
                DisplayName = displayName
            };

            PlayFabClientAPI.UpdateUserTitleDisplayName(request, _ =>
            {
                onOk?.Invoke(loginResult); 
            }, onError);
        }

        public static void SuccessLogin(LoginResult result)
        {
            LoadPlayer();
            var nickname = result.InfoResultPayload?.PlayerProfile?.DisplayName;
            if (string.IsNullOrEmpty(nickname)) nickname = result.PlayFabId;
            _player.Initialize(nickname, result.PlayFabId);

            PlayfabCommand.InitAndGetProfile(profile =>
            {
                PlayfabCommand.CheckPlayerCharacterData(chars =>
                {
                    if (chars != null)
                    {
                        try
                        {
                            var root  = JObject.Parse(chars.ToString());
                            var state = root["characterState"];
                            if (state != null)
                            {
                                var owned = state.ToObject<Dictionary<string, bool>>();
                                _player.SetOwnedCharacters(owned);
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.LogWarning("[PlayfabUserManage] 캐릭터 보유 데이터 파싱 실패: " + e.Message);
                        }
                    }

                    PlayfabCommand.LoadCharacterCatalog(() =>
                    {
                        Debug.Log("모든 초기 데이터 로드 완료 → 로비 이동");
                        SceneManager.LoadScene("Lobby");
                    });
                });
            });
        }

        public static void FailureLogin(PlayFabError err)
        {
            Debug.LogError("Login Failed : " + err.ErrorMessage);
        }

        private static void LoadPlayer()
        {
            if (_player != null)
                return;

            _player = new Player();
        }

        public static void Logout()
        {
            PlayFabClientAPI.ForgetAllCredentials(); 
            _player = null;                          
        }
    }
}
