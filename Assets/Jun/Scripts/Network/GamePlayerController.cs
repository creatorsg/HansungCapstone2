using Jun;
using Mirror;
using System;
using UnityEngine;

namespace Jun
{
    public class GamePlayerController : NetworkBehaviour
    {
        [SerializeField] private PlayerView _view;

        [SyncVar] public int FinalHeroIndex = -1;
        [SyncVar] public int FinalHeroPos;
        [SyncVar] public PlayerInfo Info;
        public override void OnStartClient()
        {
            base.OnStartClient();
            if (BattleManager.Instance != null)
            {
                BattleManager.Instance.RegisterPlayer(this); // 나도 등록하고 남도 등록함

                if (isLocalPlayer)
                {
                    var buttons = BattleManager.Instance.SkillBTN;
                    for (int i = 0; i < buttons.Count; i++)
                    {
                        int index = i; // 복사본 생성
                        buttons[i].onClick.AddListener(() => OnClickSkillBtn(index));
                    }
                }
            }
        }

        public void Start()
        {
            _view.EndMyTurn += EndMyTurn;
        }

        public void OnClickSkillBtn(int index) //스킬버튼
        {
            // 내 버튼인지 확인
            if (isLocalPlayer)
            {
                CmdCastSkill(index);
            }
        }

        //스킬 사용을 서버에 요청
        [Command]
        void CmdCastSkill(int skillIndex)
        {
            // 여기서 BattleManager를 통해 실제 데미지 계산 등의 로직 실행예정
            // BattleManager.instance.ServerExecuteSkill(this, skillIndex);

            RpcPlaySkillAnim("Attack");
        }

        // 애니메이션 실행
        [ClientRpc]
        void RpcPlaySkillAnim(string animName)
        {
            // 서버를 포함한 모든 클라이언트에서 실행됨
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

