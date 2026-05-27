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
            // [수정] 하이어라키에서 복사한 정보상 grid가 skillGrids 리스트에 빠져도 NPC 레벨/구매 상태 갱신과 클릭 리스너가 연결되도록 자식 grid도 함께 사용합니다.
            foreach (var grid in skillGrids)
            {
                if (grid != null)
                    yield return grid;
            }

            foreach (var grid in GetComponentsInChildren<InformantSkillGrid>(true))
            {
                if (grid != null && !skillGrids.Contains(grid))
                    yield return grid;
            }
        }
    }
}