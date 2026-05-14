using Mirror;
using System.Collections.Generic;
using UnityEngine;

namespace Jun
{
    public class BattleLogic : MonoBehaviour
    {
        [Server]
        public void BattleAction(GamePlayerController caster, int skillIndex, int itemIndex, bool isEnemy, List<int> targets)
        {
            var manager = BattleManager.Instance;
            if (manager == null || caster == null || caster.Info == null) return;
            if (targets == null || targets.Count == 0) return;

            if (itemIndex != -1)
            {
                if (caster.Info.Items == null || itemIndex < 0 || itemIndex >= caster.Info.Items.Count) return;

                ItemInfo item = caster.Info.Items[itemIndex];
                foreach (int targetIdx in targets)
                {
                    if (!TryGetPlayer(manager, targetIdx, out var target)) continue;

                    float healAmount = CombatCalculator.CalcHeal(target.Info.MaxHp, item.HealRate);
                    target.ApplyHpChange(healAmount);
                    Debug.Log($"[ITEM] {item.Name} -> {target.Info.Name} +{healAmount:F0} HP");

                    manager.RpcShowCombatResult(new CombatResult
                    {
                        isHit = true,
                        isCrit = false,
                        value = healAmount,
                        targetIndex = targetIdx,
                        isEnemy = false
                    });
                }

                CloseEnemyPanel(manager);
                return;
            }

            if (skillIndex == -1) return;
            if (caster.Info.Skills == null || skillIndex < 0 || skillIndex >= caster.Info.Skills.Count) return;

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

            if (!string.IsNullOrEmpty(skill.anim))
                caster.RpcPlaySkillAnim(skill.anim);

            caster.MyTurn(false);
            CloseEnemyPanel(manager);
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

        private static void CloseEnemyPanel(BattleManager manager)
        {
            if (manager != null && manager.EnemyPanel != null)
                manager.EnemyPanel.SetActive(false);
        }

        // ── 적 행동 처리 (EnemyAI 가 PickSkill/PickTargets 후 호출) ─────────
        [Server]
        public void EnemyAction(EnemyController caster, SkillInfo skill, List<int> targets)
        {
            var manager = BattleManager.Instance;
            if (manager == null || caster == null || caster.Info == null || skill == null || targets == null) return;

            foreach (int idx in targets)
            {
                if (idx < 0 || idx >= manager._players.Count) continue;
                var target = manager._players[idx];
                if (target == null || target.Info == null || target.Info.Hp <= 0f) continue;

                switch (skill.Type)
                {
                    case SkillType.Atk:
                    {
                        bool isHit = CombatCalculator.RollHit(caster.Info.Acc, target.EffectiveDodge);
                        if (!isHit)
                        {
                            Debug.Log($"[Enemy MISS] {caster.Info.Name} -> {target.Info.Name}");
                            manager.RpcShowCombatResult(new CombatResult
                            { isHit = false, isCrit = false, value = 0, targetIndex = idx, isEnemy = false });
                            continue;
                        }

                        bool isCrit = CombatCalculator.RollCrit(caster.Info.Crit);
                        float rate = skill.DamageMultiplier > 0f ? skill.DamageMultiplier : 1f;
                        float damage = CombatCalculator.CalcDamage(
                            caster.Info.Atk, target.EffectiveDef, rate, isCrit, caster.Info.Ctm);

                        Debug.Log($"[Enemy ATK] {caster.Info.Name} -> {target.Info.Name} | {damage:F0} dmg | crit={isCrit}");
                        target.ApplyHpChange(-damage);
                        target.RpcShowDamage(damage);

                        manager.RpcShowCombatResult(new CombatResult
                        { isHit = true, isCrit = isCrit, value = damage, targetIndex = idx, isEnemy = false });

                        // 신 상태이상 부여 (StatusEffects)
                        if (skill.StatusEffects != null && skill.StatusEffects.Count > 0)
                        {
                            var info = target.Info;
                            StatusProcessor.Apply(info, skill.StatusEffects);
                            target.Info = info; // SyncVar 재할당으로 클라이언트 동기화
                        }
                        break;
                    }
                    case SkillType.Debuff:
                    {
                        if (skill.StatusEffects == null || skill.StatusEffects.Count == 0) break;

                        if (CombatCalculator.RollResist(target.Info.Res))
                        {
                            Debug.Log($"[Enemy DEBUFF RESIST] {target.Info.Name} 저항 성공");
                            break;
                        }

                        var info = target.Info;
                        StatusProcessor.Apply(info, skill.StatusEffects);
                        target.Info = info;
                        Debug.Log($"[Enemy DEBUFF] {caster.Info.Name} -> {target.Info.Name}");
                        break;
                    }
                    default:
                        Debug.Log($"[Enemy] SkillType {skill.Type} not yet implemented for enemy action");
                        break;
                }
            }
        }
    }
}
