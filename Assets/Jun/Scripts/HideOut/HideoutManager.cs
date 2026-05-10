using Jun;
using Mirror;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;



public class HideoutManager : NetworkBehaviour
{
    public static HideoutManager Instance;

    [Header("���� ����Ʈ")]
    public readonly SyncList<PlayerData> _players = new SyncList<PlayerData>();

    [Header("���� ���� ��ġ")]
    [SerializeField] private List<Image> _spawnPoints;

    [Header("���ֵ��� �̹���")]
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

    //���� �� �÷��̾� �߰�(�����ϱ� ���ؼ�
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
        Debug.Log("���� �Ϸ�");
    }

    [Server]
    public void CmdGoToBattleScene()
    {
        NetworkManager.singleton.ServerChangeScene("GamePlay");
    }

    /// <summary>
    /// 던전 진입 버튼 콜백 (호스트 전용).
    /// RegionConfig 를 들고 있는 UI 버튼 OnClick 에 연결합니다.
    /// 클라이언트가 누르면 무시되며, 호스트가 누르면 RoundManager.StartDungeon → 던전 씬 전환.
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
            Debug.LogError("[HideoutManager] RegionConfig 가 비어 있습니다.");
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