using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

namespace Jun
{
    public class UnitModel : MonoBehaviour
    {
        [SerializeField] private GamePlayerController _controller;
        [SerializeField] private PlayerInfo _info;
        [SerializeField] private List<ItemInfo> _inventory = new List<ItemInfo>();
        Animator anim;
        public PlayerInfo Info => _info; //읽기전용

        private int _selectedSkill = -1;   //선택된 skill
        private int _selectedItem = -1;     //선택된 아이템    
        private List<int> _selectedTarget = new List<int>();   // 타겟들

        private int _targetNum = -1;   //적용할 타겟의 수

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
            Debug.Log("스킬선택 " + index);
            _selectedSkill = index;
            _selectedItem = -1;
            _selectedTarget.Clear();

            _targetNum = _info.Skills[index].TagetNum;
            
        }
        public void SelectedItem(int index)
        {
            Debug.Log("아이템 선택");

            _selectedItem = index;
            _selectedSkill = -1;
            _selectedTarget.Clear();

            _targetNum = _info.Items[index].TagetNum;
        }
        public void SelectedEnemy(int index)
        {
            Debug.Log("타겟 선택");

            _selectedTarget.Add(index);
            if (_targetNum > 0 && _targetNum == _selectedTarget.Count)
            {
                _controller.CMDSelectionComplete(_selectedSkill, _selectedItem, _selectedTarget);
                _selectedTarget.Clear();
                _targetNum = 0;
            }
        }
    }

}
