using inseon.Playfab.User;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace inseon.LoginWindows.Login.Register
{
    public class RegisterWindow : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _registerState;
        [SerializeField] private TMP_InputField _IDField;
        [SerializeField] private TMP_InputField _PWField;
        [SerializeField] private TMP_InputField _PWCheckField;
        [SerializeField] private TMP_InputField _nicknameField;
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

            PlayfabUserManage.RegisterAndLogin(id, pw, nickname,
                onStateChange: SetState,
                onFail: msg =>
                {
                    SetState(msg);
                    EndProgress();
                });
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
