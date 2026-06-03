using Jun;
using Mirror;
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.UI.CanvasScaler;

namespace Jun
{
    public class GamePlayerController : NetworkBehaviour
    {
        [SerializeField] private UnitModel _model;
        [SerializeField] private PlayerView _view;
        public PlayerView View => _view;

        /// <summary>CharacterCard.CharacterCode - 프리팹 룩업 기준 식별자</summary>
        [SyncVar(hook = nameof(OnHeroCodeChanged))] public string FinalHeroCode = "";

        /// <summary>CharacterDatabase.index - 레거시 UI 호환용</summary>
        [SyncVar] public int    FinalHeroIndex = -1;

        [SyncVar(hook = nameof(OnPosIndexChanged))] public int FinalHeroPos = -1;
        [SyncVar] public PlayerInfo Info;
        [SyncVar] public int PingIndex;

        // 4단계: 프로토타입 특수 처리용 SyncVar (실제 사용은 7단계 switch case)
        [SyncVar] public bool blockNext = false;   // Glue #2 엄호: 다음 피격 1회 무효
        [SyncVar] public int knifeStacks = 0;       // Choke #4 무기회수: 누적 스택 (공격 데미지 +10%/스택)

        [Header(" ý")]
        public Transform PingLayout; //   

        public bool IsMovePos = false;

        public readonly SyncList<ActiveEffect> Effects = new SyncList<ActiveEffect>();

        public int EffectiveAtk => CombatCalculator.GetEffectiveAtk(Info.Atk, Effects);
        public int EffectiveDef => CombatCalculator.GetEffectiveDef(Info.Def, Effects);
        public int EffectiveAcc => CombatCalculator.GetEffectiveAcc(Info.Acc, Effects);
        public int EffectiveDodge => CombatCalculator.GetEffectiveDodge(Info.Dodge, Effects);


        // ── 애니메이션 래퍼 ────────────────────────────────────────────
        // BattleLogic / EnemyController 등 서버 코드에서 호출합니다.

        /// <summary>피격 애니메이션을 모든 클라이언트에 재생합니다.</summary>
        [ClientRpc]
        public void RpcPlayDamagedAnim()
        {
            _view.PlayDamaged();
        }

        /// <summary>회피 애니메이션을 모든 클라이언트에 재생합니다.</summary>
        [ClientRpc]
        public void RpcPlayDodgeAnim()
        {
            _view.PlayDodge();
        }

        /// <summary>사망 애니메이션을 모든 클라이언트에 재생합니다.</summary>
        [ClientRpc]
        public void RpcPlayDeadAnim()
        {
            _view.PlayDead();
        }

        // 데이터 주입
        private PlayerData _sourcePlayerData; // 원본 PlayerData 참조 보관

        [Server]
        public void InjectData(PlayerData data)
        {
            _sourcePlayerData   = data;        // 원본 유지 → 씬 전환 후에도 살아있음
            this.Info           = data.Info;
            this.PingIndex      = data.PingIndex;
            this.FinalHeroCode  = data.FinalHeroCode;
            this.FinalHeroIndex = data.FinalHeroIndex;
            this.FinalHeroPos   = data.FinalHeroPos;
        }

        /// <summary>
        /// 배틀 종료 시 현재 Info를 원본 PlayerData에 다시 씁니다. 서버 전용.
        /// </summary>
        [Server]
        public void FlushInfoToPlayerData()
        {
            if (_sourcePlayerData == null)
            {
                Debug.LogWarning($"[GamePlayerController] {FinalHeroCode} — _sourcePlayerData가 null입니다.");
                return;
            }
            _sourcePlayerData.Info = this.Info;
            Debug.Log($"[FlushInfo] {FinalHeroCode} → PlayerData.Info 동기화 완료. Items={this.Info?.Items?.Count}");
        }
        // ��ġ�� ��� ������ ���� �Լ��� �и��ؼ� ȣ��
        void OnPosIndexChanged(int oldPos, int newPos)
        {
            if (oldPos == -1)
            {
            //ó 
                transform.position = BattleManager.Instance.SpawnPoints[newPos].position;
            }
            else
            {
            //߿ ڸ ٲ (ε巴 ̵)
                StopAllCoroutines();
                StartCoroutine(MoveRoutine(BattleManager.Instance.SpawnPoints[newPos].position));
            }
        }
 //ε巴 ̰ ִ Լ
        System.Collections.IEnumerator MoveRoutine(Vector3 targetPos)
        {
            while (Vector3.Distance(transform.position, targetPos) > 0.01f)
            {
                transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 5f);
                yield return null;
            }
            transform.position = targetPos;
            _view.UpdateOriginPos();
        }

