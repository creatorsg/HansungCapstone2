using System.Collections.Generic;
using UnityEngine;

namespace Jun
{
    /// <summary>
    /// 신 상태이상 시스템의 부여/적용/감소를 담당하는 정적 헬퍼.
    /// 모든 메서드는 서버에서 호출되어야 하며, PlayerInfo.Statuses 를 직접 변경합니다.
    /// HP 변동은 호출자가 RPC로 클라이언트에 동기화해야 합니다.
    /// (옛 ActiveEffect 시스템과는 별개로 운영됩니다.)
    /// </summary>
    public static class StatusProcessor
    {
        /// <summary>스킬에 정의된 상태이상을 확률 굴려서 부여합니다.</summary>
        public static void Apply(PlayerInfo target, List<StatusApply> applies)
        {
            if (target == null || applies == null) return;
            if (target.Statuses == null) target.Statuses = new List<ActiveStatus>();

            foreach (var a in applies)
            {
                if (a == null) continue;
                if (Random.value > a.Chance) continue;

                // 동일 타입이 이미 있으면 지속시간을 더 큰 값으로 갱신, 없으면 추가
                int idx = target.Statuses.FindIndex(s => s.Type == a.Type);
                if (idx >= 0)
                {
                    target.Statuses[idx].RemainingTurns =
                        Mathf.Max(target.Statuses[idx].RemainingTurns, a.Duration);
                    target.Statuses[idx].Value = a.Value;
                }
                else
                {
                    target.Statuses.Add(new ActiveStatus
                    {
                        Type           = a.Type,
                        RemainingTurns = a.Duration,
                        Value          = a.Value
                    });
                }
            }
        }

        /// <summary>
        /// 턴 시작 시 호출. Bleed/Poison 틱 데미지 적용, Stun 여부 판정.
        /// 반환: 이번 턴에 행동 불가(Stun)면 true.
        /// tickDamage out 파라미터에 적용된 총 틱 데미지가 들어갑니다.
        /// </summary>
        public static bool OnTurnStart(PlayerInfo unit, out float tickDamage)
        {
            tickDamage = 0f;
            if (unit == null || unit.Statuses == null) return false;

            bool stunned = false;
            for (int i = 0; i < unit.Statuses.Count; i++)
            {
                var s = unit.Statuses[i];
                switch (s.Type)
                {
                    case StatusType.Bleed:
                    case StatusType.Poison:
                        tickDamage += s.Value;
                        break;
                    case StatusType.Stun:
                        stunned = true;
                        break;
                }
            }

            if (tickDamage > 0f)
            {
                unit.Hp = Mathf.Max(0f, unit.Hp - tickDamage);
            }

            return stunned;
        }

        /// <summary>
        /// 턴 종료 시 호출. 모든 상태이상 RemainingTurns -1, 0 이하 제거.
        /// </summary>
        public static void OnTurnEnd(PlayerInfo unit)
        {
            if (unit == null || unit.Statuses == null) return;

            for (int i = unit.Statuses.Count - 1; i >= 0; i--)
            {
                unit.Statuses[i].RemainingTurns--;
                if (unit.Statuses[i].RemainingTurns <= 0)
                {
                    unit.Statuses.RemoveAt(i);
                }
            }
        }

        // ── 스탯 보정치 (버프/디버프 합산) ────────────────────────────
        public static int AtkBonus(PlayerInfo u) => StatBonus(u, StatusType.AtkUp, StatusType.AtkDown);
        public static int DefBonus(PlayerInfo u) => StatBonus(u, StatusType.DefUp, StatusType.DefDown);
        public static int SpdBonus(PlayerInfo u) => StatBonus(u, StatusType.SpdUp, StatusType.SpdDown);

        private static int StatBonus(PlayerInfo u, StatusType up, StatusType down)
        {
            if (u == null || u.Statuses == null) return 0;
            int bonus = 0;
            foreach (var s in u.Statuses)
            {
                if (s.Type == up)        bonus += s.Value;
                else if (s.Type == down) bonus -= s.Value;
            }
            return bonus;
        }
    }
}
