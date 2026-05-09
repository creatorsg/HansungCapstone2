using System.Linq;
using UnityEngine;

namespace inseon.Core
{
    /// <summary>
    /// 게임 시작 시 모니터 네이티브 해상도 자동 적용.
    /// 이후 플레이어가 변경한 설정은 PlayerPrefs에 저장/복원.
    /// Login 씬의 오브젝트에 부착 후 DontDestroyOnLoad로 유지.
    /// </summary>
    public class ResolutionManager : MonoBehaviour
    {
        public static ResolutionManager Instance { get; private set; }

        private Resolution[] _availableResolutions;

        private const string KEY_WIDTH  = "ResWidth";
        private const string KEY_HEIGHT = "ResHeight";
        private const string KEY_MODE   = "ScreenMode";

        void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitResolutions();
            ApplySavedOrNativeResolution();
        }

        // ── 초기화 ────────────────────────────────────────────

        void InitResolutions()
        {
            // 중복 해상도 제거 (Hz만 다른 경우), 낮은 순 정렬
            _availableResolutions = Screen.resolutions
                .GroupBy(r => new { r.width, r.height })
                .Select(g => g.Last())          // 해당 해상도의 최대 Hz 선택
                .OrderBy(r => r.width)
                .ThenBy(r => r.height)
                .ToArray();
        }

        void ApplySavedOrNativeResolution()
        {
            if (!PlayerPrefs.HasKey(KEY_WIDTH))
            {
                // 첫 실행: 모니터 네이티브 해상도로 자동 설정
                ApplyNativeResolution();
                return;
            }

            // 저장된 설정 복원
            int w    = PlayerPrefs.GetInt(KEY_WIDTH);
            int h    = PlayerPrefs.GetInt(KEY_HEIGHT);
            int mode = PlayerPrefs.GetInt(KEY_MODE, (int)FullScreenMode.FullScreenWindow);

            Screen.SetResolution(w, h, (FullScreenMode)mode);
            Debug.Log($"[ResolutionManager] 저장된 해상도 적용: {w}x{h}, 모드: {(FullScreenMode)mode}");
        }

        void ApplyNativeResolution()
        {
            // 지원 해상도 중 가장 큰 값 = 네이티브 해상도
            var native = _availableResolutions.Last();
            SetResolution(native.width, native.height, FullScreenMode.FullScreenWindow);
            Debug.Log($"[ResolutionManager] 네이티브 해상도 적용: {native.width}x{native.height}");
        }

        // ── 공개 메서드 ───────────────────────────────────────

        /// <summary>해상도 변경 및 PlayerPrefs 저장</summary>
        public void SetResolution(int width, int height, FullScreenMode mode)
        {
            Screen.SetResolution(width, height, mode);

            PlayerPrefs.SetInt(KEY_WIDTH,  width);
            PlayerPrefs.SetInt(KEY_HEIGHT, height);
            PlayerPrefs.SetInt(KEY_MODE,   (int)mode);
            PlayerPrefs.Save();

            Debug.Log($"[ResolutionManager] 해상도 변경: {width}x{height}, 모드: {mode}");
        }

        /// <summary>현재 모니터가 지원하는 해상도 목록 반환</summary>
        public Resolution[] GetAvailableResolutions() => _availableResolutions;
    }
}
