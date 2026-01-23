using PlayFab;
using PlayFab.ClientModels;
using System.Collections.Generic;
using UnityEngine;

public class PlayFabRegister : MonoBehaviour
{
    public void Register(string id, string pw, string nickname)
    {
        var request = new RegisterPlayFabUserRequest
        {
            Username = id,
            Password = pw,
            DisplayName = nickname,
            RequireBothUsernameAndEmail = false
        };

        PlayFabClientAPI.RegisterPlayFabUser(
            request,
            OnRegisterSuccess,
            OnRegisterFail
        );
    }

    private void OnRegisterSuccess(RegisterPlayFabUserResult result)
    {
        Debug.Log("회원가입 성공");

        InitUserData();
    }

    private void OnRegisterFail(PlayFabError error)
    {
        Debug.LogError(error.GenerateErrorReport());
    }

    private void InitUserData()
    {
        var data = new Dictionary<string, string>
    {
        { "Level", "1" },
        { "PlayGame", "0" },
        { "Rank", "Bronze" }
    };

        PlayFabClientAPI.UpdateUserData(
            new UpdateUserDataRequest
            {
                Data = data
            },
            result => Debug.Log("초기 데이터 생성 완료"),
            error => Debug.LogError(error.GenerateErrorReport())
        );
    }

}
