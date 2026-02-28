using PlayFab;
using UnityEngine;
using UnityEngine.UI;

namespace inseon.Server.Playfab.Register
{
    public class UserRegister : MonoBehaviour
    {
        [field: SerializeField] private InputField ID;
        [field: SerializeField] private InputField PW;
        [field: SerializeField] private InputField PW_Check;
        [field: SerializeField] private InputField Nickname;
        [field: SerializeField] private Text RegisterState;
        [field: SerializeField] private Button RegisterButton;

        private bool _inProgress;

        public void CheckRegister()
        {
            if (_inProgress) return;

            var id = ID.text?.Trim();
            var pw = PW.text;
            var pw2 = PW_Check.text;
            var nickname = Nickname.text?.Trim();

            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(pw) || string.IsNullOrEmpty(nickname))
            {
                SetState("ID/PW/닉네임을 입력하세요.");
                return;
            }

            if (pw != pw2)
            {
                SetState("비밀번호가 일치하지 않습니다.");
                return;
            }

            OnClickRegister(ID.text, PW.text, Nickname.text);
        }


        private void OnClickRegister(string id, string pw, string nickname)
        {
            _inProgress = true;
            RegisterButton.interactable = false;
            SetState("가입 중...");

            inseon.Playfab.Register.Authentication.PlayfabRegister.RegisterPlayFabUser(
                id, pw,
                onOk: _ =>
                {
                    SetState("로그인 중...");

                    inseon.Playfab.Login.PlayfabAuth.LoginWithPlayFab(
                        id, pw,
                        onOk: __ =>
                        {
                            SetState("닉네임 설정 중...");

                            inseon.Playfab.Register.Authentication.PlayfabRegister.RegisterPlayerNickname(
                                nickname,
                                onOk: ___ =>
                                {
                                    SetState("데이터 초기화 중...");

                                    inseon.Playfab.Register.Authentication.PlayfabRegister.InitializePlayerData(
                                        onOkJson: json =>
                                        {
                                            Debug.Log("Init OK: " + json);
                                            SetState("가입 완료!");
                                            EndProgress();
                                        },
                                        onCloudScriptError: csErr =>
                                        {
                                            SetState($"서버 초기화 실패: {csErr.message}");
                                            EndProgress();
                                        },
                                        onTransportError: pfErr =>
                                        {
                                            Debug.LogError(pfErr.GenerateErrorReport());
                                            SetState("네트워크/인증 오류로 초기화 실패");
                                            EndProgress();
                                        }
                                    );
                                },
                                onError: e => Fail(e, "닉네임 설정 실패 (계정은 생성됨)")
                            );
                        },
                        onError: e => Fail(e, "로그인 실패")
                    );
                },
                onError: e => Fail(e, "가입 실패")
            );
        }

        private void Fail(PlayFabError e, string msg)
        {
            Debug.LogError(e.GenerateErrorReport());
            SetState(msg);
            EndProgress();
        }


        private void EndProgress()
        {
            _inProgress = false;
            RegisterButton.interactable = true;
        }

        private void SetState(string msg)
        {
            if (RegisterState != null) RegisterState.text = msg;
            Debug.Log(msg);
        }

        private void OnPlayFabError(PlayFabError e)
        {
            Debug.LogError(e.GenerateErrorReport());
        }
    }
}
