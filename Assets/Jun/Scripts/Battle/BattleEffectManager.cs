using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Jun
{
    public class BattleEffectManager : MonoBehaviour
    {
        public static BattleEffectManager Instance;

        [SerializeField] private Image _dimPanel;       // 검은 반투명 패널
        [SerializeField] private CanvasGroup _battleUI; // 스킬/아이템/턴 UI묶음
        [SerializeField] private Camera _camera;

        private Vector3 _cameraOrigin;
        private Coroutine _shakeCoroutine;

        void Awake()
        {
            Instance = this;
            _cameraOrigin = _camera.transform.localPosition;
        }

        // 공격 시작 시 호출(암전, UI숨김)
        public void StartAttack()
        {
            _dimPanel.gameObject.SetActive(true);
            _dimPanel.color = new Color(0, 0, 0, 0.55f);
            _battleUI.alpha = 0f;
            _battleUI.interactable = false;
            _battleUI.blocksRaycasts = false;
        }

        // 피격 순간 호출(히트 스톱, 화면 흔들림)
        public void OnHit(bool isCrit)
        {
            StartCoroutine(HitStop(isCrit ? 0.12f : 0.06f));
            if (_shakeCoroutine != null) StopCoroutine(_shakeCoroutine);
            _shakeCoroutine = StartCoroutine(Shake(isCrit ? 0.25f : 0.15f, isCrit ? 0.12f : 0.06f));
        }

        // 공격 끝날 떄 호출
        public void EndAttack()
        {
            _dimPanel.gameObject.SetActive(false);
            _battleUI.alpha = 1f;
            _battleUI.interactable = true;
            _battleUI.blocksRaycasts = true;
        }

        private IEnumerator HitStop(float duration)
        {
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(duration);
            Time.timeScale = 1f;
        }

        private IEnumerator Shake(float duration, float magnitude)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                float x = Random.Range(-1f, 1f) * magnitude;
                float y = Random.Range(-1f, 1f) * magnitude;
                _camera.transform.localPosition = _cameraOrigin + new Vector3(x, y, 0);
                elapsed += Time.unscaledDeltaTime; 
                yield return null;
            }
            _camera.transform.localPosition = _cameraOrigin;
        }
    }
}