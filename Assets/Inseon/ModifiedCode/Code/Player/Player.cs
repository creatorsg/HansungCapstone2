using UnityEngine;

public class Player : MonoBehaviour
{
    private string _playfabId;
    private string _nickname;
    private string _currentRoom;
    private Character _character;
    private CharacterEquipment _equipment;

    public string PlayfabId => _playfabId;
    public string Nickname => _nickname;
    public string CurrentRoom => _currentRoom;

    public void initPlayerData(string nickname, string playfabId)
    {
        _nickname = nickname;
        _playfabId = playfabId;
        _currentRoom = null;
    }

    public void JoinRoom(string roomId)
    {
        _currentRoom = roomId;
    }

    public void LeaveRoom()
    {
        _currentRoom = null;
    }
}
