using insoen.Server.Playfab.Network;
using UnityEngine.SceneManagement;

namespace inseon.Server.Playfab.Login.Scenes
{ 
    public class LoginFlowController
    {
        public void OnLoginSuccess(AuthResult result)
        {
            SceneManager.LoadScene("Lobby");
        }

        public void OnLogout()
        {
            SceneManager.LoadScene("Login");
        }
    }
}