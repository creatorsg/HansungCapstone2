using Newtonsoft.Json.Linq;
using PlayFab;
using PlayFab.ClientModels;
using System;
using System.Collections.Generic;
using UnityEngine;

public static class PlayfabCommand
{
    static void ExecuteCloudScript(string functionName, object args, Action<object> onSuccess)
    {
        ExecuteCloudScript(functionName, args, onSuccess, null);
    }

    // 에러 콜백이 필요한 경우 사용하는 오버로드
    static void ExecuteCloudScript(string functionName, object args, Action<object> onSuccess, Action<string> onError)
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
                    // result.Error.Message = "JavascriptException" (오류 유형)
                    // result.Error.Error   = CloudScript에서 throw한 실제 메시지 (e.g. "Wrong password")
                    var detail = !string.IsNullOrEmpty(result.Error.Error)
                        ? result.Error.Error
                        : result.Error.Message;
                    Debug.LogError($"CloudScript Error ({functionName}): [{result.Error.Message}] {result.Error.Error}");
                    onError?.Invoke(detail);
                    return;
                }

                onSuccess?.Invoke(result.FunctionResult);
            },
            error =>
            {
                var msg = error.GenerateErrorReport();
                Debug.LogError(msg);
                onError?.Invoke(msg);
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
        string hostPublicIp,    // 호스트 공인 IP (same-IP 판별용)
        string relayIp,         // Edgegap 릴레이 서버 IP
        int port,
        int maxPlayers,
        bool isPrivate,
        string password,
        string sessionId,
        uint sessionToken,      // transport.sessionId 용 (세션 인증 토큰)
        uint[] userTokens)      // 선발급 userToken 배열: [0]=호스트, [1+]=클라이언트
    {
        PlayFabClientAPI.ExecuteCloudScript(
            new ExecuteCloudScriptRequest
            {
                FunctionName = "CreateRoom",
                FunctionParameter = new
                {
                    roomId       = roomId,
                    roomName     = roomName,
                    hostPublicIp = hostPublicIp,
                    ip           = relayIp,
                    port         = port,
                    maxPlayers   = maxPlayers,
                    isPrivate    = isPrivate,
                    password     = password,
                    sessionId    = sessionId,
                    sessionToken = sessionToken,
                    userTokens   = userTokens   // uint[] → JSON 배열로 직렬화됨
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

    /// <summary>
    /// 방 ID로 방 정보를 조회합니다. 입장은 하지 않습니다.
    /// 방이 없거나 만료된 경우 onError 콜백이 호출됩니다.
    /// </summary>
    public static void GetRoomById(string roomId, Action<RoomInfo> onSuccess, Action<string> onError = null)
    {
        ExecuteCloudScript(
            "GetRoomById",
            new { roomId = roomId },
            result =>
            {
                var json = result.ToString();
                var room = Newtonsoft.Json.JsonConvert.DeserializeObject<RoomInfo>(json);
                onSuccess?.Invoke(room);
            },
            onError
        );
    }

    public static void JoinRoom(string roomId, string password, Action<RoomInfo> onSuccess, Action<string> onError = null)
    {
        ExecuteCloudScript(
            "JoinRoom",
            new { roomId = roomId, password = password },
            result =>
            {
                var json = result.ToString();

                // CloudScript에서 throw한 오류는 { cloudError: "..." } 형태로 반환됨
                // (PlayFab SDK가 string throw를 ScriptExecutionError에 제대로 담지 않는 문제 우회)
                var jObj = JObject.Parse(json);
                if (jObj["cloudError"] != null)
                {
                    var errMsg = jObj["cloudError"].ToString();
                    Debug.LogWarning($"[JoinRoom] CloudScript 오류: {errMsg}");
                    onError?.Invoke(errMsg);
                    return;
                }

                var room = Newtonsoft.Json.JsonConvert.DeserializeObject<RoomInfo>(json);
                onSuccess?.Invoke(room);
            },
            onError
        );
    }

    public static void RemoveRoom(string roomId, int retryCount = 2)
    {
        if (string.IsNullOrEmpty(roomId))
        {
            Debug.LogWarning("[RemoveRoom] roomId가 비어있어 요청을 건너뜁니다.");
            return;
        }

        var request = new ExecuteCloudScriptRequest
        {
            FunctionName = "RemoveRoom",
            FunctionParameter = new { roomId = roomId }
        };

        PlayFabClientAPI.ExecuteCloudScript(
            request,
            r =>
            {
                if (r.Error != null)
                {
                    Debug.LogError($"[RemoveRoom] CloudScript 오류: [{r.Error.Message}] {r.Error.Error}");
                    if (retryCount > 0)
                    {
                        Debug.Log($"[RemoveRoom] 재시도 ({retryCount}회 남음): {roomId}");
                        RemoveRoom(roomId, retryCount - 1);
                    }
                    else
                    {
                        Debug.LogError($"[RemoveRoom] 최종 실패 - 방이 PlayFab에 남아있을 수 있습니다: {roomId}");
                    }
                    return;
                }
                Debug.Log($"[RemoveRoom] 삭제 완료: {roomId}");
            },
            e =>
            {
                Debug.LogError($"[RemoveRoom] 네트워크 오류: {e.GenerateErrorReport()}");
                if (retryCount > 0)
                {
                    Debug.Log($"[RemoveRoom] 재시도 ({retryCount}회 남음): {roomId}");
                    RemoveRoom(roomId, retryCount - 1);
                }
                else
                {
                    Debug.LogError($"[RemoveRoom] 최종 실패 - 방이 PlayFab에 남아있을 수 있습니다: {roomId}");
                }
            }
        );
    }

    // 연결 실패 등으로 playerCount를 롤백해야 할 때 호출
    public static void LeaveRoom(string roomId)
    {
        var request = new ExecuteCloudScriptRequest
        {
            FunctionName = "LeaveRoom",
            FunctionParameter = new { roomId = roomId }
        };

        PlayFabClientAPI.ExecuteCloudScript(
            request,
            r => Debug.Log($"[LeaveRoom] playerCount 롤백 완료: {roomId}"),
            e => Debug.LogError($"[LeaveRoom] 실패: {e.GenerateErrorReport()}")
        );
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

    // ========================= Save / Load =========================

    /// <summary>
    /// 현재 게임 상태를 PlayFab에 저장합니다. Host만 호출해야 합니다.
    /// </summary>
    public static void SaveGameState(SaveData data, Action onComplete = null)
    {
        var json = Newtonsoft.Json.JsonConvert.SerializeObject(data);
        var args = Newtonsoft.Json.JsonConvert.DeserializeObject<object>(json);

        ExecuteCloudScript(
            "SaveGameState",
            args,
            _ =>
            {
                Debug.Log("[Save] 저장 완료");
                onComplete?.Invoke();
            }
        );
    }

    /// <summary>
    /// PlayFab에서 저장된 게임 상태를 불러옵니다.
    /// </summary>
    public static void LoadGameState(Action<SaveData> onResult)
    {
        ExecuteCloudScript(
            "LoadGameState",
            null,
            result =>
            {
                if (result == null)
                {
                    Debug.Log("[Save] 저장된 데이터 없음 (새 게임)");
                    onResult?.Invoke(null);
                    return;
                }

                var json = result.ToString();
                var data = Newtonsoft.Json.JsonConvert.DeserializeObject<SaveData>(json);
                onResult?.Invoke(data);
            }
        );
    }

    /// <summary>
    /// 세이브 데이터를 초기화합니다. 새 방 생성 시 Host가 호출합니다.
    /// </summary>
    public static void ResetSaveData(Action onComplete = null)
    {
        ExecuteCloudScript(
            "ResetSaveData",
            null,
            _ =>
            {
                Debug.Log("[Save] 세이브 초기화 완료");
                onComplete?.Invoke();
            }
        );
    }

}
