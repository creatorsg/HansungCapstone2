using UnityEngine;

namespace Lsy
{
    [CreateAssetMenu(fileName = "NewSkill", menuName = "SkillData")]
    public class SkillData : ScriptableObject
    {
        public string skillName;
        public int baseAttack;
        public int baseDefense;
        public Sprite skillSprite;
        public int maxLevel = 3; // ų ִ  
    }

    [System.Serializable]
    public struct PlayerSkill
    {
        // [정보상 일원화] 강화 식별의 정식 키 = skillIndex(0~3, Info.Skills 순서와 1:1).
        // 매칭/조회는 skillIndex로만 한다. skillName은 디버그/레거시 표시용.
        public int skillIndex;   // 이 캐릭터의 몇 번째 스킬인지 (브릿지가 Info.Skills[skillIndex]에 매핑)
        public string skillName; //  ų ĺ
        public int currentLevel; //  ÷̾  ų 
    }
}
