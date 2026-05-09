using System.Collections;
using UnityEngine;
using UnityEngine.UI;


public class ButtonGuard : MonoBehaviour
{
    public static ButtonGuard Instance { get; private set; }

    [Tooltip("잠금 대상 버튼들이 속한 패널. CanvasGroup 컴포넌트가 필요합니다.")]
    [SerializeField] private CanvasGroup _lockableArea;

    private void Awake()
    {
        Instance = this;
    }

    public static bool TryLock()
    {
        if (Instance == null || Instance._lockableArea == null) return true;
        if (!Instance._lockableArea.interactable) return false;

        Instance._lockableArea.interactable = false;
        return true;
    }

    public static void Unlock()
    {
        if (Instance?._lockableArea != null)
            Instance._lockableArea.interactable = true;
    }

    /// <summary>
    /// seconds 초 동안 잠근 뒤 자동으로 해제합니다.
    /// Mirror Command처럼 완료 콜백이 없는 동작에 사용하세요.
    /// </summary>
    public static void LockFor(float seconds)
    {
        if (Instance == null) return;
        Instance.StartCoroutine(Instance.LockRoutine(seconds));
    }

    private IEnumerator LockRoutine(float seconds)
    {
        if (_lockableArea == null) yield break;
        _lockableArea.interactable = false;
        yield return new WaitForSeconds(seconds);
        _lockableArea.interactable = true;
    }

    public static bool IsLocked
        => Instance != null && Instance._lockableArea != null
           && !Instance._lockableArea.interactable;
}

