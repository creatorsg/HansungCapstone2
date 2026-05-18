using inseon.Playfab.User;
using PlayFab;
using PlayFab.ClientModels;
using TMPro;
using UnityEngine;

namespace inseon.Start
{
    /// <summary>
    /// Start 씬 로그인 팝업 내부 로직.
    ///
    /// 담당:
    ///   • ID / PW 유효성 검사 (빈 칸 등)
    ///   • 상태 메시지 팝업 ("로그인 중...", 오류 문구 등)
    ///   • PlayFab 일반 로그인 / 게스트 로그인
    ///
    /// ── Inspector 연결 가이드 ──
    /// 1. _idField       : ID  TMP_InputField
    /// 2. _pwField       : PW  TMP_InputField
    /// 3. _statusPopup   : 상태 메시지 창 루트 GameObject (평소 비활성)
    /// 4. _statusText    : 상태 텍스트 TMP
    /// 5. _guestLoginMode: 게스트 계정 모드 선택 (Inspector 드롭다운)
    ///
    /// ── 버튼 OnClick 연결 ──
    /// • 로그인 버튼       → OnClickLogin()
    /// • 게스트 로그인 버튼 → OnClickGuestLogin()
    /// • 상태창 닫기 버튼   → OnClickCloseStatus()
    /// • 회원가입 버튼      → StartSceneUI.OnClickOpenRegister() (이 스크립트와 무관)
    /// </summary>
    public class StartLoginPanel : MonoBehaviour
    {
        // ── 입력 필드 ──
        [Header("입력 필드")]
        [SerializeField] private TMP_InputField _idField;
        [SerializeField] private TMP_InputField _pwField;

        // ── 상태 메시지 창 ──
        [Header("상태 메시지 창")]
        [SerializeField] private GameObject     _statusPopup;
        [SerializeField] private TextMeshProUGUI _statusText;

        // ── 게스트 로그인 ──
        public enum GuestLoginMode
        {
            [InspectorName("기기 계정 이어가기 (같은 기기면 기존 계정 유지)")]
            ContinueExisting = 0,

            [InspectorName("새 계정으로 시작 (매번 새 계정 생성)")]
            AlwaysNew = 1
        }
        [SerializeField] private GuestLoginMode _guestLoginMode = GuestLoginMode.ContinueExisting;

        [Header("상태 메시지 자동 숨김")]
        [Tooltip("오류 메시지를 표시한 뒤 자동으로 숨길 시간(초). 0이면 자동 숨김 없음.")]
        [SerializeField] private float _errorAutoHideDelay = 2.5f;

        private Coroutine _autoHideCoroutine;

        // ────────────────────────────────────────────────────────────────
        // 로그인 버튼
        // ────────────────────────────────────────────────────────────────

        public void OnClickLogin()
        {
            var id = _idField.text?.Trim();
            var pw = _pwField.text;

            // ── 유효성 검사 (UI 피드백 포함) ──
            if (string.IsNullOrEmpty(id))
            {
                ShowStatus("ID칸을 입력해주세요.", autoHide: true);
                return;
            }

            if (string.IsNullOrEmpty(pw))
            {
                ShowStatus("비밀번호 칸을 입력해주세요.", autoHide: true);
                return;
            }

            // 중복 클릭 방지
            if (!ButtonGuard.TryLock()) return;

            // 진행 중 메시지 — 콜백이 올 때까지 유지
            ShowStatus("로그인 중...", autoHide: false);

            PlayfabUserManage.Login(id, pw,
                onOk:    OnLoginSuccess,
                onError: OnLoginFailure);
        }

        // ────────────────────────────────────────────────────────────────
        // 게스트 로그인 버튼
        // ────────────────────────────────────────────────────────────────

        public void OnClickGuestLogin()
        {
            if (!ButtonGuard.TryLock()) return;

            ShowStatus("게스트 로그인 중...", autoHide: false);

            bool forceNew = _guestLoginMode == GuestLoginMode.AlwaysNew;

            PlayfabUserManage.LoginAsGuest(
                onOk:     OnLoginSuccess,
                onError:  OnGuestLoginFailure,
                forceNew: forceNew);
        }

        // ────────────────────────────────────────────────────────────────
        // 콜백
        // ────────────────────────────────────────────────────────────────

        private void OnLoginSuccess(LoginResult result)
        {
            // 상태창은 숨기고 PlayfabUserManage가 Lobby 씬으로 전환합니다.
            HideStatus();
            ButtonGuard.Unlock();
            PlayfabUserManage.SuccessLogin(result);
        }

        private void OnLoginFailure(PlayFabError error)
        {
            ShowStatus("아이디 또는 비밀번호가 틀렸습니다.", autoHide: true);
            PlayfabUserManage.FailureLogin(error);
            ButtonGuard.Unlock();
        }

        private void OnGuestLoginFailure(PlayFabError error)
        {
            ShowStatus("게스트 로그인에 실패했습니다.\n잠시 후 다시 시도해주세요.", autoHide: true);
            PlayfabUserManage.FailureLogin(error);
            ButtonGuard.Unlock();
        }

        // ────────────────────────────────────────────────────────────────
        // 상태 메시지 창
        // ────────────────────────────────────────────────────────────────

        /// <summary>
        /// 상태 메시지를 표시합니다.
        /// autoHide = true 이면 _errorAutoHideDelay 초 뒤 자동으로 숨깁니다.
        /// "로그인 중..." 처럼 결과를 기다려야 하는 경우에는 autoHide = false 로 호출합니다.
        /// </summary>
        private void ShowStatus(string message, bool autoHide = false)
        {
            // 이전 자동 숨김 예약이 있으면 취소
            if (_autoHideCoroutine != null)
            {
                StopCoroutine(_autoHideCoroutine);
                _autoHideCoroutine = null;
            }

            if (_statusPopup != null) _statusPopup.SetActive(true);
            if (_statusText  != null) _statusText.text = message;

            if (autoHide && _errorAutoHideDelay > 0f)
                _autoHideCoroutine = StartCoroutine(AutoHideRoutine());
        }

        private System.Collections.IEnumerator AutoHideRoutine()
        {
            yield return new WaitForSeconds(_errorAutoHideDelay);
            HideStatus();
            _autoHideCoroutine = null;
        }

        private void HideStatus()
        {
            if (_autoHideCoroutine != null)
            {
                StopCoroutine(_autoHideCoroutine);
                _autoHideCoroutine = null;
            }
            if (_statusPopup != null) _statusPopup.SetActive(false);
        }

        /// <summary>상태창 닫기 버튼 OnClick에 연결합니다.</summary>
        public void OnClickCloseStatus()
        {
            HideStatus();
        }
    }
}
