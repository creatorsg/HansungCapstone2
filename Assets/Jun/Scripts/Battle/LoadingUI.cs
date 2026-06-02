using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadingUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI _loadingText;
    [SerializeField] Image _loadingIMG;

    public void StartLoading()
    {
        gameObject.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(LoadingTextRoutine());
    }

    public void EndLoading()
    {
        StopAllCoroutines(); 
        StartCoroutine(FadeOutRoutine());
    }

    private IEnumerator LoadingTextRoutine()
    {
        int dotCount = 0;

        // changeSpeed에 따라 점 찍히는 속도가 달라짐
        float changeSpeed = 0.5f;

        while (true)
        {
            _loadingText.text = "Loading" + new string('.', dotCount);
            dotCount = (dotCount + 1) % 4;
            yield return new WaitForSeconds(changeSpeed);
        }
    }

    private IEnumerator FadeOutRoutine()
    {
        float t = 1f;
        float fadeOutDuration = 0.5f;

        while (t > 0f)
        {
            t -= Time.deltaTime / fadeOutDuration;

            Color imgColor = _loadingIMG.color;
            imgColor.a = t;
            _loadingIMG.color = imgColor;

            Color txtColor = _loadingText.color;
            txtColor.a = t;
            _loadingText.color = txtColor;

            yield return null;
        }

        gameObject.SetActive(false);
    }
}