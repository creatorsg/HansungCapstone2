using Jun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lsy
{
    /// <summary>
    /// 아이템 슬롯에 마우스를 올리면 해당 아이템의 적용 수치를 팝업으로 표시합니다.
    /// - 수치가 0인 항목은 표시하지 않습니다.
    /// - Canvas의 자식으로 배치하고, 다른 UI 위에 떠야 하므로 마지막 자식으로 배치하세요.
    ///
    /// ▶ 사용법 (Unity Editor)
    ///   1. Canvas 하위에 Panel(ItemTooltip)을 만들고 이 컴포넌트를 추가합니다.
    ///   2. titleText, statsText, panel 레퍼런스를 연결합니다.
    ///   3. InventorySlotUI의 tooltipUI 슬롯에 이 오브젝트를 연결합니다.
    ///      또는 씬에 하나만 두고 ItemTooltipUI.Instance로 전역 접근합니다.
    /// </summary>
    public class ItemTooltipUI : MonoBehaviour
    {
        public static ItemTooltipUI Instance { get; private set; }

        [Header("패널 루트 (활성/비활성으로 토글)")]
        [SerializeField] private GameObject panel;

        [Header("텍스트 필드")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI statsText;

        [Header("위치 옵션")]
        [Tooltip("마우스 커서로부터의 오프셋 (픽셀)")]
        [SerializeField] private Vector2 offset = new Vector2(15f, -15f);

        private RectTransform _rect;
        private Canvas        _canvas;

        // ─────────────────────────────────────────────────────────

        private void Awake()
        {
            Instance = this;
            _rect   = GetComponent<RectTransform>();
            _canvas = GetComponentInParent<Canvas>();
            Hide();
        }

        private void Update()
        {
            if (panel != null && panel.activeSelf)
                FollowMouse();
        }

        // ─────────────────────────────────────────────────────────
        //  공개 API
        // ─────────────────────────────────────────────────────────

        /// <summary>InventoryItem(소모품 / 무기 / 방어구) 툴팁 표시</summary>
        public void Show(InventoryItem item)
        {
            if (panel == null) return;

            string title, desc, stats;

            switch (item.Type)
            {
                case ItemType.Consumable when item.ConsumInfo != null:
                    BuildConsumableTooltip(item.ConsumInfo, out title, out desc, out stats);
                    break;
                case ItemType.Weapon when item.EquipInfo != null:
                    BuildEquipTooltip("⚔ " + item.EquipInfo.Name, item.EquipInfo, out title, out desc, out stats);
                    break;
                case ItemType.Armor when item.EquipInfo != null:
                    BuildEquipTooltip("🛡 " + item.EquipInfo.Name, item.EquipInfo, out title, out desc, out stats);
                    break;
                default:
                    title = item.itemName;
                    desc  = "";
                    stats = "";
                    break;
            }

            ApplyTexts(title, desc, stats);
            panel.SetActive(true);
            panel.transform.SetAsLastSibling(); // 항상 최상위에 표시
            FollowMouse();
        }

        /// <summary>툴팁 숨기기</summary>
        public void Hide()
        {
            if (panel != null) panel.SetActive(false);
        }

        // ─────────────────────────────────────────────────────────
        //  내부 빌더
        // ─────────────────────────────────────────────────────────

        private void BuildConsumableTooltip(ConsumableInfo info,
            out string title, out string desc, out string stats)
        {
            title = info.Name;
            desc  = info.description ?? "";

            var sb = new System.Text.StringBuilder();

            // 타입 표시
            sb.AppendLine($"종류: {info.Type}");

            // 회복량
            if (info.HealRate != 0f)
                sb.AppendLine($"회복률: {info.HealRate * 100:F0}%");

            // 대상 수
            if (info.TagetNum > 0)
                sb.AppendLine($"대상: {info.TagetNum}명");

            // 타깃 유형
            sb.AppendLine($"사용 범위: {info.Target}");

            stats = sb.ToString().TrimEnd();
        }

        private void BuildEquipTooltip(string headerName, EqpInfo info,
            out string title, out string desc, out string stats)
        {
            title = headerName;
            desc  = info.description ?? "";

            var sb = new System.Text.StringBuilder();

            if (info.Hp   != 0) sb.AppendLine($"HP    {FormatStat(info.Hp)}");
            if (info.San  != 0) sb.AppendLine($"정신력 {FormatStat(info.San)}");
            if (info.Atk  != 0) sb.AppendLine($"공격력 {FormatStat(info.Atk)}");
            if (info.Def  != 0) sb.AppendLine($"방어력 {FormatStat(info.Def)}");
            if (info.Spd  != 0) sb.AppendLine($"속도   {FormatStat(info.Spd)}");
            if (info.Crit != 0) sb.AppendLine($"치명타 {FormatStat(info.Crit)}%");
            if (info.Ctm  != 0) sb.AppendLine($"치명배율 {FormatStat(info.Ctm)}%");
            if (info.Dodge!= 0) sb.AppendLine($"회피율 {FormatStat(info.Dodge)}%");
            if (info.Acc  != 0) sb.AppendLine($"명중률 {FormatStat(info.Acc)}%");
            if (info.Res  != 0) sb.AppendLine($"저항   {FormatStat(info.Res)}%");

            stats = sb.ToString().TrimEnd();
        }

        private static string FormatStat(float v) => v > 0 ? $"+{v}" : $"{v}";
        private static string FormatStat(int v)   => v > 0 ? $"+{v}" : $"{v}";

        private void ApplyTexts(string title, string desc, string stats)
        {
            if (titleText       != null) titleText.text       = title;
            if (descriptionText != null) descriptionText.text = desc;
            if (statsText       != null) statsText.text       = stats;
        }

        private void FollowMouse()
        {
            if (_rect == null || _canvas == null) return;

            Vector2 mousePos = Input.mousePosition;
            if (_canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                _rect.position = mousePos + offset;
                ClampToScreen();
            }
            else
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _canvas.transform as RectTransform,
                    mousePos, _canvas.worldCamera,
                    out Vector2 localPos);
                _rect.localPosition = localPos + offset;
            }
        }

        private void ClampToScreen()
        {
            Vector3 pos = _rect.position;
            float   w   = _rect.rect.width  * _rect.lossyScale.x;
            float   h   = _rect.rect.height * _rect.lossyScale.y;

            pos.x = Mathf.Clamp(pos.x, 0, Screen.width  - w);
            pos.y = Mathf.Clamp(pos.y, h, Screen.height);
            _rect.position = pos;
        }
    }
}
