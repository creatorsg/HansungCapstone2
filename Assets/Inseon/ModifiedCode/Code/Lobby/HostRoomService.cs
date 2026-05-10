using Edgegap;
using Mirror;
using System;
using System.Threading.Tasks;
using UnityEngine;

namespace inseon.Lobby
{
    /// <summary>
    /// 방 생성에 필요한 모든 네트워크/백엔드 로직을 담당합니다.
    /// CreateRoomWindow는 UI 입력만 수집하고 이 클래스에 위임합니다.
    ///
    /// 처리 순서:
    ///   1. GameRoomManager / Transport 존재 확인
    ///   2. 공인 IP 조회
    ///   3. Edgegap 릴레이 세션 생성 (userToken 선발급)
    ///   4. Transport 설정
    ///   5. 기존 연결 정리 → StartHost
    ///   6. PlayFab에 방 정보 등록
    /// </summary>
    public static class HostRoomService
    {
        /// <summary>
        /// 방을 생성하고 호스트로 시작합니다.
        /// 실패 시 onFail 콜백을 호출하고 false를 반환합니다.
        /// 성공 시 Mirror가 씬 전환을 처리하므로 후처리가 필요 없습니다.
        /// </summary>
        public static async Task<bool> TryCreateAndHostRoom(
            string roomName,
            string password,
            bool isPrivate,
            int maxPlayers,
            Action<string> onFail = null)
        {
            // ── 1. Manager / Transport 존재 확인 ──────────────────────────
            var manager = NetworkManager.singleton as Jun.GameRoomManager;
            if (manager == null)
            {
                Fail(onFail, "GameRoomManager를 찾을 수 없습니다. Scene에 GameRoomManager가 있는지 확인하세요.");
                return false;
            }

            var transport = manager.GetComponent<EdgegapKcpTransport>();
            if (transport == null)
            {
                Fail(onFail, "EdgegapKcpTransport를 찾을 수 없습니다.");
                return false;
            }

            // ── 2. 공인 IP 조회 ───────────────────────────────────────────
            string hostIp = await PlayfabRoomCommand.GetPublicIPAsync();
            if (string.IsNullOrEmpty(hostIp))
            {
                Fail(onFail, "공인 IP 조회에 실패했습니다.");
                return false;
            }

            // ── 3. Edgegap 릴레이 세션 생성 ──────────────────────────────
            var relay = await EdgegapRelayManager.CreateSession(hostIp, maxPlayers);
            if (relay == null)
            {
                Fail(onFail, "릴레이 세션 생성에 실패했습니다.");
                return false;
            }

            if (relay.userAuthTokens == null || relay.userAuthTokens.Length == 0)
            {
                Fail(onFail, "릴레이 userToken 발급에 실패했습니다.");
                return false;
            }

            // ── 4. Transport 설정 ─────────────────────────────────────────
            transport.relayAddress        = relay.relayAddress;
            transport.relayGameServerPort = relay.serverPort;
            transport.relayGameClientPort = relay.clientPort;
            transport.sessionId           = relay.sessionAuthToken;
            transport.userId              = relay.userAuthTokens[0]; // 호스트는 슬롯 0번

            // ── 5. 기존 연결 정리 → StartHost ─────────────────────────────
            string roomId = GenerateRoomId();
            manager.RoomId   = roomId;
            manager.RoomName = roomName;

            if (NetworkServer.active || NetworkClient.active)
            {
                Debug.LogWarning("[HostRoomService] 기존 연결 감지 → 정리 후 재시작");
                manager.StopHost();
            }

            PlayfabCommand.ResetSaveData();
            manager.StartHost();

            // ── 6. PlayFab에 방 등록 ──────────────────────────────────────
            PlayfabRoomCommand.CreateRoom(
                roomId,
                roomName,
                isPrivate,
                password,
                maxPlayers,
                hostIp,
                relay.relayAddress,
                relay.clientPort,
                relay.sessionId,
                relay.sessionAuthToken,
                relay.userAuthTokens);

            return true;
        }

        // ─────────────────────────────────────────────────────────────────

        private static string GenerateRoomId()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var sb = new System.Text.StringBuilder(6);
            for (int i = 0; i < 6; i++)
                sb.Append(chars[UnityEngine.Random.Range(0, chars.Length)]);
            return sb.ToString();
        }

        private static void Fail(Action<string> onFail, string message)
        {
            Debug.LogError($"[HostRoomService] {message}");
            onFail?.Invoke(message);
        }
    }
}
