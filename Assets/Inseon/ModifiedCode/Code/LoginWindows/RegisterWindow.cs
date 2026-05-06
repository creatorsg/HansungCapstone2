using inseon.Playfab.User;
using PlayFab;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace inseon.LoginWindows.Login.Register
{
    public class RegisterWindow : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _registerState;
        [SerializeField] private InputField _IDField;
        [SerializeField] private InputField _PWField;
        [SerializeField] private InputField _PWCheckField;
        [SerializeField] private InputField _nicknameField;
        [SerializeField] private Button _registerButton;
        [SerializeField] private Button _cancelButton;
        private bool _isProcessing = false;

        public void CheckRegister()
        {
            if (_isProcessing) return;

            var id = _IDField.text?.Trim();
            var pw = _PWField.text;
            var pw2 = _PWCheckField.text;
            var nickname = _nicknameField.text?.Trim();

            if (string.IsNullOrEmpty(id))
            {
                SetState("ID칸을 입력해주세요.");
                return;
            }

            if (string.IsNullOrEmpty(pw))
            {
                SetState("PW칸을 입력해주세요.");
                return;
            }

            if (string.IsNullOrEmpty(nickname))
            {
                SetState("nickName칸을 입력해주세요.");
                return;
            }

            if (pw != pw2)
            {
                SetState("비밀번호 확인 칸이 다릅니다.");
                return;
            }

            OnClickRegister(id, pw, nickname);
        }

        private void OnClickRegister(string id, string pw, string nickname)
        {
            _isProcessing = true;
            _registerButton.interactable = false;
            SetState("회원가입 시도중...");

            inseon.Playfab.Register.Authentication.PlayfabRegister.RegisterPlayFabUser(
                id,
                pw,
                nickname,
                onOk: _ =>
                {
                    SetState("정보 처리 중...");

                    inseon.Playfab.Register.Authentication.PlayfabRegister.InitializePlayerData(
                        onOkJson: json =>
                        {
                            Debug.Log("Init OK: " + json);
                            SetState("닉네임 설정 중...");
                            PlayfabUserManage.Login(id, pw,
                                PlayfabUserManage.SuccessLogin,
                                err =>
                                {
                                    SetState("가입이 완료되었습니다. 게임에 로그인 합니다.");
                                    EndProgress();
                                });
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
            _isProcessing = false;
            _registerButton.interactable = true;
        }

        private void SetState(string msg)
        {
            if (_registerState != null) _registerState.text = msg;
            Debug.Log(msg);
        }

    }
}
