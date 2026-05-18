using UnityEngine;

namespace Lsy
{
    [CreateAssetMenu(fileName = "Character Data", menuName = "Character Data")]
    public class CharacterData : ScriptableObject
    {
        [Header("기본 정보")]
        public int charId;
        public string charName;

        [Header("성장 및 재화")]
        public int level = 1;
        public int exp = 0;
        public int gold = 2000;

        [Header("전투 능력치")]
        public float maxHp;
        public int maxSan;
        public int atk;
        public int def;
        public int spd;
        public int crit;
        public int ctm;
        public int dodge;
        public int acc;
        public int res;

        [Header("초기 장비 ID (0 = 없음)")]
        public int weaponId;
        public int armorId;
        public int trinket1Id;
        public int trinket2Id;
    }
}
