using Jun;
using Mirror;
using UnityEngine;

// ��Ƽ���� ��������� ���� �� ��Ʈ�ѷ�
namespace Jun
{
    public class EnemyController : NetworkBehaviour
    {
        [SerializeField] private EnemyModel _model;
        [SerializeField] private EnemyView _view;
        public PlayerInfo Info;
        //[SerializeField] private EnemyView _view;

        public UnitState State = UnitState.Waiting;

        private void Start()
        {
            _model.SetUp(Info);
            _model.IsDamaged += _view.Damaged;
        }
        public void IsMyTurn(int index)
        {
            bool isMyTurn = _model.Info.Id == index ? true : false;
            if (State == UnitState.Incapacitated && isMyTurn)
            {
                ChangeState();
                return;
            }
            State = isMyTurn ? UnitState.Acting : UnitState.Waiting;
            ChangeState();
        }

        public void ChangeState()
        {
            switch (State)
            {
                case UnitState.Waiting:
                    break;// ��� �ൿ ����
                case UnitState.Incapacitated:

                    //���߿� �ൿ�Ҵ� �ϼ� �����ؼ� �Ұ�
                    break;

                case UnitState.Acting:
                    // �ൿ ����

                    break;
            }
        }
        [Command(requiresAuthority = false)]
        public void CMDDead()
        {
            Debug.Log("E");
            BattleManager.Instance.RcpEnemyDead(gameObject);
        }
    }
}