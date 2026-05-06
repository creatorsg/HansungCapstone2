using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    private string _playfabId;
    private string _nickname;
    private string _currentRoom;
    private Character _character;
    private CharacterEquipment _equipment;

    private Dictionary<string, bool> _ownedCharacters = new Dictionary<string, bool>();

    public string PlayfabId  => _playfabId;
    public string Nickname   => _nickname;
    public string CurrentRoom => _currentRoom;

    public void SetOwnedCharacters(Dictionary<string, bool> data)
    {
        _ownedCharacters = new Dictionary<string, bool>();
        if (data != null)
            foreach (var pair in data)
                _ownedCharacters[pair.Key.ToLower()] = pair.Value;

        Debug.Log($"[Player] 보유 캐릭터 {_ownedCharacters.Count}개 저장 완료");
    }

    public bool OwnsCharacter(string characterCode)
    {
        return _ownedCharacters.TryGetValue(characterCode.ToLower(), out bool v) && v;
    }

    public IEnumerable<string> GetOwnedCodes()
    {
        foreach (var pair in _ownedCharacters)
            if (pair.Value) yield return pair.Key;
    }

    public void initPlayerData(string nickname, string playfabId)
    {
        _nickname  = nickname;
        _playfabId = playfabId;
        _currentRoom = null;
    }

    public void JoinRoom(string roomId)  { _currentRoom = roomId; }
    public void LeaveRoom()              { _currentRoom = null; }
}
