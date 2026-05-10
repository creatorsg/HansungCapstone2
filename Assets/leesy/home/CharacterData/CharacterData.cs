using System.Collections.Generic;
using UnityEngine;

namespace Lsy
{
    [CreateAssetMenu(fileName = "Character Data", menuName = "Character Data")]
    public class CharacterData : ScriptableObject
    {
        [Header("기본 정보")]
        public int charId;           // 고유 ID
        public string charName;      // 직업/캐릭터 이름

        [Header("재화 및 레벨")]
        public int level = 1;
        public int exp = 0;
        public int gold = 2000; // 초기 소지 골드

        [Header("전투 스탯 (최대치 기준)")]
        public float maxHp;          // 최대 체력
        public int maxSan;           // 최대 정신력
        public int atk;              // 공격력
        public int def;              // 방어력
        public int spd;              // 스피드
        public int crit;             // 치명타 확률
        public int ctm;              // 치명타 계수
        public int dodge;            // 회피율
        public int acc;              // 명중률
        public int res;              // 상태이상 저항

        [Header("초기 장착 장비 ID (0 = 미장착)")]
        public int weaponId;
        public int armorId;
        public int trinket1Id;
        public int trinket2Id;
    }
}