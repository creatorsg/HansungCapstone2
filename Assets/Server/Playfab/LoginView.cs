using insoen.Server.Playfab.Login;
using UnityEngine;
using UnityEngine.UI;

public interface ILoginView
{
    void SetStatus(string message);
}

public class LoginView : MonoBehaviour, ILoginView
{
    [SerializeField] private InputField _idInput;
    [SerializeField] private InputField _pwInput;
    [SerializeField] private Text _statusTxt;

    private LoginPresenter _presenter;

    public void SetPresenter(LoginPresenter presenter)
    {
        _presenter = presenter;
    }

    public void OnLoginButton()
    {
        _presenter.TryLogin(_idInput.text, _pwInput.text);
    }

    public void SetStatus(string message)
    {
        _statusTxt.text = message;
    }
}
