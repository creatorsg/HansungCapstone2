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
        Debug.Log($"SkillAnim »£√‚: {skill}");
        if(skill == "Attack") anim.SetBool(skill, true);
        else  anim.SetTrigger(skill);
    }
}
