using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class RoundUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI _roundText;
    [SerializeField] CanvasGroup _canvasGroup;

    public void ShowRound(int round)
    {
        _roundText.text = "Round "+round;
        StopAllCoroutines();
        gameObject.SetActive(true);       
        StartCoroutine(RoundAnim());
    }

    private IEnumerator RoundAnim()
    {
        // 페이드 인
        _canvasGroup.alpha = 0f;
        gameObject.SetActive(true);
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / 0.4f;
            _canvasGroup.alpha = t;
            yield return null;
        }

        // 잠깐 유지
        yield return new WaitForSeconds(1.2f);

        // 페이드 아웃
        t = 1f;
        while (t > 0f)
        {
            t -= Time.deltaTime / 0.4f;
            _canvasGroup.alpha = t;
            yield return null;
        }
        gameObject.SetActive(false);
    }
}
