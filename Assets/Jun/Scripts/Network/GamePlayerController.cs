using Jun;
using Mirror;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Jun
{
    public class GamePlayerController : NetworkBehaviour
    {
        private const int PrototypeWeaponAtkPerNode = 5;

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

        public Sprite GetCharacterSprite()
        {
            var sr = GetComponent<SpriteRenderer>();
            return sr != null ? sr.sprite : null;
        }

        [Header("핑 시스템")]
        public Transform PingLayout; // 핑 표시 위치

        public bool IsMovePos = false;

        // 전투 데이터 주입
        [Server]
        public void InjectData(PlayerData data)
        {
            if (data == null || data.Info == null) return;

            var info = ClonePlayerInfo(data.Info);
            info.MaxHp = info.Hp;
            info.MaxSan = info.San;
            ApplySkillTreeUpgrades(info, data);
            ApplyWeaponUpgrade(info, data);

            this.Info = info;
            this.PingIndex = data.PingIndex;
            this.FinalHeroIndex = data.FinalHeroIndex;
            this.FinalHeroPos = data.FinalHeroPos;
            this.PingIndex = data.PingIndex;
        }

        private static PlayerInfo ClonePlayerInfo(PlayerInfo source)
        {
            var clone = new PlayerInfo
            {
                Id = source.Id,
                Name = source.Name,
                Type = source.Type,
                Skills = CloneSkills(source.Skills),
                Items = CloneItems(source.Items),
                Lvl = source.Lvl,
                Exp = source.Exp,
                Hp = source.Hp,
                MaxHp = source.MaxHp,
                San = source.San,
                MaxSan = source.MaxSan,
                Atk = source.Atk,
                Def = source.Def,
                Spd = source.Spd,
                Crit = source.Crit,
                Ctm = source.Ctm,
                Dodge = source.Dodge,
                Acc = source.Acc,
                Res = source.Res,
                WpnId = source.WpnId,
                ArmId = source.ArmId,
                Trk1 = source.Trk1,
                Trk2 = source.Trk2,
                Statuses = CloneStatuses(source.Statuses)
            };

            return clone;
        }

        private static List<SkillInfo> CloneSkills(List<SkillInfo> source)
        {
            var clone = new List<SkillInfo>();
            if (source == null) return clone;

            foreach (var skill in source)
            {
                if (skill == null)
                {
                    clone.Add(null);
                    continue;
                }

                clone.Add(new SkillInfo
                {
                    Name = skill.Name,
                    User = skill.User,
                    Type = skill.Type,
                    anim = skill.anim,
                    TagetNum = skill.TagetNum,
                    icon = skill.icon,
                    description = skill.description,
                    DamageRate = skill.DamageRate,
                    HealRate = skill.HealRate,
                    EffectType = skill.EffectType,
                    EffectValue = skill.EffectValue,
                    EffectDuration = skill.EffectDuration,
                    TierData = skill.TierData,
                    Target = skill.Target,
                    DamageMultiplier = skill.DamageMultiplier,
                    HealAmount = skill.HealAmount,
                    StatusEffects = CloneStatusApplies(skill.StatusEffects)
                });
            }

            return clone;
        }

        private static List<ItemInfo> CloneItems(List<ItemInfo> source)
        {
            var clone = new List<ItemInfo>();
            if (source == null) return clone;

            foreach (var item in source)
            {
                if (item == null)
                {
                    clone.Add(null);
                    continue;
                }

                clone.Add(new ItemInfo
                {
                    Name = item.Name,
                    Type = item.Type,
                    TagetNum = item.TagetNum,
                    icon = item.icon,
                    anim = item.anim,
                    description = item.description,
                    HealRate = item.HealRate,
                    Target = item.Target
                });
            }

            return clone;
        }

        private static List<ActiveStatus> CloneStatuses(List<ActiveStatus> source)
        {
            var clone = new List<ActiveStatus>();
            if (source == null) return clone;

            foreach (var status in source)
            {
                if (status == null)
                {
                    clone.Add(null);
                    continue;
                }

                clone.Add(new ActiveStatus
                {
                    Type = status.Type,
                    RemainingTurns = status.RemainingTurns,
                    Value = status.Value
                });
            }

            return clone;
        }

        private static List<StatusApply> CloneStatusApplies(List<StatusApply> source)
        {
            var clone = new List<StatusApply>();
            if (source == null) return clone;

            foreach (var status in source)
            {
                if (status == null)
                {
                    clone.Add(null);
                    continue;
                }

                clone.Add(new StatusApply
                {
                    Type = status.Type,
                    Chance = status.Chance,
                    Duration = status.Duration,
                    Value = status.Value
                });
            }

            return clone;
        }

        private static void ApplySkillTreeUpgrades(PlayerInfo info, PlayerData data)
        {
            if (info == null || info.Skills == null || data == null) return;

            string characterCode = data.FinalHeroCode;
            if (string.IsNullOrEmpty(characterCode))
            {
                Debug.LogWarning("[SkillTree][Battle] characterCode가 비어 있어 스킬트리 보정을 건너뜁니다.");
                return;
            }

            int applied = 0;
            foreach (string nodeId in data.unlockedNodeIds)
            {
                if (string.IsNullOrEmpty(nodeId)) continue;

                if (!SkillTreeRegistry.TryFind(characterCode, nodeId, out SkillTreeNodeSO node))
                {
                    Debug.LogWarning($"[SkillTree][Battle] nodeId를 찾지 못했습니다. character={characterCode}, node={nodeId}");
                    continue;
                }

                if (node.skillIndex < 0 || node.skillIndex >= info.Skills.Count)
                {
                    Debug.LogWarning($"[SkillTree][Battle] skillIndex 범위 오류. character={characterCode}, node={nodeId}, skillIndex={node.skillIndex}");
                    continue;
                }

                SkillInfo skill = info.Skills[node.skillIndex];
                if (skill == null)
                {
                    Debug.LogWarning($"[SkillTree][Battle] 대상 스킬이 null입니다. character={characterCode}, node={nodeId}, skillIndex={node.skillIndex}");
                    continue;
                }

                ApplySkillTreeNode(skill, node);
                applied++;
            }

            if (applied > 0)
                Debug.Log($"[SkillTree][Battle] {info.Name}({characterCode}) 스킬트리 보정 {applied}개 적용");
        }

        private static void ApplySkillTreeNode(SkillInfo skill, SkillTreeNodeSO node)
        {
            NormalizeBattleSkill(skill);

            if (node.damageDelta != 0)
            {
                float rateDelta = node.damageDelta / 100f;

                if (skill.DamageRate > 0f)
                    skill.DamageRate = Mathf.Max(0f, skill.DamageRate + rateDelta);

                if (skill.DamageMultiplier > 0f)
                    skill.DamageMultiplier = Mathf.Max(0f, skill.DamageMultiplier + rateDelta);

                if (skill.DamageRate <= 0f && skill.DamageMultiplier <= 0f)
                    skill.DamageRate = Mathf.Max(0f, 1f + rateDelta);
            }

            if (node.healAmount != 0)
            {
                skill.HealAmount += node.healAmount;

                // 현재 플레이어 힐 로직은 HealRate를 사용하므로, 값이 있는 힐 스킬은 퍼센트 보정도 함께 반영한다.
                if (skill.HealRate > 0f)
                    skill.HealRate = Mathf.Max(0f, skill.HealRate + node.healAmount / 100f);
            }

            if (node.durationDelta != 0)
            {
                skill.EffectDuration = Mathf.Max(0, skill.EffectDuration + node.durationDelta);

                if (skill.StatusEffects != null)
                {
                    foreach (var status in skill.StatusEffects)
                    {
                        if (status == null) continue;
                        status.Duration = Mathf.Max(0, status.Duration + node.durationDelta);
                    }
                }
            }

            int targetDelta = node.maxTargetsDelta + node.addTargetPosition;
            if (targetDelta != 0)
                skill.TagetNum = Mathf.Max(1, skill.TagetNum + targetDelta);

            if (node.dotDamageDelta != 0)
            {
                if (skill.EffectType == EffectType.Bleeding || skill.EffectType == EffectType.Burning)
                    skill.EffectValue += node.dotDamageDelta;

                if (skill.StatusEffects != null)
                {
                    foreach (var status in skill.StatusEffects)
                    {
                        if (status == null) continue;
                        if (status.Type == StatusType.Bleed || status.Type == StatusType.Poison)
                            status.Value += node.dotDamageDelta;
                    }
                }
            }

            if (node.debuffPotency > 0f && skill.StatusEffects != null)
            {
                foreach (var status in skill.StatusEffects)
                {
                    if (status == null) continue;
                    status.Chance = Mathf.Clamp01(status.Chance + node.debuffPotency);
                }
            }

            if (node.grantsExtraAction)
                Debug.LogWarning($"[SkillTree][Battle] '{node.nodeId}'의 grantsExtraAction은 아직 전투 턴 로직에 연결되지 않았습니다.");
        }

        private static void NormalizeBattleSkill(SkillInfo skill)
        {
            if (skill == null) return;

            if (skill.DamageRate <= 0f && skill.DamageMultiplier > 0f)
                skill.DamageRate = skill.DamageMultiplier;

            if (skill.DamageMultiplier <= 0f && skill.DamageRate > 0f)
                skill.DamageMultiplier = skill.DamageRate;

            if (skill.TagetNum <= 0)
                skill.TagetNum = 1;
        }

        private static void ApplyWeaponUpgrade(PlayerInfo info, PlayerData data)
        {
            if (info == null || data == null) return;
            if (data.PurchasedWeaponNodeCount <= 0) return;

            if (string.IsNullOrEmpty(data.SelectedWeaponId))
            {
                Debug.LogWarning($"[WeaponUpgrade][Battle] {data.FinalHeroCode} weaponNodes={data.PurchasedWeaponNodeCount} 이지만 SelectedWeaponId가 비어 있어 공격력 보정을 건너뜁니다.");
                return;
            }

            int atkBonus = data.PurchasedWeaponNodeCount * PrototypeWeaponAtkPerNode;
            info.Atk += atkBonus;

            Debug.Log($"[WeaponUpgrade][Battle] {info.Name}({data.FinalHeroCode}) weapon={data.SelectedWeaponId}, nodes={data.PurchasedWeaponNodeCount}, Atk +{atkBonus} => {info.Atk}");
        }
        // 위치 인덱스가 바뀌면 스폰 위치 또는 이동 코루틴으로 반영한다.
        void OnPosIndexChanged(int oldPos, int newPos)
        {
            if (oldPos == -1)
            {
                // 처음 위치 설정
                transform.position = BattleManager.Instance.SpawnPoints[newPos].position;
            }
            else
            {
                // 이미 배치된 유닛은 이동 애니메이션으로 위치를 바꾼다.
                StopAllCoroutines();
                StartCoroutine(MoveRoutine(BattleManager.Instance.SpawnPoints[newPos].position));
            }
        }

        // 부드럽게 목표 지점으로 이동한다.
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
        // 스킬 버튼 클릭: 공격/디버프 스킬이면 적 선택 버튼을 활성화한다.
        public void OnClickSkillBtn(int index)
        {
            if (isOwned)
            {
                if (!isOwned) return;
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
            if (isOwned)
            {
                _model.SelectItem(index);
                _view.SetButtonsInteractable(true, _view.EnemyBtn);
                Debug.Log("Item selection");
            }
        }

        public void OnClickEnemyBtn(int index)
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
            Debug.Log($"[GamePlayerController] 위치 이동 모드: {IsMovePos}");
        }

        // 유닛 클릭 시 이동 대상, 아군 대상, 또는 유닛 UI 갱신으로 분기한다.
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
            Debug.Log($"[GamePlayerController] 핑 전송 - ping:{PingIndex}");
            BattleManager.Instance.RpcShowPing(PingIndex, target);
        }

        // 서버에 위치 교체 요청
        [Command]
        public void CmdRequestChangePos(GameObject targetUnitObj)
        {
            GamePlayerController targetUnit = targetUnitObj.GetComponent<GamePlayerController>();
            if (targetUnit != null)
            {
                BattleManager.Instance.ChangeUnitPos(this, targetUnit);
            }
        }

        // 피해 반영
        public void PlDamaged(float Attack)
        {
            _view.PlHPChanged(_model.PlDamaged(Attack) / Info.MaxHp);

        }

        // 모든 클라이언트에 피해 표시를 갱신한다.
        [ClientRpc]
        public void RpcShowDamage(float damage)
        {
            if (_view != null && Info != null && Info.MaxHp > 0f)
                _view.PlHPChanged(Info.Hp / Info.MaxHp);
        }
        [Server]
        public void ApplyHpChange(float delta)
        {
            var info = Info;
            info.Hp = Mathf.Clamp(info.Hp + delta, 0f, info.MaxHp);
            Info = info; // SyncVar 재할당으로 클라이언트 동기화
            _view.PlHPChanged(info.Hp);
        }
        [Server]
        public void ApplySanChange(float delta)
        {
            var info = Info;
            info.San = (int)Mathf.Clamp(info.San + delta, 0f, info.MaxSan);
            Info = info;
            _view.PlSanChanged(info.Hp);
        }
        [Server]
        public void AddEffect(ActiveEffect effect)
        {
            Effects.Add(effect); // SyncList에 추가해 클라이언트로 동기화
        }

        // 스킬/아이템 선택 완료 요청
        [Command]
        public void CMDSelectionComplete(int skillIndex, int itemIndex, bool isEnemy, List<int> tagets)
        {
            BattleManager.Instance.VerifyClientRequest(this, skillIndex, itemIndex, isEnemy, tagets);
        }


        // 애니메이션 재생
        [ClientRpc]
        public void RpcPlaySkillAnim(string animName)
        {
            _view.SkillAnim(animName);
        }

        // 턴 종료 요청
        [Command]
        public void EndMyTurn()
        {
            _model.Reset();

            // ActiveEffect를 한 턴씩 tick 처리
            var ticked = CombatCalculator.TickEffects(Effects);
            Effects.Clear();
            foreach (var e in ticked) Effects.Add(e);
            Debug.Log($"[EndMyTurn] 효과 tick 후 남은 Effects: {Effects.Count}");

            BattleManager.Instance.QueueNextTurn();
        }
    }
}

