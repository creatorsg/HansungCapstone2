handlers.InitUserData = function (args, context) {

    var playFabId = currentPlayerId;

    // 1. 이미 데이터가 있는지 확인 (중복 초기화 방지)
    var existingData = server.GetUserReadOnlyData({
        PlayFabId: playFabId,
        Keys: ["Level"]
    });

    if (existingData.Data && existingData.Data.Level) {
        return { Result: "AlreadyInitialized" };
    }

    // 2. ReadOnly 데이터 (클라이언트 수정 불가)
    server.UpdateUserReadOnlyData({
        PlayFabId: playFabId,
        Data: {
            Level: "1",
            Rank: "Bronze"
        }
    });

    // 3. Server/Internal 데이터
    server.UpdateUserInternalData({
        PlayFabId: playFabId,
        Data: {
            PlayGame: "0"
        }
    });

    return { Result: "Initialized" };
};
