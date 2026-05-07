using System.Collections.Generic;
using UnityEngine;

public class RoomListUI : MonoBehaviour
{
    [SerializeField] private Transform _content;
    [SerializeField] private GameObject _roomItemPrefab; 

    public static RoomListUI Instance;

    private readonly List<GameObject> _spawnedItems = new();

    private void Awake()
    {
        Instance = this;
    }

    public void UpdateList(List<RoomInfo> rooms)
    {
        foreach (var item in _spawnedItems)
            Destroy(item);
        _spawnedItems.Clear();

        if (rooms == null || rooms.Count == 0)
        {
            Debug.Log("방 목록이 비어있습니다.");
            return;
        }

        foreach (var room in rooms)
        {
            var obj = Instantiate(_roomItemPrefab, _content);
            obj.GetComponent<RoomItemUI>().Setup(room);
            _spawnedItems.Add(obj);
        }
    }
}
