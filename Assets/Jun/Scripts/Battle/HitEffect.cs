using UnityEngine;
using System.Collections;

public class HitEffect : MonoBehaviour
{
    [SerializeField] private float _delay = 2f;    // 대기 시간
    [SerializeField] private float _duration = 0.5f; // 페이드 아웃 시간

    void Start() => StartCoroutine(FadeAndDestroy());

    private IEnumerator FadeAndDestroy()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            Destroy(gameObject);
            yield break;
        }
        // 대기시간 처리
        yield return new WaitForSeconds(_delay);

        // 페이드 아웃 처리
        float elapsed = 0f;
        Color startColor = sr.color;

        while (elapsed < _duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / _duration);

            sr.color = new Color(startColor.r, startColor.g, startColor.b, alpha);

            yield return null;
        }
        Destroy(gameObject);
    }
}
