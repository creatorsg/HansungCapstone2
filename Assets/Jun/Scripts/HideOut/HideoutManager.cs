using Jun;
using Mirror;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HideoutManager : NetworkBehaviour
{
    public static HideoutManager Instance;

    [Header("플레이어 목록")]
    public readonly SyncList<PlayerData> _players = new SyncList<PlayerData>();

    [Header("플레이어 표시 위치")]
    [SerializeField] private List<Image> _spawnPoints;

    [Header("캐릭터 이미지")]
    [SerializeField] private List<Sprite> _playerImages;
    public List<Sprite> PlayerImages => _playerImages;

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
        PlayerData[] players = FindObjectsByType<PlayerData>(FindObjectsSortMode.None);

        foreach (var player in players)
        {
            UpdateHideoutUILocal(player.FinalHeroPos, player.FinalHeroIndex);
        }
    }

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
        Debug.Log("[HideoutManager] 모든 플레이어 준비 완료");
    }

    [Server]
    public void CmdGoToBattleScene(string sceneName)
    {
        var rm = NetworkManager.singleton as Jun.GameRoomManager;
        if (rm != null)
            rm.GameplayScene = sceneName;

        NetworkManager.singleton.ServerChangeScene(sceneName);
    }

    /// <summary>
    /// 던전 진입 버튼 콜백입니다. 호스트만 RoundManager를 통해 던전을 시작할 수 있습니다.
    /// </summary>
    public void OnClickEnterDungeon(Jun.RegionConfig cfg)
    {
        if (!NetworkServer.active)
        {
            Debug.LogWarning("[HideoutManager] 호스트만 던전을 시작할 수 있습니다.");
            return;
        }

        if (cfg == null)
        {
            Debug.LogError("[HideoutManager] RegionConfig가 비어 있습니다.");
            return;
        }

        if (Jun.RoundManager.Instance == null)
        {
            Debug.LogError("[HideoutManager] RoundManager 인스턴스가 없습니다. GameRoomManager.RoundManagerPrefab 할당을 확인하세요.");
            return;
        }

        Jun.RoundManager.Instance.StartDungeon(cfg);
    }
}
