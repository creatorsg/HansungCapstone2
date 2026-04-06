using PlayFab;
using PlayFab.ClientModels;
using System;
using System.Collections.Generic;
using UnityEngine;

public static class PlayfabCommand
{
    static void ExecuteCloudScript(string functionName, object args, Action<object> onSuccess)
    {
        var request = new ExecuteCloudScriptRequest
        {
            FunctionName = functionName,
            FunctionParameter = args,
            GeneratePlayStreamEvent = false
        };

        PlayFabClientAPI.ExecuteCloudScript(
            request,
            result =>
            {
                if (result.Error != null)
                {
                    Debug.LogError($"CloudScript Error ({functionName}) : {result.Error.Message}");
                    return;
                }

                onSuccess?.Invoke(result.FunctionResult);
            },
            error =>
            {
                Debug.LogError(error.GenerateErrorReport());
            });
    }

    public static void InitAndGetProfile(Action<object> callback)
    {
        ExecuteCloudScript(
            "PlayerProfileLoad",
            null,
            callback
        );
    }

    public static void CheckPlayerCharacterData(Action<object> callback)
    {
        ExecuteCloudScript(
            "LoadCharacterState",
            null,
            callback
        );
    }

    // ========================= Room =========================

    public static void CreateRoom(
    string roomId,
    string roomName,
    string ip,
    int port,
    int maxPlayers,
    bool isPrivate,
    string password,
    string sessionId)
    {
        PlayFabClientAPI.ExecuteCloudScript(
            new ExecuteCloudScriptRequest
            {
                FunctionName = "CreateRoom",
                FunctionParameter = new
                {
                    roomId = roomId,
                    roomName = roomName,
                    ip = ip,
                    port = port,
                    maxPlayers = maxPlayers,
                    isPrivate = isPrivate,
                    password = password,
                    sessionId = sessionId  
                }
            },
            r => Debug.Log("Room registered"),
            e => Debug.LogError(e.GenerateErrorReport())
        );
    }

    public static void GetRooms(Action<string> onResult)
    {
        var request = new ExecuteCloudScriptRequest
        {
            FunctionName = "GetRoomList"
        };

        PlayFabClientAPI.ExecuteCloudScript(
            request,
            r =>
            {
                if (r.FunctionResult != null)
                    onResult?.Invoke(r.FunctionResult.ToString());
            },
            e => Debug.LogError(e.GenerateErrorReport())
        );
    }

    public static void JoinRoom(string roomId, string password, Action<RoomInfo> onSuccess)
    {
        ExecuteCloudScript(
            "JoinRoom",
            new { roomId = roomId, password = password },
            result =>
            {
                var json = result.ToString();
                var room = Newtonsoft.Json.JsonConvert.DeserializeObject<RoomInfo>(json);
                onSuccess?.Invoke(room);
            }
        );
    }

    public static void RemoveRoom(string roomId)
    {
        var request = new ExecuteCloudScriptRequest
        {
            FunctionName = "RemoveRoom",
            FunctionParameter = new
            {
                roomId = roomId
            }
        };

        PlayFabClientAPI.ExecuteCloudScript(request, null, null);
    }

    public static void LoadCharacterCatalog(System.Action onComplete)
    {
        PlayFabClientAPI.GetCatalogItems(
            new GetCatalogItemsRequest { CatalogVersion = "Characters" },
            result =>
            {
                CharacterDatabase.Load(result.Catalog);
                onComplete?.Invoke();
            },
            error => Debug.LogError("Catalog Load Failed: " + error.GenerateErrorReport())
        );
    }

}
