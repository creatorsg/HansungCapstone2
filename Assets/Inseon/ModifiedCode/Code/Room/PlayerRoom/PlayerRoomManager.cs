using Jun;
using Mirror;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Image = UnityEngine.UI.Image;

public class PlayerRoomManager : MonoBehaviour
{
    private GameRoomManager _gameRoomManager;

    [SerializeField] private Text roomName;
    [SerializeField] private Text roomID;
    [SerializeField] private Image roomType;
    [SerializeField] private List<PlayerSlot> playerSlots;
    [SerializeField] private Sprite privateImage;
    [SerializeField] private Sprite publicSprite;

    private void Awake()
    {
        _gameRoomManager = GameObject.FindAnyObjectByType<GameRoomManager>();
    }

    private void Start()
    {
        roomName.text = _gameRoomManager.RoomName;
        roomID.text = _gameRoomManager.RoomId;
        roomType.sprite = _gameRoomManager.RoomPrivate ? privateImage : publicSprite;
    }

    public void CopyRoomCode()
    {
        GUIUtility.systemCopyBuffer = roomID.text;
        Debug.Log("Room Code Copied: " + roomID.text);
    }

   
}
