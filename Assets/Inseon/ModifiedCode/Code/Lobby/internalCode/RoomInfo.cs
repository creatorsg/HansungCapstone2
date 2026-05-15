[System.Serializable]
public class RoomInfo
{
    public string roomId;
    public string hostPlayFabId;
    public string hostName;
    public string hostPublicIp;   // 호스트의 공인 IP (same-IP 테스트 판별용)
    public string ip;             // Edgegap 릴레이 서버 IP
    public int port;
    public string roomName;
    public int playerCount;
    public int maxPlayers;
    public bool isPrivate;
    public long createdAt;
    public string sessionId;
    public uint sessionToken;

    // 방 생성 시 maxPlayers 수만큼 선발급된 userToken 배열.
    // [0]=호스트, [1]=첫 번째 클라이언트, [2]=두 번째, ...
    // JoinRoom CloudScript가 이 배열을 포함해서 반환함 (GetRoomList는 제외).
    public uint[] userTokens;
}