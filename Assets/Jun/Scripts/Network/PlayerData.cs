using Jun;
using Mirror;
using UnityEngine;

public class PlayerData : NetworkBehaviour
{
    /// <summary>CharacterCard.CharacterCode - 프리팹 룩업의 기준 식별자</summary>
    [SyncVar] public string FinalHeroCode  = "";

    /// <summary>CharacterDatabase.index - 레거시 UI 호환용</summary>
    [SyncVar] public int    FinalHeroIndex = -1;

    /// <summary>스폰 위치 인덱스 (BattleManager.SpawnPoints[FinalHeroPos])</summary>
    [SyncVar] public int    FinalHeroPos   = -1;

    [SyncVar] public PlayerInfo Info;
    [SyncVar] public int PingIndex;

    // ────────────────────────────────────────────────────────────────────────
    // Awake에서 DontDestroyOnLoad를 호출합니다.
    // Start()에서 호출하면 ServerChangeScene() 이후 씬 전환이 시작될 때
    // Start()가 아직 실행되지 않은 상태에서 오브젝트가 파괴될 수 있습니다.
    // Awake()는 Instantiate() 직후 동기적으로 호출되므로 안전합니다.
    // ────────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        transform.SetParent(null); // DontDestroyOnLoad는 루트 오브젝트여야 합니다
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

        // Hideout씬이 없는 경우(배틀씬 직행)에도 NPE가 나지 않도록 null-safe 처리
        HideoutManager.Instance?.RegisterPlayer(this);
    }
}
