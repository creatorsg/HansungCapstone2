using Jun;
using Mirror;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;



public class HideoutManager : NetworkBehaviour
{
    public static HideoutManager Instance;

    [Header("유닛 리스트")]
    public readonly SyncList<PlayerData> _players = new SyncList<PlayerData>();

    [Header("유닛 생성 위치")]
    [SerializeField] private List<Image> _spawnPoints;

    [Header("유닛들의 이미지")]
    [SerializeField] private List<Sprite> _playerImages; public List<Sprite> PlayerImages => _playerImages;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }
    public override void OnStartClient()
    {
        base.OnStartClient();
        PlayerData[] players;
        players = FindObjectsByType<PlayerData>(FindObjectsSortMode.None);

        foreach(var player in players)
        {
            UpdateHideoutUILocal(player.FinalHeroPos, player.FinalHeroIndex);
        }
    }

    //접속 후 플레이어 추가(관리하기 위해서
    public void RegisterPlayer(PlayerData pl)
    {

        var manager = NetworkManager.singleton as GameRoomManager;
        _players.Add(pl);

        if (isServer && _players.Count == manager.HeroNum)
        {
            RpcAllPlayersReady();
        }
    }
    public void UpdateHideoutUILocal(int pos, int heroIndex)
    {
        _spawnPoints[pos].sprite = _playerImages[heroIndex];
    }

    [ClientRpc]
    private void RpcAllPlayersReady()
    {
        Debug.Log("집합 완료");
    }

    [Server]
    public void CmdGoToBattleScene()
    {
        NetworkManager.singleton.ServerChangeScene("GamePlay");
    }
}