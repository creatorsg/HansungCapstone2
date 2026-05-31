using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace inseon.Start
{
    public class StartSceneUI : MonoBehaviour
    {
        [Header("팝업 패널 (RectTransform)")]
        [SerializeField] private RectTransform _loginPanel;
        [SerializeField] private RectTransform _registerPanel;
        [SerializeField] private RectTransform _optionPanel;

        [Header("슬라이드 설정")]
        [SerializeField] private float _slideDuration = 0.35f;

        [Tooltip("숨길 때 이동할 Y 오프셋. Canvas 해상도에 맞게 조정하세요.")]
        [SerializeField] private float _hiddenOffsetY = -1300f;

        private Vector2 _loginShownPos;
        private Vector2 _registerShownPos;
        private Vector2 _optionShownPos;

        private Coroutine _loginAnim;
        private Coroutine _registerAnim;
        private Coroutine _optionAnim;
        private AudioSource _bgmAudioSource;
        private Slider _bgmVolumeSlider;

        private void Awake()
        {
            EnsureOptionPanel();

            if (_loginPanel != null)
            {
                _loginShownPos = _loginPanel.anchoredPosition;
                SetHidden(_loginPanel, _loginShownPos);
            }

            if (_registerPanel != null)
            {
                _registerShownPos = _registerPanel.anchoredPosition;
                SetHidden(_registerPanel, _registerShownPos);
            }

            if (_optionPanel != null)
            {
                _optionShownPos = _optionPanel.anchoredPosition;
                SetHidden(_optionPanel, _optionShownPos);
            }
        }


        public void OnClickStart()
        {
            RunSlide(ref _loginAnim, _loginPanel, _loginShownPos);
        }

        public void OnClickSetting()
        {
            if (_optionPanel == null)
            {
                Debug.LogError("[StartSceneUI] OptionPanel을 찾을 수 없습니다.");
                return;
            }

            SyncBgmVolumeSlider();
            RunSlide(ref _optionAnim, _optionPanel, _optionShownPos);
        }

        public void OnClickCloseSetting()
        {
            if (_optionPanel == null) return;

            RunSlide(ref _optionAnim, _optionPanel, HiddenPos(_optionPanel, _optionShownPos));
        }

        public void OnBgmVolumeChanged(float volume)
        {
            ResolveBgmAudioSource();
            if (_bgmAudioSource != null)
                _bgmAudioSource.volume = Mathf.Clamp01(volume);
        }

        public void OnClickQuit()
        {
            SystemSetting.Instance.QuitGame();
        }

        public void OnClickOpenRegister()
        {
            RunSlide(ref _loginAnim,    _loginPanel,    HiddenPos(_loginPanel,    _loginShownPos));
            RunSlide(ref _registerAnim, _registerPanel, _registerShownPos);
        }


        public void OnClickCloseRegister()
        {
            RunSlide(ref _registerAnim, _registerPanel, HiddenPos(_registerPanel, _registerShownPos));
            RunSlide(ref _loginAnim,    _loginPanel,    _loginShownPos);
        }

        public void OnClickCloseLogin()
        {
            RunSlide(ref _loginAnim, _loginPanel, HiddenPos(_loginPanel, _loginShownPos));
        }


        private void SetHidden(RectTransform panel, Vector2 shownPos)
        {
            panel.anchoredPosition = HiddenPos(panel, shownPos);
        }

        private Vector2 HiddenPos(RectTransform panel, Vector2 shownPos)
        {
            return new Vector2(shownPos.x, shownPos.y + _hiddenOffsetY);
        }

        private void RunSlide(ref Coroutine handle, RectTransform panel, Vector2 target)
        {
            if (panel == null) return;

            if (handle != null) StopCoroutine(handle);
            handle = StartCoroutine(SlideTo(panel, target));
        }

        private IEnumerator SlideTo(RectTransform panel, Vector2 target)
        {
            Vector2 start   = panel.anchoredPosition;
            float   elapsed = 0f;

            while (elapsed < _slideDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / _slideDuration));
                panel.anchoredPosition = Vector2.Lerp(start, target, t);
                yield return null;
            }

            panel.anchoredPosition = target;
        }

        private void EnsureOptionPanel()
        {
            ResolveBgmAudioSource();

            if (_optionPanel == null)
                _optionPanel = CreateOptionPanel();

            if (_optionPanel == null) return;

            _bgmVolumeSlider = _optionPanel.GetComponentInChildren<Slider>(true);
            if (_bgmVolumeSlider == null) return;

            _bgmVolumeSlider.onValueChanged.RemoveListener(OnBgmVolumeChanged);
            _bgmVolumeSlider.onValueChanged.AddListener(OnBgmVolumeChanged);
            SyncBgmVolumeSlider();
        }

        private void ResolveBgmAudioSource()
        {
            if (_bgmAudioSource != null) return;

            var bgmManager = Object.FindFirstObjectByType<Lsy.BGMManager>();
            if (bgmManager != null)
                _bgmAudioSource = bgmManager.GetComponent<AudioSource>();
        }

        private void SyncBgmVolumeSlider()
        {
            ResolveBgmAudioSource();
            if (_bgmVolumeSlider == null || _bgmAudioSource == null) return;

            _bgmVolumeSlider.SetValueWithoutNotify(_bgmAudioSource.volume);
        }

        private RectTransform CreateOptionPanel()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[StartSceneUI] OptionPanel을 생성할 Canvas를 찾을 수 없습니다.");
                return null;
            }

            RectTransform panel = CreateRect("OptionPanel", canvas.transform);
            panel.sizeDelta = new Vector2(520f, 260f);
            AddImage(panel.gameObject, new Color(0.08f, 0.08f, 0.08f, 0.95f));

            Text title = CreateText("Title", panel, "BGM Volume", 28, TextAnchor.MiddleCenter);
            title.rectTransform.anchoredPosition = new Vector2(0f, 75f);
            title.rectTransform.sizeDelta = new Vector2(420f, 45f);

            Slider slider = CreateSlider(panel);
            slider.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 5f);

            Button closeButton = CreateButton(panel, "Close");
            closeButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -80f);
            closeButton.onClick.AddListener(OnClickCloseSetting);

            return panel;
        }

        private static Slider CreateSlider(RectTransform parent)
        {
            RectTransform sliderRect = CreateRect("BgmVolumeSlider", parent);
            sliderRect.sizeDelta = new Vector2(360f, 34f);

            Slider slider = sliderRect.gameObject.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;

            RectTransform background = CreateRect("Background", sliderRect);
            background.anchorMin = new Vector2(0f, 0.25f);
            background.anchorMax = new Vector2(1f, 0.75f);
            background.sizeDelta = Vector2.zero;
            AddImage(background.gameObject, new Color(0.25f, 0.25f, 0.25f, 1f));

            RectTransform fillArea = CreateRect("Fill Area", sliderRect);
            fillArea.anchorMin = new Vector2(0f, 0.25f);
            fillArea.anchorMax = new Vector2(1f, 0.75f);
            fillArea.offsetMin = new Vector2(8f, 0f);
            fillArea.offsetMax = new Vector2(-8f, 0f);

            RectTransform fill = CreateRect("Fill", fillArea);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = Vector2.one;
            fill.sizeDelta = Vector2.zero;
            AddImage(fill.gameObject, new Color(0.35f, 0.7f, 1f, 1f));

            RectTransform handleArea = CreateRect("Handle Slide Area", sliderRect);
            handleArea.anchorMin = Vector2.zero;
            handleArea.anchorMax = Vector2.one;
            handleArea.offsetMin = new Vector2(8f, 0f);
            handleArea.offsetMax = new Vector2(-8f, 0f);

            RectTransform handle = CreateRect("Handle", handleArea);
            handle.sizeDelta = new Vector2(24f, 34f);
            Image handleImage = AddImage(handle.gameObject, Color.white);

            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handleImage;
            return slider;
        }

        private static Button CreateButton(RectTransform parent, string label)
        {
            RectTransform buttonRect = CreateRect("CloseButton", parent);
            buttonRect.sizeDelta = new Vector2(160f, 50f);

            Image image = AddImage(buttonRect.gameObject, new Color(0.2f, 0.45f, 0.7f, 1f));
            Button button = buttonRect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            Text text = CreateText("Label", buttonRect, label, 22, TextAnchor.MiddleCenter);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.sizeDelta = Vector2.zero;
            return button;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            var rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            return rect;
        }

        private static Image AddImage(GameObject gameObject, Color color)
        {
            Image image = gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static Text CreateText(string name, Transform parent, string value, int fontSize, TextAnchor alignment)
        {
            RectTransform rect = CreateRect(name, parent);
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            return text;
        }
    }
}
