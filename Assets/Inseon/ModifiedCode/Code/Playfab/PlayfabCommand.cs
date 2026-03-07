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
            "InitAndGetProfile",
            null,
            callback
        );
    }

    public static void CheckPlayerCharacterData(Action<object> callback)
    {
        ExecuteCloudScript(
            "CheckPlayerCharacterData",
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
        string password,
        Action<object> callback)
    {
        var args = new Dictionary<string, object>()
        {
            { "roomId", roomId },
            { "roomName", roomName },
            { "ip", ip },
            { "port", port },
            { "maxPlayers", maxPlayers },
            { "password", password } 
        };

        ExecuteCloudScript(
            "CreateRoom",
            args,
            callback
        );
    }

    public static void GetRoomList(Action<object> callback)
    {
        ExecuteCloudScript(
            "GetRoomList",
            null,
            callback
        );
    }

    public static void GetRoomById(string roomId, Action<object> callback)
    {
        var args = new Dictionary<string, object>()
        {
            { "roomId", roomId }
        };

        ExecuteCloudScript(
            "GetRoomById",
            args,
            callback
        );
    }

    public static void RemoveRoom(string roomId, Action<object> callback)
    {
        var args = new Dictionary<string, object>()
        {
            { "roomId", roomId }
        };

        ExecuteCloudScript(
            "RemoveRoom",
            args,
            callback
        );
    }

}
