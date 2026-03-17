using UnityEngine;
using UnityEngine.UI;

public class RoomItem : MonoBehaviour
{
    [SerializeField] private Text _roomName;
    [SerializeField] private Text _hostName;
    [SerializeField] private Text _playerNumber;
    [SerializeField] private Button _startButton;

    public void SetUp(RoomInfo room)
    {
        _roomName.text = room.room;
    }
}
