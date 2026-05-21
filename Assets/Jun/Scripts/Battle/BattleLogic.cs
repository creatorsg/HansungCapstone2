using Jun;
using Mirror;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;


namespace Jun
{
    public class BattleLogic : MonoBehaviour
    {
        // ���߿��� ���� �����ְԲ� ���� ����
        [Server]
        public void BattleAction(GamePlayerController caster, int skillIndex, int itemIndex, bool isEnemy, List<int> targets)
        {
            var manager = BattleManager.Instance;

            if (skillIndex != -1)
            {
                // 스킬 범위 검사
                if (caster.Info.Skills == null || skillIndex >= caster.Info.Skills.Count)
                {
                    Debug.LogError($"[BattleLogic] skillIndex={skillIndex} 범위 초과. Skills.Count={caster.Info.Skills?.Count}");
                    return;
                }

                SkillInfo skill = caster.Info.Skills[skillIndex];

                switch (skill.Type)
                {
                    case SkillType.Atk:
                        foreach (int targetIdx in targets)
                        {
                            if (!TryGetEnemy(manager, targetIdx, out var enemyModel, out var enemyController)) continue;

                            int effAcc = CombatCalculator.GetEffectiveAcc(caster.Info.Acc, caster.Effects);
                            int effDodge = CombatCalculator.GetEffectiveDodge(enemyController.Info.Dodge, enemyController.Effects);
                            bool isHit = CombatCalculator.RollHit(effAcc, effDodge);

                            if (!isHit)
                            {
                                Debug.Log($"[MISS] {caster.Info.Name} -> Enemy {targetIdx}");
                                enemyController.RpcPlaySkillAnim("Dodge");
                                manager.RpcShowCombatResult(new CombatResult
                                {
                                    isHit = false,
                                    isCrit = false,
                                    value = 0,
                                    targetIndex = targetIdx,
                                    isEnemy = true
                                });
                                continue;
                            }

                            bool isCrit = CombatCalculator.RollCrit(caster.Info.Crit);
                            int effAtk = CombatCalculator.GetEffectiveAtk(caster.Info.Atk, caster.Effects);
                            int effDef = CombatCalculator.GetEffectiveDef(enemyController.Info.Def, enemyController.Effects);
                            float damage = CombatCalculator.CalcDamage(effAtk, effDef, skill.DamageRate, isCrit, caster.Info.Ctm);

                            Debug.Log($"[ATK] {caster.Info.Name} -> Enemy {targetIdx} | {damage:F0} dmg | crit:{isCrit}");

                            enemyModel.Damaged(damage);
                            enemyController.RpcPlaySkillAnim("Damaged");

                            manager.RpcShowCombatResult(new CombatResult
                            {
                                isHit = true,
                                isCrit = isCrit,
                                value = damage,
                                targetIndex = targetIdx,
                                isEnemy = true
                            });
                        }
                        break;

                    case SkillType.Buff:
                        foreach (int targetIdx in targets)
                        {
                            if (!TryGetPlayer(manager, targetIdx, out var target)) continue;

                            var effect = new ActiveEffect(skill.EffectType, skill.EffectValue, skill.EffectDuration);
                            target.AddEffect(effect);
                            Debug.Log($"[BUFF] {caster.Info.Name} -> {target.Info.Name} | {skill.EffectType} +{skill.EffectValue}");
                        }
                        break;

                    case SkillType.Heal:
                        foreach (int targetIdx in targets)
                        {
                            if (!TryGetPlayer(manager, targetIdx, out var target)) continue;

                            float healAmount = CombatCalculator.CalcHeal(target.Info.MaxHp, skill.HealRate);
                            target.ApplyHpChange(healAmount);
                            Debug.Log($"[HEAL] {caster.Info.Name} -> {target.Info.Name} +{healAmount:F0} HP");

                            manager.RpcShowCombatResult(new CombatResult
                            {
                                isHit = true,
                                isCrit = false,
                                value = healAmount,
                                targetIndex = targetIdx,
                                isEnemy = false
                            });
                        }
                        break;

                    case SkillType.Debuff:
                        foreach (int targetIdx in targets)
                        {
                            if (!TryGetEnemy(manager, targetIdx, out _, out var enemyController)) continue;

                            if (CombatCalculator.RollResist(enemyController.Info.Res))
                            {
                                Debug.Log($"[RESIST] Enemy {targetIdx}");
                                continue;
                            }

                            var effect = new ActiveEffect(skill.EffectType, skill.EffectValue, skill.EffectDuration);
                            enemyController.AddEffect(effect);
                            Debug.Log($"[DEBUFF] {caster.Info.Name} -> Enemy {targetIdx} | {skill.EffectType}");
                        }
                        break;

                    case SkillType.Enforce:
                        var selfEffect = new ActiveEffect(skill.EffectType, skill.EffectValue, skill.EffectDuration);
                        caster.AddEffect(selfEffect);
                        Debug.Log($"[ENFORCE] {caster.Info.Name} | {skill.EffectType} +{skill.EffectValue}");
                        break;
                }

                manager.EnemyPanel.SetActive(false);

                if (!string.IsNullOrEmpty(skill.anim) && caster.GetComponentInChildren<Animator>() != null)
                {
                    caster.RpcPlaySkillAnim(skill.anim); // anim 있을 때만 호출
                                                         // 턴 종료는 EndAnim Animation Event가 처리
                }
                else
                {
                    Debug.Log("유효하지 않은 애니메이션 이름입니다");
                    caster.MyTurn(false);
                    BattleManager.Instance.NextTurn(); // anim 없으면 즉시 턴 종료
                }
            }
            else if (itemIndex != -1)
            {
                // 아이템 범위 검사
                if (caster.Info.Items == null || itemIndex >= caster.Info.Items.Count)
                {
                    Debug.LogError($"[BattleLogic] itemIndex={itemIndex} 범위 초과. Items.Count={caster.Info.Items?.Count}");
                    return;
                }

                ItemInfo item = caster.Info.Items[itemIndex];
                Debug.Log($"[BattleLogic] 아이템 사용: {item.Name}");

                foreach (int targetIdx in targets)
                {
                    if (!TryGetPlayer(manager, targetIdx, out var target)) continue;
                    float healAmount = 0f;

                    var info = target.Info;
                    switch (item.Type)
                    {
                        case ItemType.HPHeal:
                            healAmount = CombatCalculator.CalcHeal(target.Info.MaxHp, item.HealRate);
                            target.ApplyHpChange(healAmount);
                            Debug.Log($"[ITEM] {item.Name} -> {target.Info.Name} +{healAmount:F0} HP");
                            break;
                        case ItemType.SanHeal:
                            healAmount = CombatCalculator.CalcHeal(target.Info.MaxSan, item.HealRate);
                            target.ApplySanChange(healAmount);
                            Debug.Log($"[ITEM] {item.Name} -> {target.Info.Name} +{healAmount:F0} San");
                            break;
                        // 상태이상 회복 하는 거 해당하는 상태이상 PlayerInfo에서 지우기
                        case ItemType.BleedHeal:
                            if (info.Statuses == null || info.Statuses.Count == 0) break;
                            for (int i = info.Statuses.Count - 1; i >= 0; i--)
                                if (info.Statuses[i].Type == StatusType.Bleed)
                                    info.Statuses.RemoveAt(i);
                            target.Info = info;
                            Debug.Log($"[ITEM] {item.Name} -> {target.Info.Name}");
                            break;

                        case ItemType.PoisonHeal:
                            if (info.Statuses == null || info.Statuses.Count == 0) break;
                            for (int i = info.Statuses.Count - 1; i >= 0; i--)
                                if (info.Statuses[i].Type == StatusType.Poison)
                                    info.Statuses.RemoveAt(i);
                            target.Info = info;
                            Debug.Log($"[ITEM] {item.Name} -> {target.Info.Name}");
                            break;

                        case ItemType.StunHeal:
                            if (info.Statuses == null || info.Statuses.Count == 0) break;
                            for (int i = info.Statuses.Count - 1; i >= 0; i--)
                                if (info.Statuses[i].Type == StatusType.Stun)
                                    info.Statuses.RemoveAt(i);
                            target.Info = info;
                            Debug.Log($"[ITEM] {item.Name} -> {target.Info.Name}");
                            break;
                    }
                }

                manager.EnemyPanel.SetActive(false);

                if (!string.IsNullOrEmpty(item.anim) && caster.GetComponentInChildren<Animator>() != null)
                {
                    caster.RpcPlaySkillAnim(item.anim); // anim 있을 때만 호출
                                                         // 턴 종료는 EndAnim Animation Event가 처리
                }
                else
                {
                    Debug.Log("유효하지 않은 애니메이션 이름입니다");
                    caster.MyTurn(false);
                    BattleManager.Instance.NextTurn(); // anim 없으면 즉시 턴 종료
                }
            }
            else
            {
                Debug.LogWarning("[BattleLogic] skillIndex도 itemIndex도 -1입니다. 아무 동작도 하지 않음.");
            }

        }
        private static bool TryGetPlayer(BattleManager manager, int index, out GamePlayerController player)
        {
            player = null;
            if (manager == null || manager._players == null) return false;
            if (index < 0 || index >= manager._players.Count) return false;
            player = manager._players[index];
            return player != null && player.Info != null;
        }
        private static bool TryGetEnemy(BattleManager manager, int index, out EnemyModel enemyModel, out EnemyController enemyController)
        {
            enemyModel = null;
            enemyController = null;

            if (manager == null || manager.Enemys == null) return false;
            int stageIndex = manager.StageNum - 1;
            if (stageIndex < 0 || stageIndex >= manager.Enemys.Count) return false;
            if (manager.Enemys[stageIndex].Enemys == null) return false;
            if (index < 0 || index >= manager.Enemys[stageIndex].Enemys.Count) return false;

            var enemyObj = manager.Enemys[stageIndex].Enemys[index];
            if (enemyObj == null) return false;

            enemyModel = enemyObj.GetComponent<EnemyModel>();
            enemyController = enemyObj.GetComponent<EnemyController>();
            return enemyModel != null && enemyController != null && enemyController.Info != null;
        }
    }
}
