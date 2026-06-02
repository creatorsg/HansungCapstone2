using Jun;
using Mirror;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class EnemyView : NetworkBehaviour
{
    Animator anim;
    [SerializeField] private Slider HpBar;
    private Coroutine _hpCoroutine;
    private Vector3 _originPos;
    private Vector3 _originScale;
    [SerializeField] private TextMeshProUGUI _damagedText;
    private Vector3 _textOriginPos;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        anim = GetComponentInChildren<Animator>();
        _originPos = transform.position;
        _originScale = transform.localScale;
        if(_damagedText!=null) _textOriginPos = _damagedText.transform.position;
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
    [ClientRpc]
    public void RPCShowDamagedText(bool isHit, float damaged, Color color)
    {
        if (_damagedText == null) return;
        _damagedText.color = color;
        if (isHit) _damagedText.text = damaged.ToString();
        else _damagedText.text = "MISS";
    }
    public void ShowDamagedTextNow()
    {
        if (_damagedText == null) return;
        StartCoroutine(FloatingTextCoroutine());
    }
    private IEnumerator FloatingTextCoroutine()
    {
        Debug.Log("테스트 성공");
        _damagedText.gameObject.SetActive(true);
        Vector3 startPos = _damagedText.transform.position;
        float elapsed = 0f;
        float duration = 0.6f;
        float speed = 1f;
        Color TextColor = _damagedText.color;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            _damagedText.transform.position = startPos + Vector3.up * (t * speed);
            _damagedText.alpha = Mathf.Lerp(1f, 0f, t);
            yield return null;
        }
        _damagedText.gameObject.SetActive(false);
    }
    public void SkillAnim(string skill)
    {
        if (anim == null) { Debug.LogError("anim null!"); return; }
        Debug.Log($"SkillAnim 호출: {skill}");
        if (skill == "Attack")
        {
            StartCoroutine(StepForward());
            BattleEffectManager.Instance?.StepTargetsForward(1.5f);
            anim.SetBool(skill, true);
        }
        else if (skill == "Damaged")
        {
            BattleEffectManager.Instance?.RegisterTarget(transform);
            anim.SetTrigger(skill);
        }
        else if (skill == "Dodge")
        {
            BattleEffectManager.Instance?.RegisterTarget(transform, false); 
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
        BattleEffectManager.Instance?.EndAttack();
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
