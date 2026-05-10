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
        
        
    }
}
