using Jun;
using Mirror;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

namespace Jun
{
    public class BattleLogic : MonoBehaviour
    {
        // 나중에는 적도 쓸수있게끔 변경 예정
        [Server]
        public void BattleAction(GamePlayerController caster, int skillIndex, int itemIndex, List<int> targets)
        {
            // 배틀 로직 구현(플레이어, 적 둘 다 사용가능하게끔)
            if (skillIndex != -1)
            {
                SkillInfo skill = caster.Info.Skills[skillIndex];

                foreach (int targetIdx in targets)
                {
                    // 타겟 공격등 실제 배틀 로직
                }

                // 공격자의 애니메이션 호출
                caster.RpcPlaySkillAnim(skill.anim);
                //적들의 정보 업데이트
            }
        }

    }
}
