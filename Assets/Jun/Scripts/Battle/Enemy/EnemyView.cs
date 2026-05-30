using Mirror;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
public class EnemyView : NetworkBehaviour
{
    Animator anim;
    [SerializeField] private Slider HpBar;
    private Coroutine _hpCoroutine;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        anim = GetComponentInChildren<Animator>();
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
        if(skill == "Attack") anim.SetBool(skill, true);
        else  anim.SetTrigger(skill);
    }
    public void StopAnim()
    {
        if (anim == null) return;
        anim.SetBool("Attack", false);  // Attack Bool 리셋
        anim.Play("Idle");              // Idle 상태로 강제 전환
    }
}
