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
            // [수정] 하이어라키에서 복사한 무기 row가 weaponRows 리스트에 빠져도 갱신/클릭 리스너가 연결되도록 자식 row도 함께 사용합니다.
            foreach (var row in weaponRows)
            {
                if (row != null)
                    yield return row;
            }

            foreach (var row in GetComponentsInChildren<BlacksmithWeaponRow>(true))
            {
                if (row != null && !weaponRows.Contains(row))
                    yield return row;
            }
        }
    }
}