using Edgegap;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public static class EdgegapRelayManager
{
    private const string RELAY_API_TOKEN = "f38418d1-cfa0-4aec-8d9a-4befc373d666";
    private const string API_URL = "https://api.edgegap.com/v1/relays/sessions";

    public class RelayInfo
    {
        public string relayAddress;
        public ushort serverPort;
        public ushort clientPort;
        public string sessionId;
        public uint sessionAuthToken;   
        public uint userAuthToken;    
    }

    public static async Task<RelayInfo> CreateSession(string hostIp)
    {
        try
        {
            var body = JsonConvert.SerializeObject(new
            {
                users = new[] { new { ip = hostIp } }
            });

            var request = new UnityWebRequest(API_URL, "POST");
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"token {RELAY_API_TOKEN}");

            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"릴레이 세션 생성 오류: {request.error}\n{request.downloadHandler.text}");
                return null;
            }

            var response = JsonConvert.DeserializeObject<RelaySessionResponse>(
                request.downloadHandler.text
            );

            string sessionId = response.session_id;

            return await WaitForRelay(sessionId, response);
        }
        catch (Exception e)
        {
            Debug.LogError($"릴레이 세션 생성 실패: {e.Message}");
            return null;
        }
    }

    static async Task<RelayInfo> WaitForRelay(string sessionId, RelaySessionResponse initial)
    {
        int maxAttempts = 30;

        for (int i = 0; i < maxAttempts; i++)
        {
            if (initial.relay != null && initial.relay.ip != null)
            {
                uint sessionToken = initial.authorization_token ?? 0;
                uint userToken = initial.session_users?[0].authorization_token ?? 0;

                return new RelayInfo
                {
                    relayAddress = initial.relay.ip,
                    serverPort = (ushort)initial.relay.ports.server.port,
                    clientPort = (ushort)initial.relay.ports.client.port,
                    sessionId = initial.session_id,
                    sessionAuthToken = sessionToken,
                    userAuthToken = userToken
                };
            }

            // 1초 대기 후 재조회
            await Task.Delay(1000);
            initial = await GetSession(sessionId);

            if (initial == null) return null;
        }

        Debug.LogError("릴레이 준비 타임아웃");
        return null;
    }

    static async Task<RelaySessionResponse> GetSession(string sessionId)
    {
        var request = UnityWebRequest.Get($"{API_URL}/{sessionId}");
        request.SetRequestHeader("Authorization", $"token {RELAY_API_TOKEN}");

        await request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"세션 조회 실패: {request.error}");
            return null;
        }

        return JsonConvert.DeserializeObject<RelaySessionResponse>(
            request.downloadHandler.text
        );
    }

    public static async Task DeleteSession(string sessionId)
    {
        var request = new UnityWebRequest($"{API_URL}/{sessionId}", "DELETE");
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Authorization", $"token {RELAY_API_TOKEN}");

        await request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
            Debug.Log("릴레이 세션 종료 완료");
        else
            Debug.LogError($"세션 종료 실패: {request.error}");
    }

    public static async Task<uint> GetUserToken(string sessionId, string clientIp)
    {
        try
        {
            // Edgegap Relay API: POST /v1/relays/sessions/{session_id}:authorize-user
            // session_id를 URL 경로에 포함시켜야 합니다.
            var body = JsonConvert.SerializeObject(new
            {
                ip = clientIp
            });

            var request = new UnityWebRequest(
                $"{API_URL}/{sessionId}:authorize-user", "POST");
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"token {RELAY_API_TOKEN}");

            await request.SendWebRequest();

            var response = JsonConvert.DeserializeObject<AuthorizeUserResponse>(
                request.downloadHandler.text
            );

            return response.authorization_token ?? 0;
        }
        catch (Exception e)
        {
            Debug.LogError($"유저 토큰 가져오기 실패: {e.Message}");
            return 0;
        }
    }

    #region 응답 구조체

    [Serializable]
    class RelaySessionResponse
    {
        public string session_id;
        public uint? authorization_token;
        public List<SessionUser> session_users;
        public RelayData relay;
    }

    [Serializable]
    class SessionUser
    {
        public string ip;
        public uint? authorization_token;
    }

    [Serializable]
    class RelayData
    {
        public string ip;
        public RelayPorts ports;
    }

    [Serializable]
    class RelayPorts
    {
        public PortInfo server;
        public PortInfo client;
    }

    [Serializable]
    class PortInfo
    {
        public ushort port;
    }

    [Serializable]
    class AuthorizeUserResponse
    {
        public uint? authorization_token;
    }

    #endregion
}