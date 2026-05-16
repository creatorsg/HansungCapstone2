using System.Collections.Generic;
using UnityEngine;

namespace Jun
{
    /// <summary>
    /// 적의 스킬 테이블(가중치). 각 적 프리팹에 하나씩 붙입니다.
    /// 매 턴 PickSkill()로 가중치에 따라 스킬을 뽑고, PickTargets()로 타깃을 정합니다.
    /// </summary>
    public class EnemyAI : MonoBehaviour
    {
        [System.Serializable]
        public class WeightedSkill
        {
            public SkillSO Skill;
            [Min(1)] public int Weight = 1;
        }

        [Header("스킬 가중치 테이블 (서버에서만 사용)")]
        [SerializeField] private List<WeightedSkill> _skills = new List<WeightedSkill>();

        /// <summary>가중치 기반 랜덤으로 스킬 1개 선택. 결과는 SkillInfo로 변환되어 반환.</summary>
        public SkillInfo PickSkill()
        {
            if (_skills == null || _skills.Count == 0)
            {
                Debug.LogWarning($"[EnemyAI:{name}] 스킬 테이블이 비어 있습니다. 기본 공격을 반환합니다.");
                return DefaultAttack();
            }

            int total = 0;
            foreach (var w in _skills)
            {
                if (w == null || w.Skill == null) continue;
                total += Mathf.Max(1, w.Weight);
            }

            if (total <= 0) return DefaultAttack();

            int roll = Random.Range(0, total);
            int acc  = 0;
            foreach (var w in _skills)
            {
                if (w == null || w.Skill == null) continue;
                acc += Mathf.Max(1, w.Weight);
                if (roll < acc) return w.Skill.ToSkillInfo();
            }

            return _skills[0].Skill != null ? _skills[0].Skill.ToSkillInfo() : DefaultAttack();
        }

        /// <summary>
        /// 스킬의 TargetType에 따라 타깃 인덱스 리스트를 반환합니다.
        /// SingleAlly/SingleEnemy 는 적 입장에서 "Single 플레이어 1명"을 무작위로 고릅니다.
        /// </summary>
        public List<int> PickTargets(SkillInfo skill, IList<GamePlayerController> players)
        {
            var result = new List<int>();
            if (players == null || players.Count == 0) return result;

            switch (skill.Target)
            {
                case TargetType.AllEnemies:   // 적 입장의 "적" = 플레이어 전체
                case TargetType.AllAllies:    // 미사용 — 적이 아군(=다른 적)을 버프할 때 확장
                    for (int i = 0; i < players.Count; i++) result.Add(i);
                    break;
                case TargetType.Self:
                    // 적이 자신에게 거는 버프 — 플레이어 인덱스 의미 없음. 빈 리스트로 반환.
                    break;
                case TargetType.SingleEnemy:
                case TargetType.SingleAlly:
                default:
                    // 살아있는 플레이어 중 무작위 1명 (HP 0 초과)
                    var alive = new List<int>();
                    for (int i = 0; i < players.Count; i++)
                    {
                        if (players[i] != null && players[i].Info != null && players[i].Info.Hp > 0f)
                            alive.Add(i);
                    }
                    if (alive.Count == 0) break;
                    result.Add(alive[Random.Range(0, alive.Count)]);
                    break;
            }

            return result;
        }

        private static SkillInfo DefaultAttack()
        {
            return new SkillInfo
            {
                Name             = "기본 공격",
                Type             = SkillType.Atk,
                anim             = "",
                TagetNum         = 1,
                Target           = TargetType.SingleEnemy,
                DamageMultiplier = 1f,
                StatusEffects    = new List<StatusApply>()
            };
        }
    }
}
