using Jun;
using Mirror;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
public class EnemyView : NetworkBehaviour
{
    Animator anim;
    [SerializeField] private Slider HpBar;
    private Coroutine _hpCoroutine;
    private Vector3 _originPos;
    private Vector3 _originScale;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        anim = GetComponentInChildren<Animator>();
        _originPos = transform.position;
        _originScale = transform.localScale;
    }

    public void Damaged(float _currentHp)
    {
        if (_hpCoroutine != null) StopCoroutine(_hpCoroutine);
        _hpCoroutine = StartCoroutine(SmoothHpBar(_currentHp));
    }
    private IEnumerator SmoothHpBar(float target)
    {
        float start = HpBar.value;
        float elapsed = 0f;
        float duration = 0.3f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            HpBar.value = Mathf.Lerp(start, target, elapsed / duration);
            yield return null;
        }
        HpBar.value = target;
    }
    public void SkillAnim(string skill)
    {
        if (anim == null) { Debug.LogError("anim null!"); return; }
        Debug.Log($"SkillAnim 호출: {skill}");
        if (skill == "Attack")
        {
            StartCoroutine(StepForward());  
            anim.SetBool(skill, true);
        }
        else if (skill == "Damaged")
        {
            anim.SetTrigger(skill);
        }
        else
        {
            anim.SetTrigger(skill);
        }
    }
    public void StopAnim()
    {
        if (anim == null) return;
        anim.SetBool("Attack", false);  // Attack Bool 리셋
        anim.Play("Idle");              // Idle 상태로 강제 전환
        StartCoroutine(StepBack());
    }
    private IEnumerator StepForward()
    {
        Vector3 targetPos = BattleManager.Instance._emAnimPos.position; // 왼쪽 플레이어 방향
        Vector3 targetScale = _originScale * 2f;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / 0.12f;
            transform.position = Vector3.Lerp(_originPos, targetPos, Mathf.SmoothStep(0, 1, t));
            transform.localScale = Vector3.Lerp(_originScale, targetScale, t);
            yield return null;
        }
    }

    private IEnumerator StepBack()
    {
        Vector3 fromPos = transform.position;
        Vector3 fromScale = transform.localScale;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / 0.2f;
            transform.position = Vector3.Lerp(fromPos, _originPos, t);
            transform.localScale = Vector3.Lerp(fromScale, _originScale, t);
            yield return null;
        }
        transform.position = _originPos;
        transform.localScale = _originScale;
    }
}
