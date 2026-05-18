using System.Collections;
using UnityEngine;

namespace inseon.Start
{
    public class StartSceneUI : MonoBehaviour
    {
        [Header("팝업 패널 (RectTransform)")]
        [SerializeField] private RectTransform _loginPanel;
        [SerializeField] private RectTransform _registerPanel;

        [Header("슬라이드 설정")]
        [SerializeField] private float _slideDuration = 0.35f;

        [Tooltip("숨길 때 이동할 Y 오프셋. Canvas 해상도에 맞게 조정하세요.")]
        [SerializeField] private float _hiddenOffsetY = -1300f;

        private Vector2 _loginShownPos;
        private Vector2 _registerShownPos;

        private Coroutine _loginAnim;
        private Coroutine _registerAnim;


        private void Awake()
        {
            _loginShownPos    = _loginPanel.anchoredPosition;
            _registerShownPos = _registerPanel.anchoredPosition;

            SetHidden(_loginPanel,    _loginShownPos);
            SetHidden(_registerPanel, _registerShownPos);
        }


        public void OnClickStart()
        {
            RunSlide(ref _loginAnim, _loginPanel, _loginShownPos);
        }

        public void OnClickSetting()
        {
            Debug.Log("[StartSceneUI] Setting 버튼 – 미구현");
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
    }
}
