using System.Linq;
using UnityEngine;

namespace inseon.Core
{
    /// <summary>
    /// 게임 시작 시 모니터 네이티브 해상도 자동 적용.
    /// 이후 플레이어가 변경한 설정은 PlayerPrefs에 저장/복원.
    /// Login 씬의 오브젝트에 부착 후 DontDestroyOnLoad로 유지.
    ///
    /// ── 주의 ──
    ///  OnApplicationFocus에서 Screen.SetResolution을 호출하면
    ///  해상도 변경이 포커스 이벤트를 재트리거하는 무한 루프가 생겨 깜빡임이 심해집니다.
    ///  따라서 포커스 이벤트에서는 해상도를 재적용하지 않습니다.
    /// </summary>
    public class ResolutionManager : MonoBehaviour
    {
        public static ResolutionManager Instance { get; private set; }

        private Resolution[] _availableResolutions;

        private const string KEY_WIDTH   = "ResWidth";
        private const string KEY_HEIGHT  = "ResHeight";
        private const string KEY_MODE    = "ScreenMode";
        private const string KEY_VERSION = "ResVersion";

        // 이 값을 올리면 다음 실행 시 저장된 해상도가 초기화되고 기본값이 재적용됩니다.
        private const int SETTINGS_VERSION = 2;

        // 저장된 해상도가 이 값보다 작으면 잘못된 값으로 보고 기본값으로 재설정
        private const int MIN_WIDTH  = 1280;
        private const int MIN_HEIGHT = 720;

        // 저장값이 없거나 무효할 때 항상 이 해상도로 시작
        private const int DEFAULT_WIDTH  = 1920;
        private const int DEFAULT_HEIGHT = 1080;

        void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Alt+Tab 후 포커스 복귀 시 앱이 재시작되지 않도록 백그라운드 실행 유지
            Application.runInBackground = true;

            InitResolutions();
            MigrateSettingsIfNeeded();
            ApplySavedOrNativeResolution();
        }

        // ── 초기화 ────────────────────────────────────────────

        /// <summary>
        /// 설정 버전이 다르면 저장된 해상도를 초기화합니다.
        /// SETTINGS_VERSION 상수를 올릴 때마다 모든 클라이언트의 해상도가 기본값으로 리셋됩니다.
        /// </summary>
        void MigrateSettingsIfNeeded()
        {
            int savedVersion = PlayerPrefs.GetInt(KEY_VERSION, 0);
            if (savedVersion >= SETTINGS_VERSION) return;

            Debug.Log($"[ResolutionManager] 설정 버전 변경 ({savedVersion} → {SETTINGS_VERSION}) — 해상도 초기화");
            PlayerPrefs.DeleteKey(KEY_WIDTH);
            PlayerPrefs.DeleteKey(KEY_HEIGHT);
            PlayerPrefs.DeleteKey(KEY_MODE);
            PlayerPrefs.SetInt(KEY_VERSION, SETTINGS_VERSION);
            PlayerPrefs.Save();
        }

        void InitResolutions()
        {
            var raw = Screen.resolutions;

            if (raw == null || raw.Length == 0)
            {
                // 드라이버 문제 등으로 해상도 목록을 가져오지 못한 경우 기본값 배열 사용
                _availableResolutions = new Resolution[]
                {
                    new Resolution { width = DEFAULT_WIDTH, height = DEFAULT_HEIGHT }
                };
                Debug.LogWarning("[ResolutionManager] Screen.resolutions가 비어 있어 기본값 목록을 사용합니다.");
                return;
            }

            // 중복 해상도 제거(Hz만 다른 경우), 낮은 순 정렬
            _availableResolutions = raw
                .GroupBy(r => new { r.width, r.height })
                .Select(g => g.Last())      // 해당 해상도의 최대 Hz 선택
                .OrderBy(r => r.width)
                .ThenBy(r => r.height)
                .ToArray();
        }

        void ApplySavedOrNativeResolution()
        {
            if (!PlayerPrefs.HasKey(KEY_WIDTH))
            {
                // 첫 실행: 기본 해상도(1920×1080 이상) 적용
                ApplyDefaultResolution();
                return;
            }

            int w    = PlayerPrefs.GetInt(KEY_WIDTH);
            int h    = PlayerPrefs.GetInt(KEY_HEIGHT);
            int mode = PlayerPrefs.GetInt(KEY_MODE, (int)FullScreenMode.FullScreenWindow);

            // ExclusiveFullScreen은 D3D12와 충돌해 크래시/깜빡임 유발 → 강제 교정
            if ((FullScreenMode)mode == FullScreenMode.ExclusiveFullScreen)
            {
                mode = (int)FullScreenMode.FullScreenWindow;
                PlayerPrefs.SetInt(KEY_MODE, mode);
                PlayerPrefs.Save();
                Debug.LogWarning("[ResolutionManager] ExclusiveFullScreen → FullScreenWindow로 자동 교정");
            }

            // 저장된 해상도가 최솟값보다 작으면 잘못 저장된 것으로 판단하고 기본값으로 재설정
            if (w < MIN_WIDTH || h < MIN_HEIGHT)
            {
                Debug.LogWarning($"[ResolutionManager] 저장된 해상도({w}x{h})가 최소치 미만 — 기본값으로 재설정");
                PlayerPrefs.DeleteKey(KEY_WIDTH);
                PlayerPrefs.DeleteKey(KEY_HEIGHT);
                PlayerPrefs.Save();
                ApplyDefaultResolution();
                return;
            }

            Screen.SetResolution(w, h, (FullScreenMode)mode);
            Debug.Log($"[ResolutionManager] 저장된 해상도 적용: {w}x{h}, 모드: {(FullScreenMode)mode}");
        }

        void ApplyDefaultResolution()
        {
            // 항상 1920×1080으로 시작 (설정 창에서 직접 변경하기 전까지)
            SetResolution(DEFAULT_WIDTH, DEFAULT_HEIGHT, FullScreenMode.MaximizedWindow);
            Debug.Log($"[ResolutionManager] 기본 해상도 적용: {DEFAULT_WIDTH}x{DEFAULT_HEIGHT}");
        }

        // ── 공개 메서드 ───────────────────────────────────────

        /// <summary>해상도 변경 및 PlayerPrefs 저장</summary>
        public void SetResolution(int width, int height, FullScreenMode mode)
        {
            // ExclusiveFullScreen은 저장도, 적용도 차단
            if (mode == FullScreenMode.ExclusiveFullScreen)
            {
                Debug.LogWarning("[ResolutionManager] ExclusiveFullScreen 요청 차단 → FullScreenWindow로 변경");
                mode = FullScreenMode.FullScreenWindow;
            }

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
