using Newtonsoft.Json;
using PlayFab;
using PlayFab.ClientModels;
using System;
using System.Collections.Generic;
using UnityEngine;

public static class MirrorRoomManager
{
    public static void CreateRoom(string roomId)
    {
        PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest
        {
            FunctionName = "CreateRoom",
            FunctionParameter = new
            {
                roomId = roomId,
                port = 7777,
                maxPlayers = 4
            }
        }, null, null);
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
