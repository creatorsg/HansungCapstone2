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
        public PlayerInfo Info => _info;

        [SerializeField] private int _selectedSkill = -1; public int SelectedSkill => _selectedSkill;
        [SerializeField] private int _selectedItem  = -1; public int SelectedItem  => _selectedItem;
        private List<int> _selectedTarget = new List<int>();
        private bool _isEnemy = true;
        private int _targetNum = -1;
        private TargetType _selectedTargetType = TargetType.SingleEnemy;

        private float _currentHp;
        private float _maxHp;

        private int _currentSan;
        private int _maxSan;
        public void Start()
        {
            anim = GetComponent<Animator>();
        }

        public void SetUp(PlayerInfo info)
        {
            _info = info;
            if (_info != null)
            {
                if (_info.MaxHp <= 0f) _info.MaxHp = info.Hp;
                if (_info.MaxSan <= 0f) _info.MaxSan = info.San;
                if (_info.Statuses == null) _info.Statuses = new List<ActiveStatus>();
            }
            _currentHp = info.Hp;
            _maxHp = info.MaxHp > 0f ? info.MaxHp : info.Hp;
            _currentSan = info.San;
            _maxSan = info.MaxSan > 0f ? info.MaxSan : info.San;
        }

        public void SelectSkill(int index)
        {
            Debug.Log($"[스킬 선택] index:{index}");
            _selectedSkill = index;
            _selectedItem  = -1;
            _selectedTarget.Clear();

            var skill = _info.Skills[index];
            _targetNum          = skill.TagetNum;
            _selectedTargetType = skill.Target;
            _isEnemy            = (skill.Type == SkillType.Atk || skill.Type == SkillType.Debuff);

            // 자동 타깃(Self / AllEnemies / AllAllies)은 클릭 없이 즉시 발동
            if (IsAutoTarget(_selectedTargetType))
            {
                FireSelection();
                return;
            }

            // 레거시 호환: Enforce 는 자기 자신 대상으로 즉시 발동
            if (skill.Type == SkillType.Enforce)
            {
                Debug.Log("[스킬 발동] Enforce → 자기 자신 즉시 발동");
                _selectedTarget.Add(0); // 인덱스 의미 없음, 서버에서 caster 자체 사용
                FireSelection();
            }
        }

        // 아군 클릭 (Heal/Buff 스킬용)
        public void SelectAlly(int index)
        {
            if (IsAutoTarget(_selectedTargetType)) return;
            _selectedTarget.Add(index);
            if (_targetNum > 0 && _targetNum == _selectedTarget.Count) FireSelection();
        }

        public void SelectItem(int index)
        {
            Debug.Log("아이템 선택");
            _selectedItem  = index;
            _selectedSkill = -1;
            _selectedTarget.Clear();

            var item = _info.Items[index];
            _targetNum          = item.TagetNum;
            _selectedTargetType = item.Target;
            _isEnemy            = false;
            // 아이템은 우선 자기자신한테만 쓸 수 있게끔 구현(아군도 되게끔 가능)
            //if (IsAutoTarget(_selectedTargetType)) FireSelection();
            _selectedTarget.Add(BattleManager.Instance._players.IndexOf(this.GetComponent<GamePlayerController>())); 
            FireSelection();
        }

        public void SelectEnemy(int index)
        {
            if (IsAutoTarget(_selectedTargetType)) return;
            if (_selectedSkill == -1 && _selectedItem == -1) return;

            Debug.Log("타깃 선택");
            _selectedTarget.Add(index);
            if (_targetNum > 0 && _targetNum == _selectedTarget.Count) FireSelection();
        }

        private void FireSelection()
        {
            _controller.CMDSelectionComplete(_selectedSkill, _selectedItem, _isEnemy, new List<int>(_selectedTarget));
            _selectedTarget.Clear();
            _targetNum = 0;
        }

        private static bool IsAutoTarget(TargetType t)
        {
            return t == TargetType.AllEnemies
                || t == TargetType.AllAllies
                || t == TargetType.Self;
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
            _selectedItem   = -1;
            _selectedSkill  = -1;
            _targetNum      = -1;
            _selectedTarget.Clear();
        }
    }
}
