using Jun;
using Mirror;
using UnityEngine;

public class PlayerData : NetworkBehaviour
{
    [SyncVar] public int FinalHeroIndex = -1;
    [SyncVar] public int FinalHeroPos = -1;
    [SyncVar] public PlayerInfo Info;
    [SyncVar] public int PingIndex;

    public override void OnStartClient()
    {
        base.OnStartClient();

        // �� ȭ�鿡 �� ĳ���Ͱ� ���������� �����Ǿ��� ��, �� �ʻ�ȭ�� �׷���!
        if (HideoutManager.Instance != null && FinalHeroPos != -1 && FinalHeroIndex != -1)
        {
            HideoutManager.Instance.UpdateHideoutUILocal(FinalHeroPos, FinalHeroIndex);
        }
    }

    private void Start()
    {
        transform.SetParent(null); // �θ� ����� ��ȣ���� �۵��մϴ�
        DontDestroyOnLoad(this.gameObject);
    }
    public override void OnStartServer()
    {
        base.OnStartServer();

        // Hideout씬이 없는 경우(배틀씬 직행)에도 NPE가 나지 않도록 null-safe 처리
        HideoutManager.Instance?.RegisterPlayer(this);
    }
}
