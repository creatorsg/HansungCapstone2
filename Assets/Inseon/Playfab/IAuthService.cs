using System;

public interface IAuthService
{
    // 로그인
    void Login(
        string email,
        string password,
        Action<AuthResult> onSuccess,
        Action<string> onFailure
    );

    // 로그아웃
    void GameLogout();

    // 회원가입
    void Register(
        string email,
        string password,
        Action<AuthResult> onSuccess,
        Action<string> onFailure
    );
}

public readonly struct AuthResult
{
    public readonly string Username;
    public readonly string PlayFabId;

    public AuthResult(string username, string playFabId)
    {
        Username = username;
        PlayFabId = playFabId;
    }
}
