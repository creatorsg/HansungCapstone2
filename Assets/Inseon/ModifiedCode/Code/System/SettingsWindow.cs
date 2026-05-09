using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace inseon.Core
{
    /// <summary>
    /// 해상도 / 화면 모드 설정 UI.
    /// TMP_Dropdown 두 개(해상도, 화면 모드)와 적용/취소 버튼에 연결.
    /// </summary>
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

            // 현재 해상도 선택 표시
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
            screenModeDropdown.AddOptions(new List<string>
            {
                "전체화면 (창 모드)",    // FullScreenWindow
                "창 모드",              // Windowed
                "전체화면 (독점)"       // ExclusiveFullScreen
            });

            screenModeDropdown.value = Screen.fullScreenMode switch
            {
                FullScreenMode.FullScreenWindow    => 0,
                FullScreenMode.Windowed            => 1,
                FullScreenMode.ExclusiveFullScreen => 2,
                _                                  => 0
            };
            screenModeDropdown.RefreshShownValue();
        }

        // ── 버튼 이벤트 ───────────────────────────────────────

        /// <summary>적용 버튼 OnClick에 연결</summary>
        public void OnApply()
        {
            var res = _resolutions[resolutionDropdown.value];

            FullScreenMode mode = screenModeDropdown.value switch
            {
                0 => FullScreenMode.FullScreenWindow,
                1 => FullScreenMode.Windowed,
                2 => FullScreenMode.ExclusiveFullScreen,
                _ => FullScreenMode.FullScreenWindow
            };

            ResolutionManager.Instance.SetResolution(res.width, res.height, mode);
        }

        /// <summary>취소 버튼 OnClick에 연결</summary>
        public void OnCancel()
        {
            gameObject.SetActive(false);
        }
    }
}
