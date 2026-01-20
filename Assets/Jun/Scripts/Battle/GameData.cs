using UnityEngine;

namespace Jun
{
    public enum SkillType //스킬 종류
    {
        Atk,
        Heal,
        Buff,
        Debuff,
        Enforce
    }
    public enum UnitState //유닛 상태 정보
    {
        Waiting,       // 기다리는 중
        Acting,        // 행동 중
        Incapacitated  // 행동 불능
    }


    [System.Serializable]
    public class PlayerInfo //플레이어 정보
    {
        public int Id;      //플레이어 id
        public int Lvl;     //레벨
        public int Exp;     //경험치
        public int Hp;      //체력
        public int Atk;     //공격력
        public int Def;     //방어력
        public int Spd;     //스피드
        public int Mad;     //정신력
        public int Cpt;     //치명타 확률
        public int Ctm;     //치명타 계수
        public int Ddg;     //회피율

        public int WpnId; // 장착 중인 무기 ID
        public int ArmId; // 장착 중인 방어구 ID
        public int Trk1;  // 장신구 슬롯 1
        public int Trk2;  // 장신구 슬롯 2
    }

    [System.Serializable]
    public class SkillInfo // 스킬 정보
    {
        public string Name;
        public SkillType Type;
        public string anim;
        public int TagetNum;
        //다른 정보들
    }
    [System.Serializable]
    public class ItemInfo //아이템 정보
    {
        public string Name;
        public int TagetNum;

        //다른 정보들
    }
    [System.Serializable]
    public class EqpInfo // 장비 정보
    {
        public string Name;
        public int EqpId;   // 장비 고유 번호
                            //다른 정보들
    }
}
