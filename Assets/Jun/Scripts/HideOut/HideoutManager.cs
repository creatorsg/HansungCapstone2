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
            UpdateHideoutUILocal(player.FinalHeroPos, player.FinalHeroCode);
        }
    }

    //���� �� �÷��̾� �߰�(�����ϱ� ����)

    public void RegisterPlayer(PlayerData pl)
    {

        var manager = NetworkManager.singleton as GameRoomManager;
        _players.Add(pl);

        if (isServer && _players.Count == manager.HeroNum)
        {
            RpcAllPlayersReady();
        }
    }
    public void UpdateHideoutUILocal(int pos, string heroCode)
    {
        if (pos < 0 || pos >= _spawnPoints.Count) return;

        Sprite sprite = null;
        if (CharacterRegistry.TryGet(heroCode, out var entry))
            sprite = entry.CharacterSprite;

        if (sprite == null)
            Debug.LogWarning($"[HideoutManager] '{heroCode}' 스프라이트를 찾지 못했습니다.");

        _spawnPoints[pos].sprite = sprite;
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
}