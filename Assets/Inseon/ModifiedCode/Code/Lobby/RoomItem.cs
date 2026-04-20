using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoomItemUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _roomName;
    [SerializeField] private TextMeshProUGUI _hostName;
    [SerializeField] private TextMeshProUGUI _playerCount;
    [SerializeField] private TextMeshProUGUI _status;
    /// <summary>
    /// 비공개방 표시용 오브젝트(자물쇠 아이콘 등). 없으면 무시됩니다.
    /// </summary>
    [SerializeField] private GameObject _privateIcon;
    [SerializeField] private Button _joinButton;

    private RoomInfo _roomInfo;

    public void Setup(RoomInfo info)
    {
        _roomInfo = info;

        _roomName.text    = info.roomName;
        _hostName.text    = info.hostName;
        _playerCount.text = $"{info.playerCount}/{info.maxPlayers}";

        bool isFull = info.playerCount >= info.maxPlayers;

        // TMP 기본 폰트(LiberationSans SDF)는 한글 미지원 → ASCII 사용
        // 비공개방이면 [LOCK] 접두어로 구분
        if (isFull)
            _status.text = "FULL";
        else if (info.isPrivate)
            _status.text = "LOCK";
        else
            _status.text = "OPEN";

        // 별도 자물쇠 아이콘 오브젝트가 있을 경우 on/off
        if (_privateIcon != null)
            _privateIcon.SetActive(info.isPrivate);

        _joinButton.interactable = !isFull;
        _joinButton.onClick.RemoveAllListeners();
        _joinButton.onClick.AddListener(OnClickJoin);
    }

    private void OnClickJoin()
    {
        LobbyManager.Instance.JoinRoom(_roomInfo);
    }
}