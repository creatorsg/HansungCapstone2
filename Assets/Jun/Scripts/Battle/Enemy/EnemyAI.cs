using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace Jun
{
    /// <summary>
    /// 서버 전용. 적 턴에 어떤 스킬을 쓸지, 누구를 타겟할지 결정한다.
    /// 반환하는 targets 인덱스는 항상 원본 리스트(_players / _enemys) 기준.
    /// </summary>
    public static class EnemyAI
    {
        public struct EnemyAction
        {
            public int skillIndex;
            public List<int> targets; // _players 또는 _enemys 원본 인덱스
        }

        /// <summary>
        /// 적의 행동을 결정한다.
        /// allPlayers: BattleManager._players 원본 리스트
        /// allEnemies: 현재 스테이지 적 원본 리스트
        /// 스킬이 없으면 null 반환 (턴 스킵).
        /// </summary>
        public static EnemyAction? ChooseAction(
            EnemyController caster,
            SyncList<GamePlayerController> allPlayers,
            List<EnemyController> allEnemies)
        {
            var skills = caster.Info.Skills;
            if (skills == null || skills.Count == 0)
            {
                Debug.LogWarning($"[EnemyAI] '{caster.Info.Name}'에 스킬이 없습니다. 턴을 스킵합니다. 프리팹에 최소 1개 Atk 스킬을 넣어주세요.");
                return null;
            }

            // 살아있는 플레이어/적의 원본 인덱스 수집
            var alivePlayerIndices = new List<int>();
            for (int i = 0; i < allPlayers.Count; i++)
            {
                if (allPlayers[i] != null && allPlayers[i].Info != null && allPlayers[i].Info.Hp > 0)
                    alivePlayerIndices.Add(i);
            }

            var aliveEnemyIndices = new List<int>();
            for (int i = 0; i < allEnemies.Count; i++)
            {
                if (allEnemies[i] != null && allEnemies[i].Info != null && allEnemies[i].Info.Hp > 0 && allEnemies[i].gameObject.activeSelf)
                    aliveEnemyIndices.Add(i);
            }

            if (alivePlayerIndices.Count == 0)
            {
                Debug.LogWarning("[EnemyAI] 살아있는 플레이어가 없습니다.");
                return null;
            }

            // 힐 스킬 우선 체크: 아군 중 HP 30% 이하가 있으면 힐
            int healIdx = FindSkillIndexByType(skills, SkillType.Heal);
            if (healIdx != -1 && aliveEnemyIndices.Count > 0)
            {
                int woundedAllyOrigIdx = FindMostWoundedAlly(allEnemies, aliveEnemyIndices, 0.3f);
                if (woundedAllyOrigIdx != -1)
                {
                    return new EnemyAction
                    {
                        skillIndex = healIdx,
                        targets = new List<int> { woundedAllyOrigIdx }
                    };
                }
            }

            // 사용 가능한 스킬 분류
            var atkSkills = new List<int>();
            var debuffSkills = new List<int>();
            var buffSkills = new List<int>();
            var enforceSkills = new List<int>();

            for (int i = 0; i < skills.Count; i++)
            {
                switch (skills[i].Type)
                {
                    case SkillType.Atk: atkSkills.Add(i); break;
                    case SkillType.Debuff: debuffSkills.Add(i); break;
                    case SkillType.Buff: buffSkills.Add(i); break;
                    case SkillType.Enforce: enforceSkills.Add(i); break;
                }
            }

            // 확률 기반 행동 선택
            float roll = Random.value;

            // 15% 확률로 Buff 사용
            if (roll < 0.15f && buffSkills.Count > 0 && aliveEnemyIndices.Count > 0)
            {
                int idx = buffSkills[Random.Range(0, buffSkills.Count)];
                int targetOrigIdx = aliveEnemyIndices[Random.Range(0, aliveEnemyIndices.Count)];
                return new EnemyAction
                {
                    skillIndex = idx,
                    targets = new List<int> { targetOrigIdx }
                };
            }

            // 15% 확률로 Enforce 사용
            if (roll < 0.3f && enforceSkills.Count > 0)
            {
                int idx = enforceSkills[Random.Range(0, enforceSkills.Count)];
                return new EnemyAction
                {
                    skillIndex = idx,
                    targets = new List<int> { 0 } // self, 인덱스 의미 없음
                };
            }

            // 20% 확률로 Debuff 사용
            if (roll < 0.5f && debuffSkills.Count > 0)
            {
                int idx = debuffSkills[Random.Range(0, debuffSkills.Count)];
                var targets = PickFromOriginalIndices(alivePlayerIndices, skills[idx].TagetNum);
                return new EnemyAction
                {
                    skillIndex = idx,
                    targets = targets
                };
            }

            // 나머지: Atk 스킬 사용
            if (atkSkills.Count > 0)
            {
                int idx = atkSkills[Random.Range(0, atkSkills.Count)];
                var targets = PickFromOriginalIndices(alivePlayerIndices, skills[idx].TagetNum);
                return new EnemyAction
                {
                    skillIndex = idx,
                    targets = targets
                };
            }

            // Atk 스킬도 없으면 아무 스킬이나 사용
            int fallbackIdx = Random.Range(0, skills.Count);
            var fallbackSkill = skills[fallbackIdx];
            bool isPlayerTarget = fallbackSkill.Type == SkillType.Atk || fallbackSkill.Type == SkillType.Debuff;
            var fallbackTargets = isPlayerTarget
                ? PickFromOriginalIndices(alivePlayerIndices, fallbackSkill.TagetNum)
                : new List<int> { 0 };

            return new EnemyAction
            {
                skillIndex = fallbackIdx,
                targets = fallbackTargets
            };
        }

        /// <summary>
        /// 살아있는 인덱스 목록에서 count개를 랜덤 선택 (원본 인덱스 반환)
        /// </summary>
        private static List<int> PickFromOriginalIndices(List<int> aliveIndices, int count)
        {
            count = Mathf.Max(1, count);
            var targets = new List<int>();

            // 셔플 방식으로 중복 없이 선택
            var shuffled = new List<int>(aliveIndices);
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
            }

            for (int i = 0; i < count && i < shuffled.Count; i++)
            {
                targets.Add(shuffled[i]);
            }
            return targets;
        }

        private static int FindSkillIndexByType(List<SkillInfo> skills, SkillType type)
        {
            for (int i = 0; i < skills.Count; i++)
            {
                if (skills[i].Type == type) return i;
            }
            return -1;
        }

        /// <summary>
        /// 살아있는 적 중 HP 비율이 threshold 이하인 가장 약한 적의 원본 인덱스 반환
        /// </summary>
        private static int FindMostWoundedAlly(List<EnemyController> allEnemies, List<int> aliveIndices, float threshold)
        {
            int bestOrigIdx = -1;
            float lowestRatio = 1f;

            foreach (int origIdx in aliveIndices)
            {
                var info = allEnemies[origIdx].Info;
                if (info.MaxHp <= 0f) continue;
                float ratio = info.Hp / info.MaxHp;
                if (ratio < threshold && ratio < lowestRatio)
                {
                    lowestRatio = ratio;
                    bestOrigIdx = origIdx;
                }
            }
            return bestOrigIdx;
        }
    }
}
