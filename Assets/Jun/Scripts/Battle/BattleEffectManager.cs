using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;


namespace Jun
{
    public class BattleEffectManager : MonoBehaviour
    {
        public static BattleEffectManager Instance;

        [SerializeField] private Image _dimPanel;       // 검은 반투명 패널
        [SerializeField] private CanvasGroup _battleUI; // 스킬/아이템/턴 UI묶음
        [SerializeField] private Camera _camera;

        private List<Transform> _currentTargets = new List<Transform>();
        private List<Vector3> _targetOriginPositions = new List<Vector3>();
        private List<Vector3> _targetOriginScales = new List<Vector3>();

        [System.Serializable]
        public class HitEffectEntry
        {
            public string characterName; 
            public GameObject prefab;
        }

        [SerializeField] private List<HitEffectEntry> _hitEffectList;
        private Dictionary<string, GameObject> _hitEffectMap;
        private string _currentHitEffectName;

        private List<bool> _targetIsHit = new List<bool>();

        private Vector3 _cameraOrigin;
        private Coroutine _shakeCoroutine;

        void Awake()
        {
            Instance = this;
            _cameraOrigin = _camera.transform.localPosition;

            _hitEffectMap = new Dictionary<string, GameObject>();
            foreach (var entry in _hitEffectList)
                _hitEffectMap[entry.characterName] = entry.prefab;
        }
        public void SetHitEffectKey(string characterName)
        {
            _currentHitEffectName = characterName;
        }
        public void RegisterTarget(Transform t, bool isHit = true)
        {
            _currentTargets.Add(t);
            _targetOriginPositions.Add(t.position);
            _targetOriginScales.Add(t.localScale);
            _targetIsHit.Add(isHit);
        }
        public void StepTargetsForward(float direction)
        {
            for (int i = 0; i < _currentTargets.Count; i++)
            {
                if (_currentTargets[i] == null) continue;
                StartCoroutine(StepTransform(_currentTargets[i], _targetOriginPositions[i], _targetOriginScales[i], direction, _targetIsHit[i]));
            }
        }

        private IEnumerator StepTransform(Transform t, Vector3 originPos, Vector3 originScale, float direction, bool isHit)
        {
            Vector3 targetPos = originPos + new Vector3(direction, 0f, 0f);
            Vector3 targetScale = originScale * 2f;

            float elapsed = 0f;
            while (elapsed < 1f)
            {
                elapsed += Time.unscaledDeltaTime / 0.12f;
                t.position = Vector3.Lerp(originPos, targetPos, Mathf.SmoothStep(0, 1, elapsed));
                t.localScale = Vector3.Lerp(originScale, targetScale, elapsed);
                yield return null;
            }
            if (isHit && !string.IsNullOrEmpty(_currentHitEffectName) &&_hitEffectMap.TryGetValue(_currentHitEffectName, out var prefab))
            {
                Instantiate(prefab, t.position, Quaternion.identity);
            }
        }

        private IEnumerator StepBackTransform(Transform t, Vector3 originPos, Vector3 originScale)
        {
            Vector3 fromPos = t.position;
            Vector3 fromScale = t.localScale;

            float elapsed = 0f;
            while (elapsed < 1f)
            {
                elapsed += Time.unscaledDeltaTime / 0.2f;
                t.position = Vector3.Lerp(fromPos, originPos, elapsed);
                t.localScale = Vector3.Lerp(fromScale, originScale, elapsed);
                yield return null;
            }
            t.position = originPos;
            t.localScale = originScale;
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
            Debug.Log($"[HitEffect] OnHit 호출 / name: {_currentHitEffectName} / 타겟수: {_currentTargets.Count}");
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

            for (int i = 0; i < _currentTargets.Count; i++)
            {
                if (_currentTargets[i] == null) continue;
                StartCoroutine(StepBackTransform(_currentTargets[i], _targetOriginPositions[i], _targetOriginScales[i]));
            }
            _currentTargets.Clear();
            _targetOriginPositions.Clear();
            _targetOriginScales.Clear();

            _currentHitEffectName = string.Empty;
            _targetIsHit.Clear();
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