using Jun;
using Mirror;
using UnityEngine;

// О©╫О©╫ф╪О©╫О©╫О©╫О©╫ О©╫О©╫О©╫О©╫О©╫О©╫О©╫О©╫О©╫ О©╫О©╫О©╫О©╫ О©╫О©╫ О©╫О©╫ф╝О©╫я╥О©╫
namespace Jun
{
    public class EnemyController : NetworkBehaviour
    {
        [SerializeField] private EnemyModel _model;
        [SerializeField] private EnemyView _view;
        public PlayerInfo Info;
        //[SerializeField] private EnemyView _view;

        [Header("гн ╫ц╫╨еш")]
        public Transform PingLayout; // гн Ё╙©ю╢б ╟Ь╟ё

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
                    break;// О©╫О©╫О©╫ О©╫Ю╣© О©╫О©╫О©╫О©╫
                case UnitState.Incapacitated:

                    //О©╫О©╫О©╫ъ©О©╫ О©╫Ю╣©О©╫р╢О©╫ О©╫о╪О©╫ О©╫О©╫О©╫О©╫О©╫ь╪О©╫ О©╫р╟О©╫
                    break;

                case UnitState.Acting:
                    // О©╫Ю╣© О©╫О©╫О©╫О©╫

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