using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace Jun
{
    public class PlayerView : MonoBehaviour
    {
        public List<Button> SkillBtn;
        public List<Button> ItemBtn;
        public List<Button> EnemyBtn;  //적 버튼
        public Button HpBar;
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
    }
}
