using UnityEngine;
using UnityEngine.SceneManagement;

public class StartButton : MonoBehaviour
{
    public void onClickStart()
    {
        SceneManager.LoadScene("LoginScene");
    }
}
