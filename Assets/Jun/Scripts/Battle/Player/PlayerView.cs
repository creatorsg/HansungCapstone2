using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace Jun
{
    public class PlayerView : NetworkBehaviour
    {
        public event Action EndMyTurn;
        Animator anim;
        public List<Button> SkillBtn;
        public List<Button> ItemBtn;
        public List<Button> EnemyBtn;  //�� ��ư
        public Slider HpBar;
        public Slider SanBar;
        public Image Sel; //�ڽ��� �����϶� ��Ÿ���� �̹���
        // 애니메이션처리 코루틴
        private Coroutine _hpCoroutine;
        private Coroutine _pulseCoroutine;
        // 애니메이션 전 기존 정보 저장
        private Vector3 _originPos;
        private Vector3 _originScale;

        [SerializeField] private TextMeshProUGUI _damagedText;
        private Vector3 _textOriginPos;
        void Awake()
        {
            anim = GetComponentInChildren<Animator>();
            Debug.Log($"anim 잡힌 오브젝트: {anim.gameObject.name}");

            // HpBar Slider의 자식 Image들과 Sel Image는 클릭 이벤트를 받을 필요가 없습니다.
            // raycastTarget = true (Unity 기본값)이면 Physics2DRaycaster + EventTrigger 클릭을
            // 가로채서 캐릭터 클릭이 Game 뷰에서 동작하지 않으므로 비활성화합니다.
            if (HpBar != null)
            {
                foreach (var img in HpBar.GetComponentsInChildren<Image>(true))
                    img.raycastTarget = false;
            }
            if (SanBar != null)
            {
                foreach (var img in SanBar.GetComponentsInChildren<Image>(true))
                    img.raycastTarget = false;
            }
            if (Sel != null)
                Sel.raycastTarget = false;
        }
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            _originPos = transform.position;
            _originScale = transform.localScale;
            SetButtonsInteractable(false, SkillBtn);
            SetButtonsInteractable(false, ItemBtn);
            SetButtonsInteractable(false, EnemyBtn);
            if (_damagedText != null) _textOriginPos = _damagedText.transform.position;
        }
        public void InitBars(float currentHp, float maxHp)
        {
            if (HpBar != null)
            {
                HpBar.minValue = 0f;
                HpBar.maxValue = maxHp;
                HpBar.value = currentHp;
            }
        }
        public void SetSel(bool IsMyTurn)
        {
            Sel.gameObject.SetActive(IsMyTurn);
            if (_pulseCoroutine != null) StopCoroutine(_pulseCoroutine);
            if (IsMyTurn)
                _pulseCoroutine = StartCoroutine(PulseSel());
        }
        private IEnumerator PulseSel()
        {
            RectTransform rt = Sel.rectTransform;
            while (true)
            {
                float t = (Mathf.Sin(Time.time * 3f) + 1f) / 2f;
                float scaleX = Mathf.Lerp(0.009f, 0.011f, t);
                float scaleY = 0.01f; 
                rt.localScale = new Vector3(scaleX, scaleY, 1f);
                yield return null;
            }
        }
        [ClientRpc]
        public void RPCPlShowDamagedText(bool isHit, float damaged, Color color)
        {
            Debug.Log(" RPCPlShowDamagedText 중");
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
        public void UpdateOriginPos()
        {
            _originPos = transform.position;
            _originScale = transform.localScale;
        }
        public void SetButtonsInteractable(bool state, List<Button> Btn) // 버튼 활성화 설정
        {
            if (Btn == null) return;
            foreach (var btn in Btn)
            {
                if (btn != null) btn.interactable = state;
            }
        }
        public void PlDamaged(float currentHp)
        {
            if (_hpCoroutine != null) StopCoroutine(_hpCoroutine);
            _hpCoroutine = StartCoroutine(SmoothHpBar(currentHp));

        }
        [ClientRpc]
        public void RPCPlHPChanged(float currentHp)
        {
            if (_hpCoroutine != null) StopCoroutine(_hpCoroutine);
            _hpCoroutine = StartCoroutine(SmoothHpBar(currentHp));
            if (currentHp <= 0) PlayDead();
        }
        public void PlSanChanged(float currentSan)
        {
            SanBar.value = currentSan;
        }

        public void SkillAnim(string skill, bool isRevive)
        {
            if (anim == null) { Debug.LogError("anim null!"); return; }
            Debug.Log($"SkillAnim 호출: {skill}");
            //anim.SetBool(skill, true);
            StartCoroutine(WindUpThenAttack(skill, isRevive));
        }
        private IEnumerator WindUpThenAttack(string skill, bool isRevive)
        {
            if (isRevive)
            {
                anim.SetBool(skill, true);
                yield break; 
            }
            StartCoroutine(StepForward());  //앞으로 나오기
            BattleEffectManager.Instance?.StepTargetsForward(-1.5f);
            anim.SetBool(skill, true);
            anim.speed = 0f;                              // 첫 프레임에서 동결
            yield return new WaitForSecondsRealtime(0.15f); // 0.15초 홀드
            anim.speed = 1f;                              // 이후 공격모션 재생
        }
        public void EndAnim(string name)
        {
            anim.SetBool(name, false);
            StartCoroutine(StepBack());
            BattleEffectManager.Instance?.EndAttack();
            EndMyTurn?.Invoke();
        }
        private IEnumerator StepForward()
        {
            Vector3 targetPos = BattleManager.Instance._plAnimPos.position; // 오른쪽(적 방향)
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
        // ── 피격 / 회피 / 사망 애니메이션 ───────────────────────────
        // 기존 attack(Bool)과 달리 Trigger를 사용합니다.
        // Trigger는 SetTrigger 한 번만 호출하면 자동 소모되므로
        // 별도의 "끄기" 호출이 필요 없습니다.

        /// <summary>피격(damaged) 애니메이션 재생. 종료 후 idle로 자동 복귀합니다.</summary>
        public void PlayDamaged()
        {
            if (anim == null) return;
            BattleEffectManager.Instance?.RegisterTarget(transform);
            anim.SetTrigger("Damaged");
        }

        /// <summary>회피(dodge) 애니메이션 재생. 종료 후 idle로 자동 복귀합니다.</summary>
        public void PlayDodge()
        {
            if (anim == null) return;
            BattleEffectManager.Instance?.RegisterTarget(transform, false);
            anim.SetTrigger("Dodge");
        }

        /// <summary>사망(dead) 애니메이션 재생. dead 상태에서 idle로 돌아오지 않습니다.</summary>
        public void PlayDead()
        {
            if (anim == null) return;
            StartCoroutine(DelayedDead());
        }
        private IEnumerator DelayedDead()
        {
            yield return new WaitForSeconds(1.0f); // Damaged 애니 끝날 때까지 대기
            anim.SetTrigger("Dead");
        }
        /// <summary>스폰 직후 이미 사망 상태일 때 즉시 dead 연출 (딜레이 없음).</summary>
        public void PlayDeadImmediate()
        {
            if (anim == null) return;
            anim.SetTrigger("Dead");
            if (HpBar != null) HpBar.value = 0f;
        }
    }
}
