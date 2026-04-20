// ===================== 데이터 구조 ==========================
var TARGET_SCHEMA_REVISION = 1;

var initialPlayerData = {
    PlayCount: "0",
    ClearCount: "0"
};


// ====================== 플레이어 데이터 관련 함수 ============================
function requirePlayerCheck()
{
    if (!currentPlayerId)
        throw "Not authenticated.";

    return currentPlayerId;
}

function getSchemaRevision(playFabId)
{
    var internal = server.GetUserInternalData({
        PlayFabId: playFabId,
        Keys: ["SchemaRevision"]
    });

    if (internal &&
        internal.Data &&
        internal.Data.SchemaRevision &&
        internal.Data.SchemaRevision.Value)
    {
        return parseInt(internal.Data.SchemaRevision.Value, 10) || 0;
    }

    return 0;
}

function getPlayerDisplayName(playFabId)
{
    var profile = server.GetPlayerProfile({
        PlayFabId: playFabId,
        ProfileConstraints: {
            ShowDisplayName: true
        }
    });

    if (profile && profile.PlayerProfile && profile.PlayerProfile.DisplayName)
        return profile.PlayerProfile.DisplayName;

    return null;
}

function updatePlayerData(playFabId, schema)
{
    var keys = Object.keys(schema);

    var ro = server.GetUserReadOnlyData({
        PlayFabId: playFabId,
        Keys: keys
    });

    var result = {};
    var needUpdate = {};

    for (var i = 0; i < keys.length; i++)
    {
        var key = keys[i];

        if (!ro || !ro.Data || !ro.Data[key])
        {
            result[key] = schema[key];
            needUpdate[key] = schema[key];
        }
        else
        {
            result[key] = ro.Data[key].Value;
        }
    }

    if (Object.keys(needUpdate).length > 0)
    {
        server.UpdateUserReadOnlyData({
            PlayFabId: playFabId,
            Data: needUpdate
        });
    }

    return result;
}

function loadCharacterData(playFabId)
{
    var title = server.GetTitleData({
        Keys:["CharacterList"]
    });

    if (!title || !title.Data || !title.Data.CharacterList)
        throw "CharacterList TitleData missing";

    var characters = JSON.parse(title.Data.CharacterList);

    var ro = server.GetUserReadOnlyData({
        PlayFabId: playFabId,
        Keys:["hasCharacter"]
    });

    var oldData = {};

    if (ro && ro.Data && ro.Data.hasCharacter)
    {
        try
        {
            oldData = JSON.parse(ro.Data.hasCharacter.Value);
        }
        catch(e)
        {
            oldData = {};
        }
    }

    var newData = {};
    var changed = false;

    for (var i = 0; i < characters.length; i++)
    {
        var c = characters[i];

        if (oldData[c] !== undefined)
        {
            newData[c] = oldData[c];
        }
        else
        {
            newData[c] = false;
            changed = true;
        }
    }

    if (!changed)
    {
        var oldKeys = Object.keys(oldData);
        if (oldKeys.length !== characters.length)
            changed = true;
    }

    if (changed)
    {
        server.UpdateUserReadOnlyData({
            PlayFabId: playFabId,
            Data:{
                hasCharacter: JSON.stringify(newData)
            }
        });
    }

    return newData;
}

// ============= 방 생성 및 조회 기능 관련 =================
function createRoomInfo(args)
{
    var hostName = getPlayerDisplayName(currentPlayerId);

    return {
        roomId:        args.roomId,
        hostPlayFabId: currentPlayerId,
        hostName:      hostName,
        hostPublicIp:  args.hostPublicIp  || "",    // 호스트 공인 IP (same-IP 판별용)
        ip:            args.ip,                     // Edgegap 릴레이 서버 IP
        port:          args.port,
        roomName:      args.roomName,
        playerCount:   1,
        maxPlayers:    args.maxPlayers,
        isPrivate:     args.isPrivate     || false,
        password:      args.password      || null,
        sessionId:     args.sessionId     || "",    // Edgegap 세션 UUID
        sessionToken:  args.sessionToken  || 0,     // transport.sessionId 용
        userTokens:    args.userTokens    || [],    // 선발급 userToken 배열 ([0]=호스트, [1+]=클라이언트)
        createdAt:     Date.now()
    };
}

// 방이 생성된 지 이 시간(ms)이 지나면 좀비 방으로 간주하고 자동 삭제
var ROOM_TTL_MS = 8 * 60 * 60 * 1000; // 8시간

