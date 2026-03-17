using PlayFab;
using PlayFab.ClientModels;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

public static class PlayfabRoomService
{
    public static void CreateRoom(string roomId)
    {
        PlayFabClientAPI.ExecuteCloudScript(
            new ExecuteCloudScriptRequest
            {
                FunctionName = "CreateRoom",
                FunctionParameter = new
                {
                    roomId = roomId
                }
            },
            result => { },
            error => Debug.LogError(error.GenerateErrorReport())
        );
    }

    public static void RemoveRoom(string roomId)
    {
        PlayFabClientAPI.ExecuteCloudScript(
            new ExecuteCloudScriptRequest
            {
                FunctionName = "RemoveRoom",
                FunctionParameter = new
                {
                    roomId = roomId
                }
            },
            null,
            null
        );
    }

    public static void GetRoomList(Action<List<RoomInfo>> callback)
    {
        PlayFabClientAPI.ExecuteCloudScript(
            new ExecuteCloudScriptRequest
            {
                FunctionName = "GetRoomList"
            },
            result =>
            {
                var json = result.FunctionResult.ToString();
                var rooms = JsonConvert.DeserializeObject<List<RoomInfo>>(json);

                callback?.Invoke(rooms);
            },
            error =>
            {
                Debug.LogError(error.GenerateErrorReport());
            }
        );
    }
}