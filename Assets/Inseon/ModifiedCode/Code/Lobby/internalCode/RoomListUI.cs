using UnityEngine;
using System.Collections.Generic;

public class RoomListUI : MonoBehaviour
{
    public static RoomListUI Instance;

    void Awake()
    {
        Instance = this;
    }

    public void UpdateList(List<RoomInfo> rooms)
    {
        foreach (var room in rooms)
        {
            Debug.Log(room.roomId);
        }
    }
}