using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    private string _playfabId;
    private string _nickname;
    private string _currentRoom;
    private Character _character;
    private CharacterEquipment _equipment;

    // 보유 캐릭터 목록: key=characterCode("C001" 등), value=보유 여부
    // PlayfabUserManage.SuccessLogin → CheckPlayerCharacterData 콜백에서 채워집니다.
    private Dictionary<string, bool> _ownedCharacters = new Dictionary<string, bool>();

    public string PlayfabId  => _playfabId;
    public string Nickname   => _nickname;
    public string CurrentRoom => _currentRoom;

    /// <summary>
    /// CloudScript LoadCharacterState 결과를 저장합니다.
    /// </summary>
    public void SetOwnedCharacters(Dictionary<string, bool> data)
    {
        _ownedCharacters = data ?? new Dictionary<string, bool>();
        Debug.Log($"[Player] 보유 캐릭터 {_ownedCharacters.Count}개 저장 완료");
    }

    /// <summary>
    /// 해당 캐릭터를 보유하고 있는지 반환합니다.
    /// </summary>
    public bool OwnsCharacter(string characterCode)
    {
        return _ownedCharacters.TryGetValue(characterCode, out bool v) && v;
    }

    /// <summary>
    /// 보유 중인 캐릭터 코드 목록을 반환합니다.
    /// </summary>
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
