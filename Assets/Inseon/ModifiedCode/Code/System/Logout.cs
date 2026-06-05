using inseon.Playfab.User;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Logout : MonoBehaviour
{
    public void OnLogout()
    {
        PlayfabUserManage.Logout();
        SceneManager.LoadScene("Start"); 
    }
}
