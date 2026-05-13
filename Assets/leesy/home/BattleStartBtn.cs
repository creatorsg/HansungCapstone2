using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;
using Lsy;

public class BattleStartBtn : MonoBehaviour
{
    [SerializeField] private string battleSceneName = "00slum";

    public void GameStartBtn()
    {
        if (string.IsNullOrEmpty(battleSceneName))
        {
            Debug.LogWarning("[BattleStartBtn] battleSceneName이 비어있습니다.");
            return;
        }

        // 클라이언트가 누르면 시작이 아니라 준비 토글로 동작
        if (NetworkClient.isConnected && !NetworkServer.active)
        {
            if (ReadySystem.Instance != null && CharacterSlotManager.TryGetLocalNetId(out uint myNetId))
            {
                ReadySystem.Instance.CmdToggleReady(myNetId);
                return;
            }

            Debug.LogWarning("[BattleStartBtn] 준비 시스템을 찾지 못해 준비 토글에 실패했습니다.");
            return;
        }

        // 호스트는 정상적으로 게임 시작(씬 전환)
        if (NetworkServer.active && NetworkManager.singleton != null)
        {
            NetworkManager.singleton.ServerChangeScene(battleSceneName);
            return;
        }

        // 싱글 플레이 fallback
        SceneManager.LoadScene(battleSceneName);
    }
}
