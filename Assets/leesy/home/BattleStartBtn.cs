using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;

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

        if (NetworkServer.active && NetworkManager.singleton != null)
        {
            NetworkManager.singleton.ServerChangeScene(battleSceneName);
            return;
        }

        if (NetworkClient.isConnected)
        {
            Debug.LogWarning("[BattleStartBtn] 클라이언트는 직접 씬 전환 불가 - 호스트가 시작해야 합니다.");
            return;
        }

        SceneManager.LoadScene(battleSceneName);
    }
}
