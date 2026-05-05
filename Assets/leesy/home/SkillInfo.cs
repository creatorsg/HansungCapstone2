using System.Collections.Generic;
using UnityEngine;

namespace Lsy
{
    [System.Serializable]
    public class SkillInfo // 스킬 정보
    {
        public string Name;
        //public SkillType Type;
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
}
