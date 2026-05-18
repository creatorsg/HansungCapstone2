using Jun;
using Mirror;
using UnityEngine;

namespace Jun
{
    public class EnemyController : NetworkBehaviour
    {
        [SerializeField] private EnemyModel _model;
        [SerializeField] private EnemyView _view;
        public PlayerInfo Info;

        [Header("핑 시스템")]
        public Transform PingLayout;

        public UnitState State = UnitState.Waiting;

        public readonly SyncList<ActiveEffect> Effects = new SyncList<ActiveEffect>();

        public int EffectiveAtk => CombatCalculator.GetEffectiveAtk(Info.Atk, Effects);
        public int EffectiveDef => CombatCalculator.GetEffectiveDef(Info.Def, Effects);
        public int EffectiveAcc => CombatCalculator.GetEffectiveAcc(Info.Acc, Effects);
        public int EffectiveDodge => CombatCalculator.GetEffectiveDodge(Info.Dodge, Effects);

        private void Start()
        {
            if (Info != null)
            {
                if (Info.MaxHp <= 0f) Info.MaxHp = Info.Hp;
                if (Info.Statuses == null) Info.Statuses = new System.Collections.Generic.List<ActiveStatus>();
            }

            _model.SetUp(Info);
            _model.IsDamaged += _view.Damaged;
        }

        public void IsMyTurn(int index)
        {
            bool isMyTurn = _model.Info.Id == index;
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
                    break;
                case UnitState.Incapacitated:
                    break;
                case UnitState.Acting:
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
