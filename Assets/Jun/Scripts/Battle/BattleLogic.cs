using Jun;
using Mirror;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;


namespace Jun
{
    public class BattleLogic : MonoBehaviour
    {
        // 나중에 여러 기능 추가할 수 있게끔 여러 함수
        [Server]
        public void BattleAction(GamePlayerController caster, int skillIndex, int itemIndex, bool isEnemy, List<int> targets)
        {
            var manager = BattleManager.Instance;

            //아이템 사용 
            if (itemIndex != -1)
            {
                ItemInfo item = caster.Info.Items[itemIndex];
                foreach (int targetIdx in targets)
                {
                    var target = manager._players[targetIdx];
                    float healAmount = CombatCalculator.CalcHeal(target.Info.MaxHp, item.HealRate);
                    target.ApplyHpChange(healAmount);
                    Debug.Log($"[아이템] {item.Name} → {target.Info.Name} +{healAmount:F0} HP");

                    manager.RpcShowCombatResult(new CombatResult
                    { isHit = true, isCrit = false, value = healAmount, targetIndex = targetIdx, isEnemy = false });
                }
                BattleManager.Instance.EnemyPanel.SetActive(false);
                return;
            }

            // 배틀 로직 분기
            if (skillIndex != -1)
            {
                SkillInfo skill = caster.Info.Skills[skillIndex];

                switch (skill.Type)
                {
                    case SkillType.Atk:
                        foreach (int targetIdx in targets)
                        {
                            var enemyObj = manager.Enemys[manager.StageNum - 1].Enemys[targetIdx];
                            var target = enemyObj.GetComponent<EnemyModel>();
                            var enemy = enemyObj.GetComponent<EnemyController>();

                            // 적중 판정
                            int effAcc = CombatCalculator.GetEffectiveAcc(caster.Info.Acc, caster.Effects);
                            int effDodge = CombatCalculator.GetEffectiveDodge(enemy.Info.Dodge, enemy.Effects);
                            bool isHit = CombatCalculator.RollHit(effAcc, effDodge);

                            if (!isHit)
                            {
                                Debug.Log($"[MISS] {caster.Info.Name} → 적 {targetIdx}");
                                manager.RpcShowCombatResult(new CombatResult
                                { isHit = false, isCrit = false, value = 0, targetIndex = targetIdx, isEnemy = true });
                                continue;
                            }

                            // 치명타 판정
                            bool isCrit = CombatCalculator.RollCrit(caster.Info.Crit);

                            // 피해 계산 (실효 공격력 / 방어력 반영)
                            int effAtk = CombatCalculator.GetEffectiveAtk(caster.Info.Atk, caster.Effects);
                            int effDef = CombatCalculator.GetEffectiveDef(enemy.Info.Def, enemy.Effects);
                            float damage = CombatCalculator.CalcDamage(effAtk, effDef, skill.DamageRate, isCrit, caster.Info.Ctm);

                            Debug.Log($"[ATK] {caster.Info.Name} → 적 {targetIdx} | {damage:F0} dmg | crit:{isCrit}");
                            target.Damaged(damage);

                            manager.RpcShowCombatResult(new CombatResult
                            { isHit = true, isCrit = isCrit, value = damage, targetIndex = targetIdx, isEnemy = true });
                        }
                        break;

                    case SkillType.Buff:
                        foreach (int targetIdx in targets)
                        {
                            var target = manager._players[targetIdx];
                            var effect = new ActiveEffect(skill.EffectType, skill.EffectValue, skill.EffectDuration);
                            target.AddEffect(effect);
                            Debug.Log($"[BUFF] {caster.Info.Name} → {target.Info.Name} | {skill.EffectType} +{skill.EffectValue} ({skill.EffectDuration}턴)");
                        }
                        break;

                    case SkillType.Heal:
                        foreach (int targetIdx in targets)
                        {
                            var target = manager._players[targetIdx];
                            float healAmount = CombatCalculator.CalcHeal(target.Info.MaxHp, skill.HealRate);
                            target.ApplyHpChange(healAmount);
                            Debug.Log($"[HEAL] {caster.Info.Name} → {target.Info.Name} +{healAmount:F0} HP");

                            manager.RpcShowCombatResult(new CombatResult
                            { isHit = true, isCrit = false, value = healAmount, targetIndex = targetIdx, isEnemy = false });
                        }
                        break;

                    case SkillType.Debuff:
                        foreach (int targetIdx in targets)
                        {
                            var enemyObj = manager.Enemys[manager.StageNum - 1].Enemys[targetIdx];
                            var enemy = enemyObj.GetComponent<EnemyController>();

                            // 저항 판정
                            if (CombatCalculator.RollResist(enemy.Info.Res))
                            {
                                Debug.Log($"[RESIST] 적 {targetIdx} 저항 성공");
                                continue;
                            }

                            var effect = new ActiveEffect(skill.EffectType, skill.EffectValue, skill.EffectDuration);
                            enemy.AddEffect(effect);
                            Debug.Log($"[DEBUFF] {caster.Info.Name} → 적 {targetIdx} | {skill.EffectType} ({skill.EffectDuration}턴)");
                        }
                        break;

                    case SkillType.Enforce:
                        // 자기 강화: 시전자 본인에게 버프
                        var selfEffect = new ActiveEffect(skill.EffectType, skill.EffectValue, skill.EffectDuration);
                        caster.AddEffect(selfEffect);
                        Debug.Log($"[ENFORCE] {caster.Info.Name} | {skill.EffectType} +{skill.EffectValue} ({skill.EffectDuration}턴)");
                        break;
                }

                // 스킬 애니메이션 호출
                caster.RpcPlaySkillAnim(skill.anim);
                caster.MyTurn(false);
                //전투결과 패널 업데이트

                //적 패널 닫기
                BattleManager.Instance.EnemyPanel.SetActive(false);
            }
        }

    }
}