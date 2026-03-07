using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviour
{
    [SerializeField] private Text _playerNickname;

    private Player _player;

    private void Awake()
    {
        _player = inseon.Playfab.User.PlayfabUserManage.Player;
    }

    private void Start()
    {
        if (_player == null)
        {
            Debug.LogError("Player component not found on LobbyManager.");
            return;
        }

        _playerNickname.text = _player.Nickname;
    }
}
