using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace Jun
{
    public class PlayerView : MonoBehaviour
    {
        public event Action EndMyTurn;
        Animator anim;
        public List<Button> SkillBtn;
        public List<Button> ItemBtn;
        public List<Button> EnemyBtn;  //적 버튼
        public Button HpBar;

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

        public void SetButtonsInteractable(bool state, List<Button> Btn) // 버튼 활성화 선택
        {
            foreach (var btn in Btn)
            {
                btn.interactable = state;
            }
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
