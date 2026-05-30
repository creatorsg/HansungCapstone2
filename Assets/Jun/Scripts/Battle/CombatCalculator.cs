using System.Collections.Generic;
using UnityEngine;

namespace Jun
{
    /// <summary>
    /// </summary>
    public static class CombatCalculator
    {
        // 
        public static bool RollHit(int attackerAcc, int defenderDodge)
        {
            int chance = Mathf.Clamp(75 + attackerAcc - defenderDodge, 10, 95);
            return Random.Range(0, 100) < chance;
        }

        // 
        public static bool RollCrit(int critChance)
        {
            return Random.Range(0, 100) < critChance;
        }

        // 
        public static bool RollResist(int targetRes)
        {
            return Random.Range(0, 100) < targetRes;
        }

        // 
        // 
        public static float CalcDamage(int atk, int def, float rate, bool isCrit, int ctm)
        {
            float raw = atk * rate;
            if (isCrit) raw *= (1f + ctm / 100f);
            return Mathf.Max(1f, raw - def);
        }

        // 
        public static float CalcHeal(float maxHp, float healRate)
        {
            return healRate;
        }

        //
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
            {
                if (e.type == EffectType.DodgeUp) mod += e.value;
                if (e.type == EffectType.DodgeDown) mod -= e.value;
            }
            return baseDodge + (int)mod;
        }

        public static int GetEffectiveCrit(int baseCrit, IList<ActiveEffect> effects)
        {
            float mod = 0f;
            foreach (var e in effects)
                if (e.type == EffectType.CritUp) mod += e.value;
            return baseCrit + (int)mod;
        }

        //
        public static bool IsStunned(IList<ActiveEffect> effects)
        {
            foreach (var e in effects)
                if (e.type == EffectType.Stunned && e.duration > 0) return true;
            return false;
        }

        // 
        public static float CalcDotDamage(IList<ActiveEffect> effects)
        {
            float total = 0f;
            foreach (var e in effects)
                if (e.type == EffectType.Bleeding && e.duration > 0) total += e.value;
            return total;
        }

        public static List<ActiveEffect> TickEffects(IList<ActiveEffect> effects)
        {
            var next = new List<ActiveEffect>();
            foreach (var e in effects)
                if (e.duration > 0) {
                    next.Add(new ActiveEffect(e.type, e.value, e.duration - 1));
                }
            if (next.Count > 0)
            {
                foreach (var e in next)
                    Debug.Log($"[TickEffects] {e.type} | value={e.value} | ���� ��={e.duration}");
            }
            else
            {
                Debug.Log("[TickEffects] ���� ȿ�� ����");
            }
            return next;
        }
    }
}