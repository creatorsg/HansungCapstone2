using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;
using Lsy;

/// <summary>
/// 씬 이름은 DistrictHover.OnPointerClick에서 GameRoomManager.GameplayScene에 자동 등록됩니다.
/// 이 버튼은 GameplayScene을 읽어 실행만 합니다.
/// </summary>
public class BattleStartBtn : MonoBehaviour
{
    public void GameStartBtn()
    {
        // ReadyOrStartButton이 같은 오브젝트에서 이미 클릭을 처리하면 중복 실행 금지
        if (GetComponent<ReadyOrStartButton>() != null)
            return;

        var rm = NetworkManager.singleton as Jun.GameRoomManager;
        string targetScene = rm?.GameplayScene;

        if (QuestVoteSystem.Instance != null && !QuestVoteSystem.Instance.CanUseReadyOrStartButton)
            return;

        if (NetworkClient.isConnected && !NetworkServer.active)
        {
            if (ReadySystem.Instance != null && CharacterSlotManager.TryGetLocalNetId(out uint myNetId))
            {
                ReadySystem.Instance.CmdToggleReady(myNetId);
                return;
            }
            return;
        }

        if (NetworkServer.active && NetworkManager.singleton != null)
        {
            if (!IsSoloHost() && (ReadySystem.Instance == null || !ReadySystem.Instance.AllReady))
                return;

            if (string.IsNullOrEmpty(targetScene))
                return;

            PlayerAccount.SyncAllAccountsHideoutDataToBattleData();
            NetworkManager.singleton.ServerChangeScene(targetScene);
            return;
        }

        if (!string.IsNullOrEmpty(targetScene))
            SceneManager.LoadScene(targetScene);
    }

    private bool IsSoloHost()
    {
        if (!NetworkServer.active) return false;

        foreach (var conn in NetworkServer.connections.Values)
        {
            if (conn == null) continue;
            if (conn.connectionId == 0) continue;
            return false;
        }

        return true;
    }
}
