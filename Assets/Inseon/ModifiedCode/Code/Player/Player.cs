using NUnit.Framework;
using UnityEngine;

public class Player : MonoBehaviour
{
    private string _nickname;
    private string _currentRoom;

    public void initPlayerData(string nickname)
    {
        _nickname = nickname;
        _currentRoom = null;
    }
}
