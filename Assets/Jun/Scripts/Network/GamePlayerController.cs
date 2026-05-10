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
        [SyncVar] public int PingIndex;

        public readonly SyncList<ActiveEffect> Effects = new SyncList<ActiveEffect>();

        public int EffectiveAtk => CombatCalculator.GetEffectiveAtk(Info.Atk, Effects);
        public int EffectiveDef => CombatCalculator.GetEffectiveDef(Info.Def, Effects);
        public int EffectiveAcc => CombatCalculator.GetEffectiveAcc(Info.Acc, Effects);
        public int EffectiveDodge => CombatCalculator.GetEffectiveDodge(Info.Dodge, Effects);

       [Header("핑 시스템")]
        public Transform PingLayout; // 핑 나오는 공간

        public bool IsMovePos = false;

        // 정보 저장
        [Server]
        public void InjectData(PlayerData data)
        {
            var info = data.Info;
            info.MaxHp = info.Hp; 
            this.Info = info;
            this.PingIndex = data.PingIndex;
            this.FinalHeroIndex = data.FinalHeroIndex;
            this.FinalHeroPos = data.FinalHeroPos;
            this.PingIndex = data.PingIndex;
        }
        // 위치를 잡는 로직을 별도 함수로 분리해서 호출
        void OnPosIndexChanged(int oldPos, int newPos)
        {
            if (oldPos == -1)
            {
                // 처음 스폰될 때
                transform.position = BattleManager.Instance.SpawnPoints[newPos].position;
            }
            else
            {
                // 턴 도중에 자리가 바뀌었을 때 (부드럽게 이동)
                StopAllCoroutines();
                StartCoroutine(MoveRoutine(BattleManager.Instance.SpawnPoints[newPos].position));
            }
        }
        //부드럽게 움직이게 해주는 함수
        System.Collections.IEnumerator MoveRoutine(Vector3 targetPos)
        {
            while (Vector3.Distance(transform.position, targetPos) > 0.01f)
            {
                transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 5f);
                yield return null;
            }
            transform.position = targetPos;
        }

        public void Start()
        {
            _view.EndMyTurn += EndMyTurn;
            _model.SetUp(Info);
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
                if (!isOwned) return;
                _model.SelectSkill(index);

                var skillType = Info.Skills[index].Type;
                if (skillType == SkillType.Atk || skillType == SkillType.Debuff)
                {
                    // 적 대상 스킬 → 적 버튼 활성화
                    _view.SetButtonsInteractable(true, _view.EnemyBtn);
                }
            }

        }
        public void OnClickItemBtn(int index) //아이템 버튼
        {
            if (isOwned)
            {
                _model.SelectItem(index);
                _view.SetButtonsInteractable(true, _view.EnemyBtn);
            }
        }

        public void OnClickEnemyBtn(int index) //적버튼
        {
            if (_model.SelectedItem == -1 && _model.SelectedSkill == -1)
            {
                Debug.Log("적UI패널 나오기 "+ index);
                BattleManager.Instance.UpdateEnemyUI(index);
            }
            // 적 핑 추가
            GameObject enemyObj = BattleManager.Instance.Enemys[BattleManager.Instance.StageNum - 1].Enemys[index].gameObject;
            foreach (var unit in BattleManager.Instance._players)
            {
                if (unit.isOwned)
                {
                    unit.CmdSendPing(unit.PingIndex, enemyObj);
                    break;
                }
            }

            if (isOwned)
            {
                _model.SelectEnemy(index);
            }
        }
        public void OnClickMoveBtn()
        {
            if (!isOwned) return;
            IsMovePos = true;
            Debug.Log("자리이동" + IsMovePos);
        }
        // 바꿀 대상(다른 아군 유닛)을 클릭했을 때 실행
        public void OnClickedUnit()
        {
            var currentUnit = BattleManager.Instance.CurrentTurnUnit;

            if (currentUnit != null && currentUnit.IsMovePos)
            {
                currentUnit.CmdRequestChangePos(this.gameObject);
                currentUnit.IsMovePos = false;
            }
            else if (currentUnit != null && currentUnit.isOwned)
            {
                var model = currentUnit.GetComponent<UnitModel>();
                if (model.SelectedSkill != -1)
                {
                    var skillType = currentUnit.Info.Skills[model.SelectedSkill].Type;
                    bool isAllyTarget = skillType == SkillType.Heal || skillType == SkillType.Buff;
                    if (isAllyTarget)
                    {
                        model.SelectAlly(BattleManager.Instance._players.IndexOf(this));
                        return;
                    }
                }
                BattleManager.Instance.UpdateUnitUI(this);
            }
            else
            {
                BattleManager.Instance.UpdateUnitUI(this);
            }
        }

        [Command]
        public void CmdSendPing(int ping, GameObject target)
        {
            Debug.Log("Ping1");
            BattleManager.Instance.RpcShowPing(PingIndex, target);
        }

        // 서버로 자리 교체 요청
        [Command]
        public void CmdRequestChangePos(GameObject targetUnitObj)
        {
            GamePlayerController targetUnit = targetUnitObj.GetComponent<GamePlayerController>();
            if (targetUnit != null)
            {
                BattleManager.Instance.ChangeUnitPos(this, targetUnit);
            }
        }

        // 피해 받음
        public void PlDamaged(float Attack)
        {
            _view.PlDamaged(_model.PlDamaged(Attack) / Info.MaxHp);

        }
        [Server]
        public void ApplyHpChange(float delta)
        {
            var info = Info;
            info.Hp = Mathf.Clamp(info.Hp + delta, 0f, info.MaxHp);
            Info = info; // SyncVar 재할당해야 클라이언트에 동기화됨
        }

        [Server]
        public void AddEffect(ActiveEffect effect)
        {
            Effects.Add(effect); // SyncList라 자동으로 클라이언트에 동기화됨
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
            _model.Reset();

            // ActiveEffect도 내 턴 끝날 때만 tick
            var ticked = CombatCalculator.TickEffects(Effects);
            Effects.Clear();
            foreach (var e in ticked) Effects.Add(e);
            Debug.Log($"[EndMyTurn] tick 후 Effects 수: {Effects.Count}");

            BattleManager.Instance.NextTurn();
        }
    }
}

