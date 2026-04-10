using Edgegap;
using Newtonsoft.Json;
using System;
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

        // 세션 생성 시 maxPlayers 수만큼 미리 발급된 userToken 배열.
        // [0] = 호스트, [1] = 첫 번째 클라이언트, [2] = 두 번째 클라이언트, ...
        // Edgegap Relay는 세션 생성 이후 사용자를 추가할 수 없으므로 여기서 전부 발급함.
        public uint[] userAuthTokens;
    }

    // ─────────────────────────────────────────────────────────────────────
    // 방 생성 시 호출. maxPlayers 수만큼 유저 슬롯을 미리 등록해 토큰을 전부 발급받음.
    // Edgegap Relay는 세션 생성 이후 사용자 추가 API(405/404)를 지원하지 않는다.
    // ─────────────────────────────────────────────────────────────────────
    public static async Task<RelayInfo> CreateSession(string hostIp, int maxPlayers)
    {
        try
        {
            // maxPlayers 개의 슬롯 생성. 실제 클라이언트 IP는 미리 알 수 없으므로
            // 호스트 IP로 플레이스홀더 등록. Edgegap은 토큰을 IP가 아닌 인덱스로 발급한다.
            var users = new object[maxPlayers];
            for (int i = 0; i < maxPlayers; i++)
                users[i] = new { ip = hostIp };

            var body = JsonConvert.SerializeObject(new { users = users });

            var request = new UnityWebRequest(API_URL, "POST");
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"token {RELAY_API_TOKEN}");

            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[CreateSession] 릴레이 세션 생성 오류: {request.error}\n{request.downloadHandler.text}");
                return null;
            }

            Debug.Log($"[CreateSession] 응답: {request.downloadHandler.text}");

            var response = JsonConvert.DeserializeObject<RelaySessionResponse>(request.downloadHandler.text);
            return await WaitForRelay(response.session_id, response);
        }
        catch (Exception e)
        {
            Debug.LogError($"[CreateSession] 릴레이 세션 생성 실패: {e.Message}");
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

                // 전체 user 토큰 배열 수집
                var tokens = new uint[initial.session_users?.Count ?? 0];
                if (initial.session_users != null)
                {
                    for (int j = 0; j < initial.session_users.Count; j++)
                        tokens[j] = initial.session_users[j].authorization_token ?? 0;
                }

                Debug.Log($"[CreateSession] 세션 준비 완료. 슬롯 수={tokens.Length}, 토큰={string.Join(",", tokens)}");

                return new RelayInfo
                {
                    relayAddress     = initial.relay.ip,
                    serverPort       = (ushort)initial.relay.ports.server.port,
                    clientPort       = (ushort)initial.relay.ports.client.port,
                    sessionId        = initial.session_id,
                    sessionAuthToken = sessionToken,
                    userAuthTokens   = tokens
                };
            }

            await Task.Delay(1000);
            initial = await GetSession(sessionId);
            if (initial == null) return null;
        }

        Debug.LogError("[CreateSession] 릴레이 준비 타임아웃");
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

        return JsonConvert.DeserializeObject<RelaySessionResponse>(request.downloadHandler.text);
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

    #endregion
}
