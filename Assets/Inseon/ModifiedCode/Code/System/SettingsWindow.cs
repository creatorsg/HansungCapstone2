using inseon.Playfab.User;
using PlayFab;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace inseon.Core
{
    public class SettingsWindow : MonoBehaviour
    {
        [SerializeField] private TMP_Dropdown resolutionDropdown;
        [SerializeField] private TMP_Dropdown screenModeDropdown;
        // [수정] 로비 설정창에서 BGM 음량을 조절할 슬라이더
        [SerializeField] private Slider bgmVolumeSlider;

        private Resolution[] _resolutions;
        // [수정] Start 씬에서 생성되어 유지되는 BGMManager의 AudioSource
        private AudioSource _bgmAudioSource;

        void OnEnable()
        {
            // [수정] 다른 설정 초기화에서 오류가 발생해도 BGM 연결은 먼저 완료
            InitBgmVolumeSlider();
            InitResolutionDropdown();
            InitScreenModeDropdown();
        }

        void OnDisable()
        {
            // [수정] 설정창을 다시 열 때 리스너가 중복 등록되지 않도록 해제
            if (bgmVolumeSlider != null)
                bgmVolumeSlider.onValueChanged.RemoveListener(OnBgmVolumeChanged);
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

        void InitBgmVolumeSlider()
        {
            // [수정] 로비 설정창을 열 때 현재 BGM 음량과 슬라이더를 동기화
            if (bgmVolumeSlider == null) return;

            ResolveBgmAudioSource();
            bgmVolumeSlider.onValueChanged.RemoveListener(OnBgmVolumeChanged);
            bgmVolumeSlider.onValueChanged.AddListener(OnBgmVolumeChanged);

            if (_bgmAudioSource != null)
                bgmVolumeSlider.SetValueWithoutNotify(_bgmAudioSource.volume);
        }

        void ResolveBgmAudioSource()
        {
            // [수정] 씬 전환 후에도 유지되는 BGMManager에서 AudioSource를 탐색
            if (_bgmAudioSource != null) return;

            var bgmManager = Object.FindFirstObjectByType<Lsy.BGMManager>();
            if (bgmManager != null)
                _bgmAudioSource = bgmManager.GetComponent<AudioSource>();
        }

        void OnBgmVolumeChanged(float volume)
        {
            // [수정] 로비 SOUND 슬라이더 값을 현재 BGM에 즉시 반영
            ResolveBgmAudioSource();
            if (_bgmAudioSource != null)
                _bgmAudioSource.volume = Mathf.Clamp01(volume);
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
