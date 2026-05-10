using inseon.Playfab.User;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace inseon.Server.Playfab.Register
{
    public class UserRegister : MonoBehaviour
    {
        [field: SerializeField] private TMP_InputField ID;
        [field: SerializeField] private TMP_InputField PW;
        [field: SerializeField] private TMP_InputField PW_Check;
        [field: SerializeField] private TMP_InputField Nickname;
        [field: SerializeField] private TextMeshProUGUI RegisterState;
        [field: SerializeField] private Button RegisterButton;

        private bool _inProgress;

        private void Awake()
        {
            SetState("정보를 입력해주세요.");
        }

        public void CheckRegister()
        {
            if (_inProgress) return;

            var id = ID.text?.Trim();
            var pw = PW.text;
            var pw2 = PW_Check.text;
            var nickname = Nickname.text?.Trim();

            if (string.IsNullOrEmpty(id))
            {
                SetState("ID를 입력해주세요.");
                return;
            }

            if (string.IsNullOrEmpty(pw))
            {
                SetState("PW를 입력해주세요.");
                return;
            }

            if (string.IsNullOrEmpty(nickname))
            {
                SetState("nickName을 입력해주세요.");
                return;
            }

            if (pw != pw2)
            {
                SetState("비밀번호가 일치하지 않습니다.");
                return;
            }

            OnClickRegister(id, pw, nickname);
        }

        private void OnClickRegister(string id, string pw, string nickname)
        {
            _inProgress = true;
            RegisterButton.interactable = false;
            SetState("가입 중...");

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
            _inProgress = false;
            RegisterButton.interactable = true;
        }

        private void SetState(string msg)
        {
            if (RegisterState != null) RegisterState.text = msg;
            Debug.Log(msg);
        }
    }
}
