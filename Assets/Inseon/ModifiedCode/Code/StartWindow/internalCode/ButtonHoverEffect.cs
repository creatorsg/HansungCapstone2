using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class ButtonHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("스케일 설정")]
    [SerializeField] private float _hoverScale = 1.08f;   
    [SerializeField] private float _animDuration = 0.15f;   

    [Header("글로우 설정")]
    [SerializeField] private Image _glowImage;
    [SerializeField] private Color _glowColor = new Color(0.85f, 0.05f, 0.05f, 0.75f);

    private Vector3 _originScale;
    private Coroutine _scaleCo;
    private Coroutine _glowCo;

    private void Awake()
    {
        _originScale = transform.localScale;

        if (_glowImage != null)
            _glowImage.color = new Color(_glowColor.r, _glowColor.g, _glowColor.b, 0f);
    }

    public void OnPointerEnter(PointerEventData _)
    {
        PlayScale(_originScale * _hoverScale);
        PlayGlow(_glowColor.a);
    }

    public void OnPointerExit(PointerEventData _)
    {
        PlayScale(_originScale);
        PlayGlow(0f);
    }

    private void PlayScale(Vector3 target)
    {
        if (_scaleCo != null) StopCoroutine(_scaleCo);
        _scaleCo = StartCoroutine(Co_Scale(target));
    }

    private void PlayGlow(float targetAlpha)
    {
        if (_glowImage == null) return;
        if (_glowCo != null) StopCoroutine(_glowCo);
        _glowCo = StartCoroutine(Co_Glow(targetAlpha));
    }

    private IEnumerator Co_Scale(Vector3 target)
    {
        Vector3 start = transform.localScale;
        float elapsed = 0f;

        while (elapsed < _animDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / _animDuration);
            transform.localScale = Vector3.LerpUnclamped(start, target, SmoothStep(t));
            yield return null;
        }

        transform.localScale = target;
    }

    private IEnumerator Co_Glow(float targetAlpha)
    {
        Color startColor = _glowImage.color;
        Color endColor = new Color(_glowColor.r, _glowColor.g, _glowColor.b, targetAlpha);
        float elapsed = 0f;

        while (elapsed < _animDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / _animDuration);
            _glowImage.color = Color.LerpUnclamped(startColor, endColor, SmoothStep(t));
            yield return null;
        }

        _glowImage.color = endColor;
    }

    private static float SmoothStep(float t) => t * t * (3f - 2f * t);
}