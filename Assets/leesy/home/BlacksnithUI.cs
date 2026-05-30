using System.Collections.Generic;
using UnityEngine;

namespace Lsy
{
    public class BlacksmithUI : BaseUpgradeUI
    {
        [Header("���� �� ��� (����, Ȱ�� ��)")]
        public List<BlacksmithWeaponRow> weaponRows = new List<BlacksmithWeaponRow>();

        [Header("고유 특성 강화 행")]
        [Tooltip("UniqueTraitRow 컴포넌트가 붙은 오브젝트를 연결하세요.")]
        public UniqueTraitRow uniqueTraitRow;

        private void Start()
        {
            // weaponRows와 uniqueTraitRow 둘 다 비어있을 때만 경고
            if (weaponRows.Count == 0 && uniqueTraitRow == null)
                Debug.LogWarning("[BlacksmithUI] weaponRows와 uniqueTraitRow가 모두 비어있습니다. Inspector에서 연결하세요.");

            foreach (var row in weaponRows)
            {
                if (row == null)
                    Debug.LogWarning("[BlacksmithUI] weaponRows에 NULL 항목이 있습니다.");
            }
        }
        protected override IEnumerable<BaseUpgradeRow> GetRows()
        {
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

            // 고유 특성 강화 행 — weaponRows와 동일한 RefreshNodes 흐름으로 처리됩니다.
            if (uniqueTraitRow != null)
                yield return uniqueTraitRow;
        }
    }
}