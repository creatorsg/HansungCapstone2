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
        public PlayerInfo Info => _info; //�б�����

        [SerializeField]private int _selectedSkill = -1; public int SelectedSkill => _selectedSkill;  //���õ� skill
        [SerializeField] private int _selectedItem = -1;  public int SelectedItem => _selectedItem;   //���õ� ������    
        private List<int> _selectedTarget = new List<int>();   // Ÿ�ٵ�
        private bool _isEnemy = true;//Ÿ���� ������ �Ʊ�����
        private int _targetNum = -1;   //������ Ÿ���� ��

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
            _selectedItem = -1;
            _selectedTarget.Clear();

            _targetNum = _info.Skills[index].TagetNum;
            
        }
        public void SelectItem(int index)
        {
            Debug.Log("아이템 선택");

            _selectedItem = index;
            _selectedSkill = -1;
            _selectedTarget.Clear();

            //_targetNum = _info.Items[index].TagetNum;
            _selectedTarget.Add(BattleManager.Instance._players.IndexOf(this.GetComponent<GamePlayerController>()));
            _controller.CMDSelectionComplete(_selectedSkill, _selectedItem, _isEnemy, _selectedTarget);
        }
        public void SelectEnemy(int index)
        {
            Debug.Log("타깃 선택");

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
            _selectedTarget.Clear();
        }
    }

}