        /// <summary>
        /// FinalHeroCode SyncVar 동기화 훅 — 클라이언트에서 코드가 설정되는 순간
        /// CharacterRegistry에서 스프라이트를 꺼내 SpriteRenderer에 적용합니다.
        /// </summary>
        private void OnHeroCodeChanged(string oldCode, string newCode)
        {
            ApplyCharacterSprite(newCode);
        }

        /// <summary>
        /// 코드에 해당하는 스프라이트를 SpriteRenderer에 적용합니다.
        /// Start()와 OnHeroCodeChanged() 양쪽에서 호출합니다.
        /// </summary>
        private void ApplyCharacterSprite(string code)
        {
            if (string.IsNullOrEmpty(code)) return;

            if (!CharacterRegistry.TryGet(code, out var entry))
            {
                Debug.LogWarning($"[GamePlayerController] '{code}' 스프라이트 적용 실패: Registry에 없음");
                return;
            }

            if (entry.CharacterSprite == null)
            {
                Debug.LogWarning($"[GamePlayerController] '{code}' CharacterSprite가 null입니다. CharacterCard에 Sprite를 연결하세요.");
                return;
            }

            // GetComponentInChildren으로 루트·자식 오브젝트를 모두 탐색합니다.
            var sr = GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = entry.CharacterSprite;
                Debug.Log($"[GamePlayerController] '{code}' → SpriteRenderer({sr.gameObject.name}) 스프라이트 적용 완료");
            }
            else
            {
                // SpriteRenderer가 없으면 UI Image로 폴백
                var img = GetComponentInChildren<UnityEngine.UI.Image>();
                if (img != null)
                {
                    img.sprite = entry.CharacterSprite;
                    Debug.Log($"[GamePlayerController] '{code}' → Image({img.gameObject.name}) 스프라이트 적용 완료");
                }
                else
                {
                    Debug.LogWarning($"[GamePlayerController] SpriteRenderer/Image를 찾지 못했습니다. ({gameObject.name})");
                }
            }
        }

        /// <summary>
        /// 이 유닛의 캐릭터 스프라이트를 반환합니다.
        /// CharacterRegistry → SpriteRenderer 순으로 조회합니다.
        /// BattleManager 등 외부에서 이 메서드를 사용하면 null 안전하게 스프라이트를 얻을 수 있습니다.
        /// </summary>
        public Sprite GetCharacterSprite()
        {
            if (!string.IsNullOrEmpty(FinalHeroCode) &&
                CharacterRegistry.TryGet(FinalHeroCode, out var entry) &&
                entry.CharacterSprite != null)
            {
                return entry.CharacterSprite;
            }
            // 폴백: SpriteRenderer에서 직접 읽기
            return GetComponentInChildren<SpriteRenderer>()?.sprite;
        }

        public void Start()
        {
            _view.EndMyTurn += EndMyTurn;
            _model.SetUp(Info);

            // 서버에서 이미 FinalHeroCode가 설정된 채로 클라이언트에 스폰될 경우
            // SyncVar 훅이 트리거되지 않으므로 Start()에서도 명시적으로 적용합니다.
            ApplyCharacterSprite(FinalHeroCode);
        }
        public void MyTurn(bool IsMyTurn)
        {
            _view.SetSel(IsMyTurn);
        }
        // ��ų ��ư�� ������ ������ ��ų�� ������ ������ �ǰ� (���� ���� �������� �����ߴٸ� �����, Ÿ�ٵ鵵 �����)
        // ������ ��ų�� Ÿ�� ���� ���� ���� ������ Ÿ�� �� ����
        // Ÿ�� ��ư Ȱ��ȭ
        // ������ ��ư�� ������ ����

        public void OnClickSkillBtn(int index)
        {
            Debug.Log($"[OnClickSkillBtn] index={index}, isOwned={isOwned}, CurrentTurnUnit={BattleManager.Instance.CurrentTurnUnit?.name ?? "NULL"}, this={name}, 같은유닛={BattleManager.Instance.CurrentTurnUnit == this}");

            // 내 유닛이고 현재 내 턴일 때만 동작
            if (isOwned && BattleManager.Instance.CurrentTurnUnit == this)
            {
                _model.SelectSkill(index);
                var skillType = Info.Skills[index].Type;
                if (skillType == SkillType.Atk || skillType == SkillType.Debuff)
                {
                    // 적 대상 스킬 선택 시 적 버튼 활성화
                    _view.SetButtonsInteractable(true, _view.EnemyBtn);
                }
            }
        }

        public void OnClickItemBtn(int index)
        {
            // 내 유닛이고 현재 내 턴일 때만 동작
            if (isOwned && BattleManager.Instance.CurrentTurnUnit == this)
            {
                _model.SelectItem(index);
                var item = Info.Expendables[index];

                switch (item.Target)
                {
                    case TargetType.SingleEnemy:
                        _view.SetButtonsInteractable(true, _view.EnemyBtn);
                        break;
                }
            }
        }

        public void OnClickEnemyBtn(int index) //ư
        {
            if (_model.SelectedItem == -1 && _model.SelectedSkill == -1)
            {
                Debug.Log($"[GamePlayerController] 적 UI 갱신 요청 - index:{index}");
                BattleManager.Instance.UpdateEnemyUI(index);
            }
            // 적 선택 핑 전송
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
            Debug.Log("ڸ̵" + IsMovePos);
        }
            //ٲ (ٸ Ʊ ) Ŭ 
        public void OnClickedUnit()
        {
            Debug.Log($"[OnClickedUnit] 클릭됨: name={name}, code={FinalHeroCode}, Skills={Info.Skills.Count  -1}");

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
                else if (model.SelectedItem != -1)
                {
                    var itemTarget = currentUnit.Info.Expendables[model.SelectedItem].Target;
                    if (itemTarget == TargetType.SingleAlly)
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

                foreach (var unit in BattleManager.Instance._players)
                {
                    if (unit.isOwned) 
                    {
                        unit.CmdSendPing(unit.PingIndex,this.gameObject); 
                        break;
                    }
                }
            }
        }

        [Command]
        public void CmdSendPing(int ping, GameObject target)
        {
            Debug.Log("Ping1");
            BattleManager.Instance.RpcShowPing(PingIndex, target);
        }

            //ڸ ü û
        [Command]
        public void CmdRequestChangePos(GameObject targetUnitObj)
        {
            GamePlayerController targetUnit = targetUnitObj.GetComponent<GamePlayerController>();
            if (targetUnit != null)
            {
                BattleManager.Instance.ChangeUnitPos(this, targetUnit);
            }
        }

            //
        public void PlDamaged(float Attack)
        {
            _view.PlDamaged(_model.PlDamaged(Attack)/Info.Hp);
            
        }
        // 모든 클라이언트에 피해 표시를 갱신한다.
        [ClientRpc]
        public void RpcShowDamage(float damage)
        {
            if (_view != null && Info != null)
                _view.RPCPlHPChanged(Info.Hp);
        }
        [Server]
        public void ApplyHpChange(float delta)
        {
            var info = Info;
            //info.Hp = Mathf.Clamp(info.Hp + delta, 0f, info.MaxHp);
            info.Hp = info.Hp + delta;
            Info = info; // SyncVar 재할당으로 클라이언트 동기화
            _view.RPCPlHPChanged(info.Hp);
            BattleManager.Instance.RpcRefreshUnitPanel(this);
        }
        [Server]
        public void ApplySanChange(float delta)
        {
            var info = Info;
            info.San = (int)Mathf.Clamp(info.San + delta, 0f, info.MaxSan);
            Info = info;
            _view.PlSanChanged(info.San);
            BattleManager.Instance.RpcRefreshUnitPanel(this);
        }
        [Server]
        public void AddEffect(ActiveEffect effect)
        {
            Effects.Add(effect); // SyncList에 추가해 클라이언트로 동기화
        }
 //ų û
        [Command]
        public void CMDSelectionComplete(int skillIndex, int itemIndex, bool isEnemy, List<int> tagets)
        {
            BattleManager.Instance.VerifyClientRequest(this, skillIndex, itemIndex, isEnemy, tagets);
        }


        //ִϸ̼ 
        [ClientRpc]
        public void RpcPlaySkillAnim(string animName, bool isRevive)
        {
            //ų ص ɵ
            _view.SkillAnim(animName, isRevive);
        }

            //-> ѱ
        [Command]
        public void EndMyTurn()
        {
            _model.Reset();
            BattleManager.Instance.NextTurn();
        }

        // ===== [DEBUG] 1단계 검증용 임시 코드 — 검증 완료 후 이 region 전체 삭제 =====
        // F1: 자신에게 출혈(DoT) 5/턴 × 3턴   → 매 자기 턴 시작 시 HP -5
        // F2: 자신에게 스턴 2턴               → 자기 다음 2턴 스킵
        // F3: 자신에게 회피감소 20 × 3턴      → 로그로 유효 회피 감소 확인
        // F4: 첫 번째 생존 적에게 스턴 2턴    → 적 다음 2턴 스킵
        // F5: 자신에게 크리증가 40 × 3턴      → 로그로 유효 크리 증가 확인 (4단계)
        void Update()
        {
            if (!isOwned) return;
            if (Input.GetKeyDown(KeyCode.F1)) CmdDebugApplyToSelf((int)EffectType.Bleeding, 5f, 3);
            if (Input.GetKeyDown(KeyCode.F2)) CmdDebugApplyToSelf((int)EffectType.Stunned, 0f, 2);
            if (Input.GetKeyDown(KeyCode.F3)) CmdDebugApplyToSelf((int)EffectType.DodgeDown, 20f, 3);
            if (Input.GetKeyDown(KeyCode.F4)) CmdDebugStunFirstEnemy();
            if (Input.GetKeyDown(KeyCode.F5)) CmdDebugApplyToSelf((int)EffectType.CritUp, 40f, 3);
        }

        [Command]
        void CmdDebugApplyToSelf(int effectType, float value, int duration)
        {
            var type = (EffectType)effectType;
            AddEffect(new ActiveEffect(type, value, duration));
            Debug.Log($"[DEBUG] {Info.Name} ← {type} v={value} d={duration} | 유효회피={CombatCalculator.GetEffectiveDodge(Info.Dodge, Effects)} 유효크리={CombatCalculator.GetEffectiveCrit(Info.Crit, Effects)}");
        }

        [Command]
        void CmdDebugStunFirstEnemy()
        {
            var enemies = BattleManager.Instance.GetAliveEnemies();
            if (enemies.Count == 0) { Debug.Log("[DEBUG] 살아있는 적이 없습니다."); return; }
            enemies[0].AddEffect(new ActiveEffect(EffectType.Stunned, 0f, 2));
            Debug.Log($"[DEBUG] 적 '{enemies[0].Info.Name}' ← Stunned 2턴");
        }
        // ===== [DEBUG] 끝 =====
    }
}

