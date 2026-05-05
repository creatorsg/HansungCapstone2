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

        // 내 화면에 내 캐릭터가 성공적으로 스폰되었을 때, 내 초상화를 그려라!
        if (HideoutManager.Instance != null && FinalHeroPos != -1 && FinalHeroIndex != -1)
        {
            HideoutManager.Instance.UpdateHideoutUILocal(FinalHeroPos, FinalHeroIndex);
        }
    }

    private void Start()
    {
        transform.SetParent(null); // 부모가 없어야 보호막이 작동합니다
        DontDestroyOnLoad(this.gameObject);
    }
    public override void OnStartServer()
    {
        base.OnStartServer();
       
        HideoutManager.Instance.RegisterPlayer(this);
    }
}
