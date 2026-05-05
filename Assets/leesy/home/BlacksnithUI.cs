using System.Collections.Generic;
using UnityEngine;

namespace Lsy
{
    public class BlacksmithUI : BaseUpgradeUI
    {
        [Header("무기 줄 목록 (검줄, 활줄 등)")]
        public List<BlacksmithWeaponRow> weaponRows = new List<BlacksmithWeaponRow>();

        private void Start()
        {
            // 인스펙터 연결 상태 초기 검증
            if (weaponRows.Count == 0)
                Debug.LogWarning("[BlacksmithUI] weaponRows가 비어있습니다. 인스펙터에서 연결하세요.");

            foreach (var row in weaponRows)
            {
                if (row == null)
                    Debug.LogWarning("[BlacksmithUI] weaponRows에 NULL 항목이 있습니다.");
            }
        }

        protected override IEnumerable<BaseUpgradeRow> GetRows()
        {
            return weaponRows;
        }
    }
}