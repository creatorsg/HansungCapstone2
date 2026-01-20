using Jun;
using UnityEngine;
using Mirror;

namespace Jun
{// 멀티서버 만들기전에 만들어둔 플레이어 컨트롤러
    public class PlayerController : NetworkBehaviour
    {
        [SerializeField] private UnitModel _model;
        [SerializeField] private PlayerView _view;

        public UnitState State = UnitState.Waiting;
        public void IsMyTurn(int index)
        {
            bool isMyTurn = _model.Info.Id == index ? true : false;
            if (State == UnitState.Incapacitated) return;
            State = isMyTurn ? UnitState.Acting : UnitState.Waiting;
            ChangeState();
        }

        public void ChangeState()
        {
            switch (State)
            {
                case UnitState.Waiting:
                case UnitState.Incapacitated:
                    // 모든 버튼 비활성화
                    _view.SetButtonsInteractable(false, _view.SkillBtn);
                    _view.SetButtonsInteractable(false, _view.ItemBtn);
                    _view.SetButtonsInteractable(false, _view.EnemyBtn);
                    break;

                case UnitState.Acting:
                    // 내 턴일 때만 버튼 활성화
                    _view.SetButtonsInteractable(true, _view.SkillBtn);
                    _view.SetButtonsInteractable(true, _view.ItemBtn);
                    break;
            }
        }

        // 스킬 버튼을 누르면 선택한 스킬의 정보가 저장이 되고 (만약 전에 아이템을 선택했다면 지우기, 타겟들도 지우기)
        // 선택한 스킬의 타겟 수에 따라 선택 가능한 타겟 수 변경
        // 타겟 버튼 활성화
        // 아이템 버튼도 과정은 동일
        public void OnClickSkillBtn(int index) //스킬버튼
        {
            _model.SelectedSkill(index);
            _view.SetButtonsInteractable(true, _view.EnemyBtn);
        }
        public void OnClickItemBtn(int index) //아이템 버튼
        {
            _model.SelectedItem(index);
            _view.SetButtonsInteractable(true, _view.EnemyBtn);
        }

        public void OnClickEnemyBtn(int index) //적버튼
        {
            _model.SelectedEnemy(index);
        }
    }
}
