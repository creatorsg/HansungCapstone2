using System.Collections.Generic;
using UnityEngine;

namespace Jun
{
    /// <summary>
    /// 전투 수치 계산 전담 정적 클래스.
    /// 서버에서만 호출되지만 순수 계산이므로 Mirror 의존 없음.
    /// </summary>
    public static class CombatCalculator
    {
        // 적중 판정: 기본 75% + Acc - Dodge, 최소 10% / 최대 95%
        public static bool RollHit(int attackerAcc, int defenderDodge)
        {
            int chance = Mathf.Clamp(75 + attackerAcc - defenderDodge, 10, 95);
            return Random.Range(0, 100) < chance;
        }

        // 치명타 판정
        public static bool RollCrit(int critChance)
        {
            return Random.Range(0, 100) < critChance;
        }

        // 상태이상 저항 판정 (true = 저항 성공 → 효과 무효)
        public static bool RollResist(int targetRes)
        {
            return Random.Range(0, 100) < targetRes;
        }

        // 물리 피해: max(1, Atk * rate * critMult - Def)
        // Ctm=50 이면 치명타 배율 1.5배
        public static float CalcDamage(int atk, int def, float rate, bool isCrit, int ctm)
        {
            float raw = atk * rate;
            if (isCrit) raw *= (1f + ctm / 100f);
            return Mathf.Max(1f, raw - def);
        }

        // 힐: MaxHp * healRate
        public static float CalcHeal(float maxHp, float healRate)
        {
            return maxHp * healRate;
        }

        //버프/디버프를 반영한 실효 스탯 
        public static int GetEffectiveAtk(int baseAtk, IList<ActiveEffect> effects)
        {
            float mod = 0f;
            foreach (var e in effects)
            {
                if (e.type == EffectType.AtkUp) mod += e.value;
                if (e.type == EffectType.AtkDown) mod -= e.value;
            }
            return Mathf.Max(0, baseAtk + (int)mod);
        }

        public static int GetEffectiveDef(int baseDef, IList<ActiveEffect> effects)
        {
            float mod = 0f;
            foreach (var e in effects)
            {
                if (e.type == EffectType.DefUp) mod += e.value;
                if (e.type == EffectType.DefDown) mod -= e.value;
            }
            return Mathf.Max(0, baseDef + (int)mod);
        }

        public static int GetEffectiveAcc(int baseAcc, IList<ActiveEffect> effects)
        {
            float mod = 0f;
            foreach (var e in effects)
            {
                if (e.type == EffectType.AccUp) mod += e.value;
                if (e.type == EffectType.AccDown) mod -= e.value;
            }
            return baseAcc + (int)mod;
        }

        public static int GetEffectiveDodge(int baseDodge, IList<ActiveEffect> effects)
        {
            float mod = 0f;
            foreach (var e in effects)
                if (e.type == EffectType.DodgeUp) mod += e.value;
            return baseDodge + (int)mod;
        }

        // 기절 여부
        public static bool IsStunned(IList<ActiveEffect> effects)
        {
            foreach (var e in effects)
                if (e.type == EffectType.Stunned && e.duration > 0) return true;
            return false;
        }

        // 출혈 등 DoT 합산
        public static float CalcDotDamage(IList<ActiveEffect> effects)
        {
            float total = 0f;
            foreach (var e in effects)
                if (e.type == EffectType.Bleeding && e.duration > 0) total += e.value;
            return total;
        }

        // 효과 한 턴 경과 (duration 1이면 만료 → 제거)
        public static List<ActiveEffect> TickEffects(IList<ActiveEffect> effects)
        {
            var next = new List<ActiveEffect>();
            foreach (var e in effects)
                if (e.duration > 1) next.Add(new ActiveEffect(e.type, e.value, e.duration - 1));
            return next;
        }
    }
}