function GetRoomList()
{
    var index = server.GetTitleInternalData({
        Keys:["room_index"]
    });

    if (!index || !index.Data || !index.Data.room_index)
        return [];

    var keys = [];

    try
    {
        keys = JSON.parse(index.Data.room_index);
    }
    catch(e)
    {
        return [];
    }

    if (keys.length === 0)
        return [];

    var data = server.GetTitleInternalData({
        Keys: keys
    });

    var rooms       = [];
    var expiredKeys = [];      // 만료된 방 키 목록
    var now         = Date.now();

    for (var i = 0; i < keys.length; i++)
    {
        var key = keys[i];

        if (!data.Data || !data.Data[key])
        {
            // 인덱스에는 있지만 실제 데이터가 없는 유령 키 → 제거 대상
            expiredKeys.push(key);
            continue;
        }

        try
        {
            var room = JSON.parse(data.Data[key]);

            // TTL 초과 방 → 좀비 방 (Host 크래시/인터넷 끊김 등)
            if (room.createdAt && (now - room.createdAt) > ROOM_TTL_MS)
            {
                expiredKeys.push(key);
                server.SetTitleInternalData({ Key: key, Value: null });
                continue;
            }

            delete room.password;
            delete room.sessionToken;  // 목록에는 노출 금지 — JoinRoom 시에만 제공
            delete room.userTokens;    // 목록에는 노출 금지 — JoinRoom 시에만 제공
            rooms.push(room);
        }
        catch(e)
        {
            expiredKeys.push(key); // 파싱 실패한 손상된 데이터도 정리
        }
    }

    // 만료된 키들을 인덱스에서 제거
    if (expiredKeys.length > 0)
    {
        var cleanKeys = [];
        for (var j = 0; j < keys.length; j++)
        {
            var found = false;
            for (var k = 0; k < expiredKeys.length; k++)
            {
                if (keys[j] === expiredKeys[k]) { found = true; break; }
            }
            if (!found) cleanKeys.push(keys[j]);
        }

        server.SetTitleInternalData({
            Key: "room_index",
            Value: JSON.stringify(cleanKeys)
        });
    }

    return rooms;
}

function joinRoom(args)
{
    var roomKey = "room_" + args.roomId;

    var data = server.GetTitleInternalData({
        Keys:[roomKey]
    });

    if (!data || !data.Data || !data.Data[roomKey])
        throw "Room not found";

    var room = JSON.parse(data.Data[roomKey]);

    if (room.isPrivate)
    {
        if (!args.password)
            throw "Password required";

        if (room.password !== args.password)
            throw "Wrong password";
    }

    if (room.playerCount >= room.maxPlayers)
        throw "Room is full";

    room.playerCount++;

    server.SetTitleInternalData({
        Key: roomKey,
        Value: JSON.stringify(room)
    });

    // 참가자에게는 sessionToken + userTokens 포함해서 반환
    delete room.password;
    return room;  // sessionToken, userTokens 모두 포함
}

function leaveRoom(args)
{
    var roomKey = "room_" + args.roomId;

    var data = server.GetTitleInternalData({
        Keys:[roomKey]
    });

    if (!data || !data.Data || !data.Data[roomKey])
        return;

    var room = JSON.parse(data.Data[roomKey]);

    // 호스트 여부와 무관하게 항상 playerCount만 감소
    // (호스트가 방을 폭파할 때는 RemoveRoom을 직접 호출 → OnStopHost에서 처리)
    room.playerCount = Math.max(0, room.playerCount - 1);

    server.SetTitleInternalData({
        Key: roomKey,
        Value: JSON.stringify(room)
    });
}

function removeRoom(args)
{
    var roomKey = "room_" + args.roomId;

    var data = server.GetTitleInternalData({
        Keys:[roomKey]
    });

    if (!data || !data.Data || !data.Data[roomKey])
        return;

    var room = JSON.parse(data.Data[roomKey]);

    if (room.hostPlayFabId !== currentPlayerId)
        throw "Only host can remove room";

    var index = server.GetTitleInternalData({
        Keys:["room_index"]
    });

    var list = [];

    if (index && index.Data && index.Data.room_index)
    {
        try
        {
            list = JSON.parse(index.Data.room_index);
        }
        catch(e){}
    }

    var newList = [];

    for (var i = 0; i < list.length; i++)
    {
        if (list[i] !== roomKey)
            newList.push(list[i]);
    }

    server.SetTitleInternalData({
        Key:"room_index",
        Value: JSON.stringify(newList)
    });

    server.SetTitleInternalData({
        Key: roomKey,
        Value: null
    });

    return { removed:true };
}

// ============== 세이브 / 로드 ====================

// 세이브 데이터 저장 (Host만 호출)
// args: { clearedStages, currentFloor, gold, characters[], npcLevels{} }
function CurrentGameSave(args, playFabId)
{
    var saveData = {
        clearedStages: args.clearedStages || 0,
        currentFloor:  args.currentFloor  || 0,
        gold:          args.gold          || 0,
        characters:    args.characters    || [],  // CharacterSaveData[]
        npcLevels:     args.npcLevels     || {}   // { "blacksmith": 2, ... }
    };

    server.UpdateUserInternalData({
        PlayFabId: playFabId,
        Data: {
            GameSave: JSON.stringify(saveData),
            SavedAt:  new Date().toISOString()
        }
    });

    return { ok: true };
}

