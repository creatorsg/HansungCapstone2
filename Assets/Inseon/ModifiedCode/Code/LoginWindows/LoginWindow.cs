using inseon.Playfab.User;
using PlayFab;
using TMPro;
using UnityEngine;

namespace inseon.LoginWindows.Login
{
    public class LoginWindow : MonoBehaviour
    {
        [SerializeField] private TMP_InputField _id;
        [SerializeField] private TMP_InputField _pw;

        public enum GuestLoginMode
        {
            [InspectorName("기기 계정 이어가기 (같은 기기면 기존 계정 유지)")]
            ContinueExisting = 0,

            [InspectorName("새 계정으로 시작 (매번 새 계정 생성)")]
            AlwaysNew = 1
        }

        [Header("게스트 로그인 모드")]
        [SerializeField] private GuestLoginMode _guestLoginMode = GuestLoginMode.ContinueExisting;

        public void LoginWithPlayFab()
        {
            var ID = _id.text?.Trim();
            var PW = _pw.text;

            if (string.IsNullOrEmpty(ID) || string.IsNullOrEmpty(PW))
            {
                Debug.Log("Invalid Login Input");
                return;
            }

            if (!ButtonGuard.TryLock()) return;

            PlayfabUserManage.Login(ID, PW,
                onOk: PlayfabUserManage.SuccessLogin,
                onError: error =>
                {
                    PlayfabUserManage.FailureLogin(error);
                    ButtonGuard.Unlock();
                });
        }

        public void LoginWithGuest()
        {
            if (!ButtonGuard.TryLock()) return;

            bool forceNew = _guestLoginMode == GuestLoginMode.AlwaysNew;

            PlayfabUserManage.LoginAsGuest(
                onOk: PlayfabUserManage.SuccessLogin,
                onError: err =>
                {
                    PlayfabUserManage.FailureLogin(err);
                    ButtonGuard.Unlock();
                },
                forceNew: forceNew);
        }
    }
}