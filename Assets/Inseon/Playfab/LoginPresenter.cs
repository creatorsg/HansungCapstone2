using inseon.Server.Playfab.Login.Scenes;
using insoen.Server.Playfab.Login;
using UnityEngine;

public class LoginPresenter
{
    private readonly IAuthService _authService;
    private readonly LoginFlowController _flowController;
    private readonly ILoginView _view;
    private LoginView loginView;
    private LoginFlowController flowController;

    public LoginPresenter(
        ILoginView view,
        IAuthService authService,
        LoginFlowController flowController)
    {
        _view = view;
        _authService = authService;
        _flowController = flowController;
    }

    public LoginPresenter(LoginView loginView, IAuthService authService, LoginFlowController flowController)
    {
        this.loginView = loginView;
        _authService = authService;
        this.flowController = flowController;
    }

    public void TryLogin(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            _view.SetStatus("입력값이 부족합니다.");
            return;
        }

        _view.SetStatus("로그인 중...");

        _authService.Login(
            email,
            password,
            OnLoginSuccess,
            OnLoginFail
        );
    }

    private void OnLoginSuccess(AuthResult result)
    {
        _view.SetStatus("로그인 성공");
        _flowController.OnLoginSuccess(result);
    }

    private void OnLoginFail(string reason)
    {
        _view.SetStatus($"로그인 실패: {reason}");
    }
}


