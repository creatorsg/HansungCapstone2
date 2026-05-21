using Jun;
using Mirror;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

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

            // 클릭 이벤트 등록
            var trigger = GetComponent<EventTrigger>();
            if (trigger != null)
            {
                var entry = new EventTrigger.Entry();
                entry.eventID = EventTriggerType.PointerClick;
                entry.callback.AddListener((data) => OnClickedEnemy());
                trigger.triggers.Add(entry);
            }
        }
        public void OnClickedEnemy()
        {
            // 내 index 찾기
            var enemyList = BattleManager.Instance
                            .Enemys[BattleManager.Instance.StageNum - 1].Enemys;
            int index = -1;
            for (int i = 0; i < enemyList.Count; i++)
            {
                if (enemyList[i] == this) { index = i; break; }
            }
            if (index == -1) return;

            // 현재 턴인 GamePlayerController한테 전달
            var currentUnit = BattleManager.Instance.CurrentTurnUnit;
            if (currentUnit == null || !currentUnit.isOwned) return;

            currentUnit.OnClickEnemyBtn(index);
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
        [ClientRpc]
        public void RpcPlaySkillAnim(string animName)
        {
            _view.SkillAnim(animName);
        }
        [Command(requiresAuthority = false)]
        public void CMDDead()
        {
            Debug.Log("[EnemyController] CMDDead 호출");
            RpcPlaySkillAnim("Dead");
            StartCoroutine(DeadDelay());
        }

        private IEnumerator DeadDelay()
        {
            yield return new WaitForSeconds(2.0f);
            BattleManager.Instance.OnEnemyDead(gameObject);
        }
    }
}