using Mirror;
using UnityEngine;
using UnityEngine.UI;
public class EnemyView : NetworkBehaviour
{
    Animator anim;
    [SerializeField] private Slider HpBar;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        anim = GetComponentInChildren<Animator>();
    }

    public void Damaged(float _currentHp)
    {
        HpBar.value = _currentHp;
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
