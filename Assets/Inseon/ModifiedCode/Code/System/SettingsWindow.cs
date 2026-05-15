using inseon.Playfab.User;
using PlayFab;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace inseon.Core
{
    public class SettingsWindow : MonoBehaviour
    {
        [SerializeField] private TMP_Dropdown resolutionDropdown;
        [SerializeField] private TMP_Dropdown screenModeDropdown;

        private Resolution[] _resolutions;

        void OnEnable()
        {
            InitResolutionDropdown();
            InitScreenModeDropdown();
        }

        // ── 초기화 ────────────────────────────────────────────

        void InitResolutionDropdown()
        {
            _resolutions = ResolutionManager.Instance.GetAvailableResolutions();

            resolutionDropdown.ClearOptions();
            resolutionDropdown.AddOptions(
                _resolutions.Select(r => $"{r.width} x {r.height}").ToList()
            );

            int current = global::System.Array.FindIndex(
                _resolutions,
                r => r.width == Screen.width && r.height == Screen.height
            );
            resolutionDropdown.value = current >= 0 ? current : _resolutions.Length - 1;
            resolutionDropdown.RefreshShownValue();
        }

        void InitScreenModeDropdown()
        {
            screenModeDropdown.ClearOptions();
            // ExclusiveFullScreen은 D3D12 충돌/크래시를 유발하므로 선택지에서 제외
            screenModeDropdown.AddOptions(new List<string>
            {
                "전체화면 (창 모드)",    // FullScreenWindow
                "창 모드",              // Windowed
            });

            screenModeDropdown.value = Screen.fullScreenMode switch
            {
                FullScreenMode.FullScreenWindow    => 0,
                FullScreenMode.Windowed            => 1,
                _                                  => 0
            };
            screenModeDropdown.RefreshShownValue();
        }

        // ── 버튼 이벤트 ───────────────────────────────────────
        public void OnApply()
        {
            var res = _resolutions[resolutionDropdown.value];

            FullScreenMode mode = screenModeDropdown.value switch
            {
                0 => FullScreenMode.FullScreenWindow,
                1 => FullScreenMode.Windowed,
                _ => FullScreenMode.FullScreenWindow
            };

            ResolutionManager.Instance.SetResolution(res.width, res.height, mode);
        }

        public void OnCancel()
        {
            gameObject.SetActive(false);
        }

        public void OnQuitGame()
        {
            // 로그인 상태라면 로그아웃 처리 후 종료
            if (PlayFabClientAPI.IsClientLoggedIn())
                PlayfabUserManage.Logout();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
        }

        public void OnLogout()
        {
            PlayfabUserManage.Logout();
            SceneManager.LoadScene("Login"); 
        }
    }
}
