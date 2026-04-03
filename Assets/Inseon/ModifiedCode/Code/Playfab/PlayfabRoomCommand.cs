using kcp2k;
using Mirror;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using UnityEngine;

public static class PlayfabRoomCommand
{
    public static void CreateRoom(
        string roomId,
        string roomName,
        bool isPrivate,
        string password,
        int maxPlayers,
        string ip,
        int port,
        string sessionId)
    {
        var manager = NetworkManager.singleton as MirrorNetworkManager;

        if (manager == null)
        {
            Debug.LogError("MirrorNetworkManager missing");
            return;
        }

        manager.maxConnections = maxPlayers;

        PlayfabCommand.CreateRoom(
            roomId,
            roomName,
            ip,
            port,
            maxPlayers,
            isPrivate,
            password,
            sessionId 
        );
    }

    public static async Task<string> GetPublicIPAsync()
    {
        try
        {
            var request = WebRequest.Create("https://api.ipify.org");
            using var response = await Task.Factory.FromAsync(
                request.BeginGetResponse,
                request.EndGetResponse,
                null
            );
            using var stream = response.GetResponseStream();
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync();
        }
        catch (Exception e)
        {
            Debug.LogError($"공인 IP 가져오기 실패: {e.Message}");
            return "0.0.0.0";
        }
    }
}