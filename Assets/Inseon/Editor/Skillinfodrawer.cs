#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Jun;

[CustomPropertyDrawer(typeof(SkillInfo))]
public class SkillInfoDrawer : PropertyDrawer
{
    private const float LINE = 20f;
    private const float PAD = 3f;
    private const float INDENT = 10f;

    // OnGUI에서 실제로 사용한 높이를 캐싱 → GetPropertyHeight가 정확하게 반환
    private static readonly Dictionary<string, float> _heightCache = new Dictionary<string, float>();

    // ── 타입별 색상 ──────────────────────────────
    private static Color GetTypeColor(SkillType type) => type switch
    {
        SkillType.Atk => new Color(0.85f, 0.25f, 0.25f),
        SkillType.Heal => new Color(0.20f, 0.75f, 0.35f),
        SkillType.Buff => new Color(0.20f, 0.55f, 0.90f),
        SkillType.Debuff => new Color(0.65f, 0.20f, 0.85f),
        SkillType.Enforce => new Color(0.90f, 0.70f, 0.10f),
        _ => Color.gray
    };

    private static string GetTypeLabel(SkillType type) => type switch
    {
        SkillType.Atk => "ATTACK",
        SkillType.Heal => "HEAL",
        SkillType.Buff => "BUFF",
        SkillType.Debuff => "DEBUFF",
        SkillType.Enforce => "ENFORCE",
        _ => type.ToString()
    };

    private static string CacheKey(SerializedProperty p) => p.propertyPath;

