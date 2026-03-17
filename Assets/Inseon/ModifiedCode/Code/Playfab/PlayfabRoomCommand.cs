using kcp2k;
using Mirror;
using System;
using System.IO;
using System.Net;
using UnityEngine;

public static class PlayfabRoomCommand
{
    public static void CreateRoom(string roomId, string roomName, bool isPrivate, string password, int maxPlayers)
    {
        var manager = NetworkManager.singleton as MirrorNetworkManager;

        if (manager == null)
        {
            Debug.LogError("MirrorNetworkManager missing");
            return;
        }

        manager.maxConnections = maxPlayers;

        string ip = GetPublicIP();

        var transport = manager.GetComponent<KcpTransport>();

        int port = transport != null ? transport.Port : 7777;

        PlayfabCommand.CreateRoom(
            roomId,
            roomName,
            ip,
            port,
            maxPlayers,
            isPrivate,
            password
        );
    }

    public static void GetRoomList()
    {

    }


    static string GetPublicIP()
    {
        try
        {
            var request = WebRequest.Create("https://api.ipify.org");

            using (var response = request.GetResponse())
            using (var stream = response.GetResponseStream())
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
        catch
        {
            return "0.0.0.0";
        }
    }

}
