using Jun;
using UnityEditor;
using UnityEngine;

// 멀티서버 만들기전에 만든 적 컨트롤러
namespace Jun
{
    public class EnemyController : MonoBehaviour
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
                    break;// 모든 행동 정지
                case UnitState.Incapacitated:

                    //나중에 행동불능 턴수 제어해서 할것
                    break;

                case UnitState.Acting:
                    // 행동 가능

                    break;
            }
        }
    }
}