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
            Debug.Log("��ų���� " + index);
            _selectedSkill = index;
            _selectedItem = -1;
            _selectedTarget.Clear();

            _targetNum = _info.Skills[index].TagetNum;
            
        }
        public void SelectItem(int index)
        {
            Debug.Log("������ ����");

            _selectedItem = index;
            _selectedSkill = -1;
            _selectedTarget.Clear();

            _targetNum = _info.Items[index].TagetNum;
        }
        public void SelectEnemy(int index)
        {
            Debug.Log("Ÿ�� ����");

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
        public void Reset()
        {
            _selectedItem = -1;
            _selectedSkill = -1;
            _targetNum = -1;
        }
    }

}
