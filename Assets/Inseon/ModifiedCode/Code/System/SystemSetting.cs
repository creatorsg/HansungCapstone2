using inseon.Playfab.User;
using PlayFab;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SystemSetting : MonoBehaviour
{
    public static SystemSetting Instance { get; private set; }

    public bool IsConnected { get; private set; }
    public float MasterVolume { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); 
    }

    public void ChangeScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    public void QuitGame()
    {
        if (PlayFabClientAPI.IsClientLoggedIn())
            PlayfabUserManage.Logout();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
    }
}
