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
        var rm = NetworkManager.singleton as Jun.GameRoomManager;
        string targetScene = rm?.GameplayScene;

        // 클라이언트: 준비 토글
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

        // 호스트: 씬 전환
        if (NetworkServer.active && NetworkManager.singleton != null)
        {
            if (string.IsNullOrEmpty(targetScene))
            {
                Debug.LogWarning("[BattleStartBtn] GameplayScene이 비어있습니다. 지도에서 영지를 먼저 클릭하세요.");
                return;
            }

            NetworkManager.singleton.ServerChangeScene(targetScene);
            return;
        }

        // 오프라인 테스트 fallback
        if (!string.IsNullOrEmpty(targetScene))
            SceneManager.LoadScene(targetScene);
        else
            Debug.LogWarning("[BattleStartBtn] GameplayScene이 비어있습니다.");
    }
}
