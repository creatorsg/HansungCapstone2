using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System;


namespace Jun
{
    public class UnitModel : MonoBehaviour
    {
        [SerializeField] private GamePlayerController _controller;
        [SerializeField] private PlayerInfo _info;
        [SerializeField] private List<ItemInfo> _inventory = new List<ItemInfo>();
 
        Animator anim;
        public PlayerInfo Info => _info; //읽기전용

        [SerializeField]private int _selectedSkill = -1; public int SelectedSkill => _selectedSkill;  //선택된 skill
        [SerializeField] private int _selectedItem = -1;  public int SelectedItem => _selectedItem;   //선택된 아이템    
        private List<int> _selectedTarget = new List<int>();   // 타겟들
        private bool _isEnemy = true;//타겟이 적인지 아군인지
        private int _targetNum = -1;   //적용할 타겟의 수

        private float _currentHp;
        private float _maxHp;
        public void Start()
        {
            anim = GetComponent<Animator>();
        }
        public void SetUp(PlayerInfo info)
        {
            _info = info;
            _currentHp = info.Hp;
        }

        public void SelectSkill(int index)
        {
            Debug.Log($"[스킬 선택] index:{index}");
            _selectedSkill = index;
            _selectedItem = -1;
            _selectedTarget.Clear();
            _targetNum = _info.Skills[index].TagetNum;

            var skillType = _info.Skills[index].Type;
            _isEnemy = (skillType == SkillType.Atk || skillType == SkillType.Debuff);

            // ── 스킬 정보 로그
            var skill = _info.Skills[index];
            Debug.Log($"[스킬 정보] 이름:{skill.Name} | 타입:{skill.Type} | 타겟수:{skill.TagetNum} | " +
                      $"DamageRate:{skill.DamageRate} | HealRate:{skill.HealRate} | " +
                      $"EffectType:{skill.EffectType} | EffectValue:{skill.EffectValue} | EffectDuration:{skill.EffectDuration}");

            // ── Enforce는 타겟이 자기 자신 → 바로 발동
            if (skillType == SkillType.Enforce)
            {
                Debug.Log("[스킬 발동] Enforce → 자기 자신 대상으로 즉시 발동");
                _selectedTarget.Add(0); // 인덱스 의미 없음, 서버에서 caster 직접 사용
                _controller.CMDSelectionComplete(_selectedSkill, _selectedItem, _isEnemy, _selectedTarget);
                _selectedTarget.Clear();
                _targetNum = 0;
            }
        }

        // ← 추가: 아군 선택
        public void SelectAlly(int index)
        {
            _selectedTarget.Add(index);
            if (_targetNum > 0 && _targetNum == _selectedTarget.Count)
            {
                _controller.CMDSelectionComplete(_selectedSkill, _selectedItem, _isEnemy, _selectedTarget);
                _selectedTarget.Clear();
                _targetNum = 0;
            }
        }
        public void SelectItem(int index)
        {
            Debug.Log("아이템 선택");

            _selectedItem = index;
            _selectedSkill = -1;
            _selectedTarget.Clear();

            _targetNum = _info.Items[index].TagetNum;
        }
        public void SelectEnemy(int index)
        {
            Debug.Log("타겟 선택");

            _selectedTarget.Add(index);
            if (_targetNum > 0 && _targetNum == _selectedTarget.Count)
            {
                _controller.CMDSelectionComplete(_selectedSkill, _selectedItem, _isEnemy, _selectedTarget);
                _selectedTarget.Clear();
                _targetNum = 0;
            }
        }
        public float PlDamaged(float Attack)
        {
            _currentHp -= Attack;
            return _currentHp;
        }

        public float PlHeal(float amount)
        {
            _currentHp = Mathf.Min(_currentHp + amount, _maxHp);
            return _currentHp;
        }

        public void Reset()
        {
            _selectedItem = -1;
            _selectedSkill = -1;
            _targetNum = -1;
        }
    }

}
