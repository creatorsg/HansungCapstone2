using Jun;
using UnityEditor;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [SerializeField] private UnitModel _model;
    //[SerializeField] private EnemyView _view;

    public UnitState State = UnitState.Waiting;
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
