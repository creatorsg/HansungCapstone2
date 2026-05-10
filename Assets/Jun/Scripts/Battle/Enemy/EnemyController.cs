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

        [Header("�� �ý���")]
        public Transform PingLayout; // �� ������ ����

        public UnitState State = UnitState.Waiting;

        public readonly SyncList<ActiveEffect> Effects = new SyncList<ActiveEffect>();

        public int EffectiveAtk => CombatCalculator.GetEffectiveAtk(Info.Atk, Effects);
        public int EffectiveDef => CombatCalculator.GetEffectiveDef(Info.Def, Effects);
        public int EffectiveAcc => CombatCalculator.GetEffectiveAcc(Info.Acc, Effects);
        public int EffectiveDodge => CombatCalculator.GetEffectiveDodge(Info.Dodge, Effects);

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
        [Server]
        public void AddEffect(ActiveEffect effect)
        {
            Effects.Add(effect);
        }

        [Command(requiresAuthority = false)]
        public void CMDDead()
        {
            Debug.Log("[EnemyController] CMDDead 호출");
            BattleManager.Instance.OnEnemyDead(gameObject);
        }
    }
}