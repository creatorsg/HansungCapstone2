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
        public int maxLevel = 3; // 스킬 최대 레벨 제한
    }

    // 미러(Mirror) 네트워크에서 안전하게 동기화하기 위한 구조체
    [System.Serializable]
    public struct PlayerSkill
    {
        public string skillName; // 어떤 스킬인지 식별
        public int currentLevel; // 이 플레이어의 현재 스킬 레벨
    }
}