using inseon.Server.Playfab.Login.Scenes;
using insoen.Server.Playfab.Login;
using UnityEngine;


public class LoginInstaller : MonoBehaviour
{
    [SerializeField] private LoginView _loginView;

    private void Awake()
    {
        IAuthService authService = new PlayFabAuthService();
        var flowController = new LoginFlowController();

        var presenter = new LoginPresenter(
            _loginView,
            authService,
            flowController
        );

        _loginView.SetPresenter(presenter);
    }
}
