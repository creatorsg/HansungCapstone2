using Mirror;
using System;
using System.Collections.Generic;
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
        public Image Sel; //�ڽ��� �����϶� ��Ÿ���� �̹���
        void Awake()
        {
            anim = GetComponent<Animator>();

            // HpBar Slider의 자식 Image들과 Sel Image는 클릭 이벤트를 받을 필요가 없습니다.
            // raycastTarget = true (Unity 기본값)이면 Physics2DRaycaster + EventTrigger 클릭을
            // 가로채서 캐릭터 클릭이 Game 뷰에서 동작하지 않으므로 비활성화합니다.
            if (HpBar != null)
            {
                foreach (var img in HpBar.GetComponentsInChildren<Image>(true))
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
            HpBar.value = currentHp;

        }

        public void SkillAnim(string skill)
        {
            anim.SetBool(skill, true);
        }
        public void EndAnim(string name)
        {
            anim.SetBool(name, false);
            EndMyTurn?.Invoke();
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
            anim.SetBool("Dead", true);
        }
    }
}
