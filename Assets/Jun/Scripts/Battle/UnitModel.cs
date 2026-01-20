using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

namespace Jun
{// 이거는 멀티 서버 만들기전에 만들어둔 유닛 모델(참고용)
    public class UnitModel : MonoBehaviour
    {
        [SerializeField] private PlayerInfo _info;
        [SerializeField] private List<SkillInfo> _skills = new List<SkillInfo>();
        [SerializeField] private List<ItemInfo> _inventory = new List<ItemInfo>();
        Animator anim;
        public PlayerInfo Info => _info; //읽기전용

        private SkillInfo _selectedSkill;   //선택된 skill
        private ItemInfo _selectedItem;     //선택된 아이템    
        private List<int> _selectedTarget = new List<int>();   // 타겟들

        private int _targetNum;   //적용할 타겟의 수

        public void Start()
        {
            anim = GetComponent<Animator>();
        }
        public void SetUp(PlayerInfo info)
        {
            _info = info;
        }

        public void SelectedSkill(int index)
        {
            Debug.Log("스킬선택");
            _selectedSkill = _skills[index];
            _selectedItem = null;
            _selectedTarget.Clear();

            _targetNum = _selectedSkill.TagetNum;

            anim.SetBool("Attack", true);
        }
        public void SelectedItem(int index)
        {
            Debug.Log("아이템 선택");

            _selectedItem = _inventory[index];
            _selectedSkill = null;
            _selectedTarget.Clear();

            _targetNum = _selectedItem.TagetNum;
        }
        public void SelectedEnemy(int index)
        {
            Debug.Log("타겟 선택");

            _selectedTarget.Add(index);
            if (_targetNum > 0 && _targetNum == _selectedTarget.Count) BattleLogic.Instance.BattleAction(_selectedSkill, _selectedItem, this, _selectedTarget);
        }
    }

}
