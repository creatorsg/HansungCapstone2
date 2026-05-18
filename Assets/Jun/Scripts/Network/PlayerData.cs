using Jun;
using Mirror;
using UnityEngine;

public class PlayerData : NetworkBehaviour
{
    /// <summary>CharacterCard.CharacterCode - 프리팹 룩업의 기준 식별자</summary>
    [SyncVar] public string FinalHeroCode  = "";

    /// <summary>CharacterDatabase.index - 레거시 UI 호환용</summary>
    [SyncVar] public int    FinalHeroIndex = -1;


    [SyncVar] public int    FinalHeroPos   = -1;

    [SyncVar] public PlayerInfo Info;
    [SyncVar] public int PingIndex;


    public readonly SyncList<string> unlockedNodeIds = new SyncList<string>();


    [SyncVar] public string SelectedWeaponId = "";


    [SyncVar] public int PurchasedWeaponNodeCount = 0;

    // ????????????????????????????????????????????????????????????????????????

    // Start()에서 호출하면 ServerChangeScene() 이후 씬 전환이 시작될 때


    // ????????????????????????????????????????????????????????????????????????
    private void Awake()
    {
        transform.SetParent(null);
        DontDestroyOnLoad(this.gameObject);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        // Hideout 씬이 활성화되어 있을 때만 UI 업데이트
        if (HideoutManager.Instance != null && FinalHeroPos != -1 && FinalHeroIndex >= 0)
        {
            HideoutManager.Instance.UpdateHideoutUILocal(FinalHeroPos, FinalHeroIndex);
        }
    }

    public override void OnStartServer()
    {
        base.OnStartServer();


        HideoutManager.Instance?.RegisterPlayer(this);
    }
}
