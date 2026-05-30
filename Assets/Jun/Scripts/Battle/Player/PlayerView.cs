using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;


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
        void Awake()
        {
            anim = GetComponentInChildren<Animator>();
            Debug.Log($"anim 잡힌 오브젝트: {anim?.gameObject.name}");

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
            SetButtonsInteractable(false, SkillBtn);
            SetButtonsInteractable(false, ItemBtn);
            SetButtonsInteractable(false, EnemyBtn);
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
        public void PlHPChanged(float currentHp)
        {
            if (_hpCoroutine != null) StopCoroutine(_hpCoroutine);
            _hpCoroutine = StartCoroutine(SmoothHpBar(currentHp));
        }
        public void PlSanChanged(float currentSan)
        {
            SanBar.value = currentSan;
        }

        public void SkillAnim(string skill)
        {
            if (anim == null) { Debug.LogError("anim null!"); return; }
            Debug.Log($"SkillAnim 호출: {skill}");
            anim.SetBool(skill, true);
        }
        public void EndAnim(string name)
        {
            anim.SetBool(name, false);
            EndMyTurn?.Invoke();
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
            anim.SetTrigger("Damaged");
        }

        /// <summary>회피(dodge) 애니메이션 재생. 종료 후 idle로 자동 복귀합니다.</summary>
        public void PlayDodge()
        {
            if (anim == null) return;
            anim.SetTrigger("Dodge");
        }

        /// <summary>사망(dead) 애니메이션 재생. dead 상태에서 idle로 돌아오지 않습니다.</summary>
        public void PlayDead()
        {
            if (anim == null) return;
            anim.SetTrigger("Dead");
        }
    }
}
