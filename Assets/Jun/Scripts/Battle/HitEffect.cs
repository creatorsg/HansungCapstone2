using UnityEngine;
using System.Collections;
public class HitEffect : MonoBehaviour
{
    [SerializeField] private float _duration = 0.5f;
    [SerializeField] private float firstTime = 2f;
    void Start() => StartCoroutine(FadeAndDestroy());

    private IEnumerator FadeAndDestroy()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr == null) { Destroy(gameObject); yield break; }

        float elapsed = 0f;
        Color originalColor = sr.color;

        while (elapsed < _duration)
        {
            if (elapsed <= firstTime) { elapsed += Time.unscaledDeltaTime; }
            else
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsed / _duration);
                sr.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
                yield return null;
            }
        }

        Destroy(gameObject);
    }
}
