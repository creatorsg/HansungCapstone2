using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lsy
{
    /// <summary>
    /// 고유 특성 강화 UI 행. BlacksmithUI의 uniqueTraitRow 슬롯에 연결합니다.
    ///
    /// ★ Unity Editor 연결 방법
    ///   1. BlacksmithUI 오브젝트 하위에 빈 오브젝트를 만들고 이 컴포넌트를 추가합니다.
    ///   2. traitImage     → 캐릭터마다 다른 고유 특성 아이콘을 표시할 Image를 연결합니다.
    ///   3. upgradeNodes   → BaseUpgradeRow의 UpgradeNode를 최대 3개 순서대로 연결합니다.
    ///                       노드 0 = 1단계, 노드 1 = 2단계, 노드 2 = 3단계.
    ///   4. BlacksmithUI의 uniqueTraitRow 슬롯에 이 오브젝트를 연결합니다.
    ///
    /// ★ 스팸 클릭 방지
    ///   OnNodeClicked에서 _isUpgrading = true로 락을 걸고,
    ///   서버 응답 후 uniqueTraitLevel SyncVar 변경 → hook → RefreshUpgradeUI →
    ///   OnRefreshCompleted에서 _isUpgrading = false로 해제합니다.
    /// </summary>
    public class UniqueTraitRow : BaseUpgradeRow
    {
        [Header("특성 아이콘 이미지")]
        [Tooltip("캐릭터마다 다른 고유 특성 아이콘이 표시될 Image를 연결하세요. CharacterCard의 UniqueTrait 아이콘을 자동으로 표시합니다.")]
        [SerializeField] private Image traitImage;

        [Header("강화 진행 텍스트 (선택)")]
        [SerializeField] private TextMeshProUGUI upgradeProgressText;

        // 연속 클릭 방지 락 (클라이언트 전용)
        private bool _isUpgrading = false;

        // ── BaseUpgradeRow 구현 ──────────────────────────────────────────────

        /// <summary>노드 인덱스에 해당하는 강화 단계의 비용을 반환합니다.</summary>
        protected override int GetNodePrice(int nodeIndex)
        {
            // nodeIndex 0 → 1단계, nodeIndex 1 → 2단계, nodeIndex 2 → 3단계
            UniqueTraitSO trait = GetCurrentTrait();
            if (trait == null) return 0;

            int level = nodeIndex + 1;
            if (level > UniqueTraitSO.MaxLevel) return 0;

            return trait.GetLevel(level).price;
        }

        /// <summary>해당 노드를 지금 구매(강화)할 수 있는지 판단합니다.</summary>
        protected override bool CanPurchaseNode(int nodeIndex, int npcLevel, CharacterUnit unit)
        {
            if (unit == null) return false;

            UniqueTraitSO trait = GetCurrentTrait(unit);
            if (trait == null) return false;

            int targetLevel = nodeIndex + 1;
            if (targetLevel > UniqueTraitSO.MaxLevel) return false;

            // 정확히 현재 레벨의 바로 다음 단계만 구매 가능
            // nodeIndex 0 (1단계): uniqueTraitLevel == 0 이어야 함
            // nodeIndex 1 (2단계): uniqueTraitLevel == 1 이어야 함
            // nodeIndex 2 (3단계): uniqueTraitLevel == 2 이어야 함
            int currentLevel = unit.uniqueTraitLevel;

            if (currentLevel != nodeIndex) return false; // 이미 강화됐거나 선행 단계 미완료

            return true;
        }

        /// <summary>노드 클릭 시 호출됩니다.</summary>
        protected override void OnNodeClicked(int nodeIndex)
        {
            // 처리 중이면 추가 클릭 무시
            if (_isUpgrading)
            {
                Debug.Log("[UniqueTraitRow] 강화 처리 중 - 중복 클릭 무시");
                return;
            }

            CharacterUnit unit = PlayerAccount.LocalInstance?.currentSelectedCharacter;
            if (unit == null) return;

            UniqueTraitSO trait = GetCurrentTrait(unit);
            if (trait == null)
            {
                Debug.LogWarning("[UniqueTraitRow] 캐릭터의 고유 특성(UniqueTraitSO)이 CharacterRegistry에 없습니다.");
                return;
            }

            CharacterShop shop = GetLocalShop();
            if (shop == null) return;

            int targetLevel = nodeIndex + 1;

            _isUpgrading = true; // 락 설정 (서버 응답 후 OnRefreshCompleted에서 해제)
            shop.CmdUpgradeUniqueTrait(targetLevel, unit.heroCode);
        }

        /// <summary>RefreshNodes 완료 후 아이콘/텍스트를 갱신합니다.</summary>
        protected override void OnRefreshCompleted(int npcLevel, CharacterUnit unit)
        {
            // ── 특성 아이콘 갱신 ────────────────────────────────────────────
            if (traitImage != null)
            {
                Sprite icon = GetCurrentTrait(unit)?.icon;
                traitImage.sprite  = icon;
                traitImage.enabled = icon != null;
            }

            // ── 강화 진행 텍스트 갱신 ────────────────────────────────────────
            if (upgradeProgressText != null)
            {
                int currentLevel = unit != null ? unit.uniqueTraitLevel : 0;
                upgradeProgressText.text = currentLevel >= UniqueTraitSO.MaxLevel
                    ? "MAX"
                    : $"Lv.{currentLevel}";
            }

            // ── 서버 응답이 왔으므로 락 해제 ────────────────────────────────
            _isUpgrading = false;
        }

        // ── 유틸 ────────────────────────────────────────────────────────────

        /// <summary>현재 선택된 캐릭터의 UniqueTraitSO를 CharacterRegistry에서 조회합니다.</summary>
        private UniqueTraitSO GetCurrentTrait(CharacterUnit unit = null)
        {
            string heroCode = unit != null
                ? unit.heroCode
                : PlayerAccount.LocalInstance?.currentSelectedCharacter?.heroCode;

            if (string.IsNullOrEmpty(heroCode)) return null;

            if (CharacterRegistry.TryGet(heroCode, out var entry))
                return entry.UniqueTrait;

            return null;
        }
    }
}
