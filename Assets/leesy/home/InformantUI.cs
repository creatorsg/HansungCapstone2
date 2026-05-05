using System.Collections.Generic;
using UnityEngine;

namespace Lsy
{
    /// <summary>
    /// 정보상 팝업 UI.
    /// 인스펙터에서 skillGrids에 InformantSkillGrid를 등록.
    /// 사진처럼 2x2 그리드면 InformantSkillGrid 하나에 노드 4개를 연결하면 됨.
    /// </summary>
    public class InformantUI : BaseUpgradeUI
    {
        [Header("스킬 그리드 목록")]
        public List<InformantSkillGrid> skillGrids = new List<InformantSkillGrid>();

        protected override IEnumerable<BaseUpgradeRow> GetRows()
        {
            return skillGrids;
        }
    }
}