using Mirror;
using Newtonsoft.Json.Linq;
using PlayFab;
using PlayFab.ClientModels;
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
            _player.initPlayerData(nickname, result.PlayFabId);

            PlayfabCommand.InitAndGetProfile(profile =>
            {
                PlayfabCommand.CheckPlayerCharacterData(chars =>
                {
                    // CloudScript 반환: { "characterState": { "C001": true, "C002": false, ... } }
                    // 결과를 Player 세션에 캐싱해 캐릭터 선택 씬에서 재사용합니다.
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

            GameObject g = new GameObject("PlayerSession");
            _player = g.AddComponent<Player>();

            UnityEngine.Object.DontDestroyOnLoad(g);
        }
    }
}