// 세이브 데이터 불러오기
function LoadSaveData(playFabId)
{
    var data = server.GetUserInternalData({
        PlayFabId: playFabId,
        Keys: ["GameSave"]
    });

    if (!data || !data.Data || !data.Data.GameSave)
        return null;

    try
    {
        return JSON.parse(data.Data.GameSave.Value);
    }
    catch(e)
    {
        return null;
    }
}

// 세이브 데이터 삭제 (새 방 생성 시 호출)
function DeleteSaveData(playFabId)
{
    server.UpdateUserInternalData({
        PlayFabId: playFabId,
        Data: {
            GameSave: null,
            SavedAt:  null
        }
    });

    return { ok: true };
}


// ================= 실제 적용 함수 ==========================

handlers.PlayerProfileLoad = function (args, context)
{
    try
    {
        var playFabId = requirePlayerCheck();
        var rev = getSchemaRevision(playFabId);

        var profile = updatePlayerData(playFabId, initialPlayerData);
        var characters = loadCharacterData(playFabId);

        if (rev < TARGET_SCHEMA_REVISION)
        {
            server.UpdateUserInternalData({
                PlayFabId: playFabId,
                Data: {
                    SchemaRevision: String(TARGET_SCHEMA_REVISION),
                    InitAt: new Date().toISOString()
                }
            });
        }

        return {
            ok: true,
            revision: Math.max(rev, TARGET_SCHEMA_REVISION),
            profile: profile
        };
    }
    catch (e)
    {
        log.error("PlayerDataLoad Failed: " + (e && e.stack ? e.stack : e));
        throw e;
    }
};

handlers.CreateRoom = function(args)
{
    requirePlayerCheck();

    if (!args.roomId)   throw "roomId required";
    if (!args.ip)       throw "ip required";
    if (!args.port)     throw "port required";
    if (!args.maxPlayers || args.maxPlayers <= 0)
        throw "invalid maxPlayers";

    var roomKey = "room_" + args.roomId;

    var exist = server.GetTitleInternalData({
        Keys:[roomKey]
    });

    if (exist && exist.Data && exist.Data[roomKey])
        throw "Room already exists";

    var room = createRoomInfo(args);  // sessionId, sessionToken 포함됨

    server.SetTitleInternalData({
        Key: roomKey,
        Value: JSON.stringify(room)
    });

    var index = server.GetTitleInternalData({
        Keys:["room_index"]
    });

    var list = [];

    if (index && index.Data && index.Data.room_index)
    {
        try { list = JSON.parse(index.Data.room_index); }
        catch(e){}
    }

    if (list.indexOf(roomKey) === -1)
        list.push(roomKey);

    server.SetTitleInternalData({
        Key:"room_index",
        Value: JSON.stringify(list)
    });

    delete room.password;
    return room;
};

handlers.GetRoomList = function(args)
{
    requirePlayerCheck();
    return GetRoomList();
};

handlers.JoinRoom = function(args)
{
    requirePlayerCheck();
    return joinRoom(args);  // sessionToken 포함 반환
};

handlers.LeaveRoom = function(args)
{
    requirePlayerCheck();
    leaveRoom(args);
};

handlers.RemoveRoom = function(args)
{
    requirePlayerCheck();
    return removeRoom(args);
};

// [FIX] 반환값 추가
handlers.LoadCharacterState = function(args)
{
    var playFabId = requirePlayerCheck();
    var charData = loadCharacterData(playFabId);
    return { characterState: charData };  // Unity에서 FunctionResult로 수신
};

// [NEW] 세이브 저장 (Host만 호출해야 함 - Unity 쪽에서 NetworkServer.active 체크)
handlers.SaveGameState = function(args)
{
    var playFabId = requirePlayerCheck();
    return CurrentGameSave(args, playFabId);
};

// [NEW] 세이브 불러오기
handlers.LoadGameState = function(args)
{
    var playFabId = requirePlayerCheck();
    return LoadSaveData(playFabId);
};

// [NEW] 세이브 초기화 (새 방 생성 시 Host가 호출)
handlers.ResetSaveData = function(args)
{
    var playFabId = requirePlayerCheck();
    return DeleteSaveData(playFabId);
};

// [NEW] 방 ID로 방 정보 조회 (입장하지 않음 — 민감 정보 제외)
// args: { roomId }
handlers.GetRoomById = function(args)
{
    requirePlayerCheck();

    if (!args.roomId) throw "roomId required";

    var roomKey = "room_" + args.roomId;

    var data = server.GetTitleInternalData({ Keys: [roomKey] });

    if (!data || !data.Data || !data.Data[roomKey])
        throw "Room not found";

    var room = JSON.parse(data.Data[roomKey]);

    // TTL 초과 방 → 좀비 방으로 간주
    var now = Date.now();
    if (room.createdAt && (now - room.createdAt) > ROOM_TTL_MS)
        throw "Room not found";

    // 민감 정보는 제거 후 반환 (sessionToken, userTokens, password)
    delete room.password;
    delete room.sessionToken;
    delete room.userTokens;

    return room;
};
