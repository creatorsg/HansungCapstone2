using System.Collections.Generic;
using UnityEngine;

namespace Jun
{
    /// <summary>
    /// 디자이너가 인스펙터에서 편집하는 스킬 정의.
    /// CharacterCard / EnemyAI 가 List&lt;SkillSO&gt; 를 들고 있다가
    /// SetUp 시점에 ToSkillInfo()로 SyncVar 가능한 SkillInfo 로 변환합니다.
    /// 메뉴: Create → Battle → Skill
    /// </summary>
    [CreateAssetMenu(fileName = "Skill_", menuName = "Battle/Skill", order = 0)]
    public class SkillSO : ScriptableObject
    {
        [Header("표시")]
        public string DisplayName;
        public SkillType Type = SkillType.Atk;
        public string AnimTrigger;

        [Header("타깃")]
        public TargetType Target = TargetType.SingleEnemy;
        [Tooltip("Single 계열일 때 선택해야 하는 대상 수. AoE/Self는 0 또는 1.")]
        public int TargetCount = 1;

        [Header("효과")]
        [Tooltip("Atk형: 시전자 Atk * 이 값. 1.0 = 기본")]
        public float DamageMultiplier = 1f;
        [Tooltip("Heal형 회복량(고정값)")]
        public int HealAmount = 0;

        [Header("부여 상태이상")]
        public List<StatusApply> StatusEffects = new List<StatusApply>();

        public SkillInfo ToSkillInfo()
        {
            var copied = new List<StatusApply>(StatusEffects.Count);
            foreach (var s in StatusEffects)
            {
                copied.Add(new StatusApply
                {
                    Type     = s.Type,
                    Chance   = s.Chance,
                    Duration = s.Duration,
                    Value    = s.Value
                });
            }

            return new SkillInfo
            {
                Name             = string.IsNullOrEmpty(DisplayName) ? name : DisplayName,
                Type             = Type,
                anim             = AnimTrigger,
                TagetNum         = TargetCount,
                Target           = Target,
                DamageMultiplier = DamageMultiplier,
                HealAmount       = HealAmount,
                StatusEffects    = copied
            };
        }
    }
}
