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
                SetState("ID�� �Է����ּ���.");
                return;
            }

            if (string.IsNullOrEmpty(pw))
            {
                SetState("PW�� �Է����ּ���.");
                return;
            }

            if (string.IsNullOrEmpty(nickname))
            {
                SetState("nickName�� �Է����ּ���.");
                return;
            }

            if (pw != pw2)
            {
                SetState("��й�ȣ�� ��ġ���� �ʽ��ϴ�.");
                return;
            }

            OnClickRegister(id, pw, nickname);
        }

        private void OnClickRegister(string id, string pw, string nickname)
        {
            _isProcessing = true;
            _registerButton.interactable = false;
            SetState("���� ��...");

            inseon.Playfab.Register.Authentication.PlayfabRegister.RegisterPlayFabUser(
                id,
                pw,
                nickname,
                onOk: _ =>
                {
                    SetState("������ �ʱ�ȭ ��...");

                    inseon.Playfab.Register.Authentication.PlayfabRegister.InitializePlayerData(
                        onOkJson: json =>
                        {
                            Debug.Log("Init OK: " + json);
                            SetState("���� �Ϸ�!");
                            PlayfabUserManage.Login(id, pw,
                                PlayfabUserManage.SuccessLogin,
                                err =>
                                {
                                    SetState("�ڵ� �α��� ����. �������� �α������ּ���.");
                                    EndProgress();
                                });
                            EndProgress();
                        },
                        onCloudScriptError: csErr =>
                        {
                            SetState($"���� �ʱ�ȭ ����: {csErr.message}");
                            EndProgress();
                        },
                        onTransportError: pfErr =>
                        {
                            Debug.LogError(pfErr.GenerateErrorReport());
                            SetState("��Ʈ��ũ/���� ������ �ʱ�ȭ ����");
                            EndProgress();
                        }
                    );
                },
                onError: e => Fail(e, "���� ����")
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
