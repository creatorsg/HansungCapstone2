using Jun;
using Mirror;
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace Jun
{
    public class GamePlayerController : NetworkBehaviour
    {
        [SerializeField] private UnitModel _model;
        [SerializeField] private PlayerView _view;

        [SyncVar] public int FinalHeroIndex = -1;
        [SyncVar(hook = nameof(OnPosIndexChanged))] public int FinalHeroPos = -1;
        [SyncVar] public PlayerInfo Info;

        public override void OnStartServer()
        {
            base.OnStartServer();
            _model.SetUp(Info);
            BattleManager.Instance.RegisterPlayer(this);
        }
        // 위치를 잡는 로직을 별도 함수로 분리해서 호출
        void OnPosIndexChanged(int oldPos, int newPos)
        {
            Transform targetPoint = BattleManager.Instance.SpawnPoints[newPos];
            if (targetPoint != null)
            {
                transform.position = targetPoint.position;
                Debug.Log($"{gameObject.name}가 {newPos}번 위치로 배치되었습니다.");
            }
        }

        public void Start()
        {
            _view.EndMyTurn += EndMyTurn;
        }

        public void MyTurn(bool IsMyTurn)
        {
            _view.SetSel(IsMyTurn);
        }
        // 스킬 버튼을 누르면 선택한 스킬의 정보가 저장이 되고 (만약 전에 아이템을 선택했다면 지우기, 타겟들도 지우기)
        // 선택한 스킬의 타겟 수에 따라 선택 가능한 타겟 수 변경
        // 타겟 버튼 활성화
        // 아이템 버튼도 과정은 동일

        public void OnClickSkillBtn(int index) //스킬버튼
        {
            if (isOwned)
            {
                _model.SelectedSkill(index);
                _view.SetButtonsInteractable(true, _view.EnemyBtn);
            }

        }
        public void OnClickItemBtn(int index) //아이템 버튼
        {
            if (isOwned)
            {
                _model.SelectedItem(index);
                _view.SetButtonsInteractable(true, _view.EnemyBtn);
            }
        }

        public void OnClickEnemyBtn(int index) //적버튼
        {
            BattleManager.Instance.UpdateEnemyUI(index);
            if (isOwned)
            {
                _model.SelectedEnemy(index);
            }
        }

        void OnMouseDown()
        {
            Debug.Log("OnClick");
            BattleManager.Instance.UpdateUnitUI(this);
        }

        // 피해 받음
        public void PlDamaged(float Attack)
        {
            _view.PlDamaged(_model.PlDamaged(Attack)/Info.Hp);
            
        }

        //스킬 사용을 서버에 요청
        //배틀 매니저에게 무결성 검사 요청
        [Command]
        public void CMDSelectionComplete(int skillIndex, int itemIndex, bool isEnemy, List<int> tagets)
        {
            BattleManager.Instance.VerifyClientRequest(this, skillIndex, itemIndex, isEnemy, tagets);
        }


        // 애니메이션 실행
        [ClientRpc]
        public void RpcPlaySkillAnim(string animName)
        {
            //굳이 스킬로 한정 안해도 될듯
            _view.SkillAnim(animName);
        }

        // 나의 턴종료 -> 턴 넘기기
        [Command]
        public void EndMyTurn()
        {
            BattleManager.Instance.NextTurn();
        }
    }
}