    // ── 높이 계산 ─────────────────────────────────
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.isExpanded) return LINE + PAD;

        // 캐시된 실제 높이 반환, 없으면 기본값
        if (_heightCache.TryGetValue(CacheKey(property), out float cached))
            return cached;

        // 캐시 없을 때 fallback 계산 (처음 렌더 전)
        var skillType = (SkillType)property.FindPropertyRelative("Type").enumValueIndex;
        float h = (LINE + PAD * 3)           // 헤더
                + (LINE + 4)                 // 섹션 레이블 1
                + (LINE + PAD) * 6           // 공통 6개
                + PAD                        // 여백
                + LINE                       // 설명 레이블
                + (LINE * 3 + PAD * 3)       // TextArea
                + (LINE + 4)                 // 섹션 레이블 2
                + PAD * 2;                   // 하단 여백

        h += skillType switch
        {
            SkillType.Atk => (LINE + PAD),
            SkillType.Heal => (LINE + PAD),
            SkillType.Buff or SkillType.Debuff or SkillType.Enforce => (LINE + PAD) * 3,
            _ => 0
        };

        return h;
    }

    // ── 그리기 ────────────────────────────────────
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        var typeProp = property.FindPropertyRelative("Type");
        var nameProp = property.FindPropertyRelative("Name");
        var skillType = (SkillType)typeProp.enumValueIndex;
        var typeColor = GetTypeColor(skillType);

        // ── 전체 배경 ───────────────────────────
        var bgRect = new Rect(position.x, position.y + 1, position.width, position.height - 2);
        EditorGUI.DrawRect(bgRect, new Color(0.13f, 0.13f, 0.13f, 0.25f));

        // ── 헤더 바 ─────────────────────────────
        var headerRect = new Rect(position.x, position.y + 1, position.width, LINE + 2);
        EditorGUI.DrawRect(headerRect, new Color(typeColor.r * 0.5f, typeColor.g * 0.5f, typeColor.b * 0.5f, 1f));

        // 타입 뱃지
        var badgeRect = new Rect(position.x + 2, position.y + 3, 58, LINE - 2);
        EditorGUI.DrawRect(badgeRect, typeColor);
        EditorGUI.LabelField(badgeRect, GetTypeLabel(skillType), new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 9,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        });

        // 스킬 이름
        string skillName = string.IsNullOrEmpty(nameProp.stringValue) ? "(이름 없음)" : nameProp.stringValue;
        EditorGUI.LabelField(
            new Rect(position.x + 66, position.y + 2, position.width - 80, LINE),
            skillName,
            new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 11,
                normal = { textColor = Color.white }
            });

        // Foldout (헤더 전체 클릭 영역)
        property.isExpanded = EditorGUI.Foldout(
            new Rect(position.x, position.y + 1, position.width, LINE + 2),
            property.isExpanded, GUIContent.none, true);

        if (!property.isExpanded)
        {
            EditorGUI.EndProperty();
            return;
        }

        // ── 본문 영역 ───────────────────────────
        float y = position.y + LINE + PAD * 3;
        float x = position.x + INDENT;
        float w = position.width - INDENT * 2;

        // 섹션: 기본 정보
        DrawSectionLabel(ref y, x, w, "기본 정보", new Color(0.75f, 0.75f, 0.75f));
        DrawProp(ref y, x, w, property, "Name", "이름", 0);
        DrawProp(ref y, x, w, property, "User", "사용자", 1);
        DrawProp(ref y, x, w, property, "Type", "스킬 타입", 0);
        DrawProp(ref y, x, w, property, "anim", "애니메이션", 1);
        DrawProp(ref y, x, w, property, "TagetNum", "대상 수", 0);
        DrawProp(ref y, x, w, property, "icon", "아이콘", 1);

        // 설명 필드
        y += PAD;
        EditorGUI.LabelField(new Rect(x, y, w, LINE), "설명", EditorStyles.miniBoldLabel);
        y += LINE;
        var descProp = property.FindPropertyRelative("description");
        var descRect = new Rect(x, y, w, LINE * 3);
        EditorGUI.DrawRect(new Rect(x - 1, y - 1, w + 2, LINE * 3 + 2), new Color(0f, 0f, 0f, 0.2f));
        descProp.stringValue = EditorGUI.TextArea(descRect, descProp.stringValue);
        y += LINE * 3 + PAD * 3;

        // 섹션: 타입별 스탯
        DrawSectionLabel(ref y, x, w, GetTypeLabel(skillType) + " 설정", typeColor);

        switch (skillType)
        {
            case SkillType.Atk:
                DrawProp(ref y, x, w, property, "DamageRate", "공격력 배율", 0);
                break;

            case SkillType.Heal:
                DrawProp(ref y, x, w, property, "HealRate", "회복 비율 (MaxHP %)", 0);
                break;

            case SkillType.Buff:
            case SkillType.Debuff:
            case SkillType.Enforce:
                DrawProp(ref y, x, w, property, "EffectType", "효과 종류", 0);
                DrawProp(ref y, x, w, property, "EffectValue", "효과 수치", 1);
                DrawProp(ref y, x, w, property, "EffectDuration", "지속 턴", 0);
                break;
        }

        // 실제 사용된 높이 캐싱 (다음 GetPropertyHeight에서 사용)
        float usedHeight = y - position.y + PAD * 2;
        _heightCache[CacheKey(property)] = usedHeight;

        EditorGUI.EndProperty();
    }

    // ── 헬퍼: 필드 한 줄 그리기 ──────────────────
    private void DrawProp(ref float y, float x, float w,
                          SerializedProperty parent, string propName, string label, int shade)
    {
        var prop = parent.FindPropertyRelative(propName);
        if (prop == null) return;

        // 줄 배경 (홀짝 교차)
        if (shade == 1)
        {
            var rowBg = new Rect(x - INDENT * 0.5f, y, w + INDENT, LINE);
            EditorGUI.DrawRect(rowBg, new Color(0f, 0f, 0f, 0.12f));
        }

        EditorGUI.PropertyField(new Rect(x, y, w, LINE), prop, new GUIContent(label));
        y += LINE + PAD;
    }

    // ── 헬퍼: 섹션 구분선 + 레이블 ───────────────
    private void DrawSectionLabel(ref float y, float x, float w, string title, Color color)
    {
        // 가로선
        EditorGUI.DrawRect(new Rect(x, y, w, 1f), color * 0.7f);
        y += 4f;

        // 레이블
        EditorGUI.LabelField(
            new Rect(x, y, w, LINE),
            title,
            new GUIStyle(EditorStyles.miniLabel)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = color }
            });
        y += LINE;
    }
}
#endif