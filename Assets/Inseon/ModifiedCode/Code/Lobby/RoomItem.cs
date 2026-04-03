using UnityEngine;
using UnityEngine.UI;

public class RoomItemUI : MonoBehaviour
{
    [SerializeField] private Text _roomName;
    [SerializeField] private Text _hostName;
    [SerializeField] private Text _playerCount;
    [SerializeField] private Text _status;
    [SerializeField] private Button _joinButton;

    private RoomInfo _roomInfo;

    public void Setup(RoomInfo info)
    {
        _roomInfo = info;

        _roomName.text = info.roomName;
        _hostName.text = info.hostName;
        _playerCount.text = $"{info.playerCount}/{info.maxPlayers}";
        _status.text = info.playerCount >= info.maxPlayers ? "가득참" : "대기중";

        _joinButton.interactable = info.playerCount < info.maxPlayers;
        _joinButton.onClick.RemoveAllListeners();
        _joinButton.onClick.AddListener(OnClickJoin);
    }

    private void OnClickJoin()
    {
        if (_roomInfo.isPrivate)
        {
            LobbyManager.Instance.JoinRoom(_roomInfo);
        }
        else
        {
            LobbyManager.Instance.JoinRoom(_roomInfo);
        }
    }
}