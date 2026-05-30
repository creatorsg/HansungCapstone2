using System.Collections.Generic;
using UnityEngine;
using Jun; // EffectType / TargetType (Jun.SkillInfo와 동일 enum)

namespace Lsy
{
    
 // nodes[0]: skillId="tracking", skillName="", price=1000, requiredNpcLevel=1
 // nodes[1]: skillId="disguise", skillName="", price=2000, requiredNpcLevel=1
 // nodes[2]: skillId="sabotage", skillName="ذ", price=3000, requiredNpcLevel=2
 // nodes[3]: skillId="ambush", skillName="ź", price=4000, requiredNpcLevel=2

    [CreateAssetMenu(fileName = "SkillUpgradeData", menuName = "Upgrade/SkillUpgradeData")]
    public class SkillUpgradeData : ScriptableObject
    {
        public string groupName;

        // [정보상 일원화] 이 SO가 어느 캐릭터의 강화 데이터인지. 키 = (characterCode, node.skillIndex, node.targetLevel).
        // 한 캐릭터당 SO 1개를 권장(4스킬 × Lv2/3/4 = 12노드). SkillUpgradeRegistry가 이 값으로 인덱싱한다.
        public string characterCode;

        public List<SkillUpgradeNode> nodes = new List<SkillUpgradeNode>();
    }

    [System.Serializable]
    public class SkillUpgradeNode
    {
        public string skillId;
        public string skillName;

        // 정보상 UI에서 이 레벨 노드를 누르면 표시할 설명(레벨별로 자유롭게 작성).
        [TextArea(2, 4)]
        public string skillDescription;

        // ── [정보상 일원화] 정식 키 ──
        public int skillIndex;   // 0~3, Info.Skills 순서와 1:1
        public int targetLevel;  // 이 노드를 구매하면 도달하는 레벨 (2/3/4)

        // ── 전투 수치 오버라이드 (절대값, Jun.SkillInfo에 그대로 매핑) ──
        // 수치 필드: 0 = 무시(base 유지). enum 필드: use*Override == true 일 때만 덮어쓴다
        // (TargetType.SingleEnemy / EffectType.AtkUp 가 0이라 0=무시로 쓰면 그 값을 못 넣음).
        public float DamageRate;        // 0=무시
        public float HealRate;          // 0=무시
        public float EffectValue;       // 0=무시
        public int   EffectDuration;    // 0=무시

        public bool       useTargetOverride;
        public TargetType Target;
        public bool       useEffectOverride;
        public EffectType EffectType;

        public int price;
        public int requiredNpcLevel;
    }
}
