using System.Collections.Generic;
using UnityEngine;

namespace Lsy
{
    [System.Serializable]
    public class PlayerInfo //플레이어 정보
    {
        public int Id;      //플레이어 id
        public string Name; //이름
        public List<SkillInfo> Skills;
        public List<ItemInfo> Items;
        public int Lvl;     //레벨
        public int Exp;     //경험치
        public float Hp;      //체력
        public int Atk;     //공격력
        public int Def;     //방어력
        public int Spd;     //스피드
        public int San;     //정신력
        public int Crit;    //치명타 확률
        public int Ctm;     //치명타 계수
        public int Dodge;     //회피율
        public int Acc;     //명중률
        public int Res;     //상태이상저항

        public int WpnId; // 장착 중인 무기 ID
        public int ArmId; // 장착 중인 방어구 ID
        public int Trk1;  // 장신구 슬롯 1
        public int Trk2;  // 장신구 슬롯 2

        public int Gold;  //소지한 골드
    }
}

