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
        public List<Button> EnemyBtn;  //적 버튼
        public Slider HpBar;
        public Image Sel; //자신의 차례일때 나타내는 이미지
        void Awake()
        {
            anim = GetComponent<Animator>();
        }
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            SetButtonsInteractable(false, SkillBtn);
            SetButtonsInteractable(false, ItemBtn);
            SetButtonsInteractable(false, EnemyBtn);
        }
        [ClientRpc]
        public void RpcSetSel(bool IsMyTurn)
        {
            Sel.gameObject.SetActive(IsMyTurn);
        }
        public void SetButtonsInteractable(bool state, List<Button> Btn) // 버튼 활성화 선택
        {
            foreach (var btn in Btn)
            {
                btn.interactable = state;
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
