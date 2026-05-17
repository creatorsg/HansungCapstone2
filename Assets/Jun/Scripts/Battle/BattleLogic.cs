using Jun;
using Mirror;
using System.Collections.Generic;
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
                int damageDelta = caster.GetTotalDamageDelta(skillIndex);
                int totalDamage = caster.Info.Atk + damageDelta;
                if (damageDelta != 0)
                    Debug.Log($"[BattleLogic] 스킬트리 강화 적용: Atk={caster.Info.Atk} + damageDelta={damageDelta} = {totalDamage}");

                foreach (int targetIdx in targets)
                {
                    if (isEnemy)
                    {
                        var enemyList = manager.Enemys[manager.StageNum - 1].Enemys;
                        if (targetIdx < 0 || targetIdx >= enemyList.Count)
                        {
                            Debug.LogWarning($"[BattleLogic] 적 인덱스 {targetIdx} 범위 초과");
                            continue;
                        }
                        var target = enemyList[targetIdx].GetComponent<EnemyModel>();
                        if (target != null) target.Damaged(totalDamage);
                    }
                    else
                    {
                        // 아군 대상 스킬 (Heal, Buff 등)
                        if (targetIdx < 0 || targetIdx >= manager._players.Count)
                        {
                            Debug.LogWarning($"[BattleLogic] 플레이어 인덱스 {targetIdx} 범위 초과");
                            continue;
                        }
                        // TODO: 아군 스킬 효과 처리 (힐, 버프 등)
                        Debug.Log($"[BattleLogic] 아군 대상 스킬: {skill.Name} → 플레이어 {targetIdx}");
                    }
                }

                caster.RpcPlaySkillAnim(skill.anim);
                manager.EnemyPanel.SetActive(false);

                // Animator가 설정되어 있으면 EndAnim Animation Event가 턴을 종료합니다.
                // Animator가 없거나 테스트 중이면 여기서 바로 다음 턴으로 넘깁니다.
                if (caster.GetComponent<Animator>() == null || string.IsNullOrEmpty(skill.anim))
                {
                    BattleManager.Instance.NextTurn();
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
                    if (targetIdx < 0 || targetIdx >= manager._players.Count)
                    {
                        Debug.LogWarning($"[BattleLogic] 아이템 대상 인덱스 {targetIdx} 범위 초과");
                        continue;
                    }
                    // TODO: 아이템 효과 처리 (회복 등)
                    Debug.Log($"[BattleLogic] 아이템 {item.Name} → 플레이어 {targetIdx}");
                }

                // 아이템 사용 후 턴 종료 (애니메이션 없는 경우 즉시)
                BattleManager.Instance.NextTurn();
            }
            else
            {
                Debug.LogWarning("[BattleLogic] skillIndex도 itemIndex도 -1입니다. 아무 동작도 하지 않음.");
            }
        }

    }
}
