#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Jun;

[CustomPropertyDrawer(typeof(SkillInfo))]
public class SkillInfoDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        float y = position.y;
        float spacing = EditorGUIUtility.standardVerticalSpacing;

        void DrawProp(string propName, string customLabel = null)
        {
            var prop = property.FindPropertyRelative(propName);
            if (prop == null) return;
            float height = EditorGUI.GetPropertyHeight(prop, true);
            var rect = new Rect(position.x, y, position.width, height);
            EditorGUI.PropertyField(rect, prop,
                new GUIContent(string.IsNullOrEmpty(customLabel) ? prop.displayName : customLabel), true);
            y += height + spacing;
        }

        void DrawHeader(string title)
        {
            var rect = new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(rect, title, EditorStyles.boldLabel);
            y += EditorGUIUtility.singleLineHeight + spacing;
        }

        // ── 공통 ──────────────────────────────────────────────────────────
        DrawProp("User", "스킬 주체 (Player / Enemy)");
        DrawProp("Name", "스킬 이름");
        DrawProp("icon", "스킬 아이콘");
        DrawProp("description", "스킬 설명");
        DrawProp("anim", "애니메이션 키");
        DrawProp("Type", "스킬 타입");
        DrawProp("TierData", "스킬트리 연동");

        var userProp = property.FindPropertyRelative("User");
        var typeProp = property.FindPropertyRelative("Type");
        var skillUser = userProp != null ? (SkillUser)userProp.enumValueIndex : SkillUser.Player;
        var skillType = typeProp != null ? (SkillType)typeProp.enumValueIndex : SkillType.Atk;

        if (skillUser == SkillUser.Player)
        {
            // ── 플레이어 스킬 ──────────────────────────────────────────────
            DrawHeader("── 플레이어 스킬 ──");
            DrawProp("TagetNum", "타겟 수");

            switch (skillType)
            {
                case SkillType.Atk:
                    DrawProp("DamageRate", "공격력 배율  ex) 1.2 = 120%");
                    break;
                case SkillType.Heal:
                    DrawProp("HealRate", "최대HP 비율 회복  ex) 0.3 = 30%");
                    break;
                case SkillType.Buff:
                case SkillType.Debuff:
                case SkillType.Enforce:
                    DrawProp("EffectType", "효과 종류");
                    DrawProp("EffectValue", "효과 수치");
                    DrawProp("EffectDuration", "지속 턴수");
                    break;
            }
        }
        else
        {
            // ── 적 스킬 ───────────────────────────────────────────────────
            DrawHeader("── 적 스킬 ──");
            DrawProp("Target", "타겟 종류");

            switch (skillType)
            {
                case SkillType.Atk:
                    DrawProp("DamageMultiplier", "데미지 배율  ex) 1.2 = 120%");
                    break;
                case SkillType.Heal:
                    DrawProp("HealAmount", "고정 회복량");
                    break;
                case SkillType.Buff:
                case SkillType.Debuff:
                case SkillType.Enforce:
                    DrawProp("EffectType", "효과 종류");
                    DrawProp("EffectValue", "효과 수치");
                    DrawProp("EffectDuration", "지속 턴수");
                    break;
            }

            DrawProp("StatusEffects", "부여할 상태이상 목록");
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float total = 0f;
        float spacing = EditorGUIUtility.standardVerticalSpacing;
        float lineH = EditorGUIUtility.singleLineHeight;

        void AddProp(string propName)
        {
            var prop = property.FindPropertyRelative(propName);
            if (prop != null) total += EditorGUI.GetPropertyHeight(prop, true) + spacing;
        }
        void AddHeader() => total += lineH + spacing;

        // 공통
        AddProp("User");
        AddProp("Name");
        AddProp("icon");
        AddProp("description");
        AddProp("anim");
        AddProp("Type");
        AddProp("TierData");

        var userProp = property.FindPropertyRelative("User");
        var typeProp = property.FindPropertyRelative("Type");
        var skillUser = userProp != null ? (SkillUser)userProp.enumValueIndex : SkillUser.Player;
        var skillType = typeProp != null ? (SkillType)typeProp.enumValueIndex : SkillType.Atk;

        AddHeader();

        if (skillUser == SkillUser.Player)
        {
            AddProp("TagetNum");
            switch (skillType)
            {
                case SkillType.Atk: AddProp("DamageRate"); break;
                case SkillType.Heal: AddProp("HealRate"); break;
                case SkillType.Buff:
                case SkillType.Debuff:
                case SkillType.Enforce:
                    AddProp("EffectType");
                    AddProp("EffectValue");
                    AddProp("EffectDuration");
                    break;
            }
        }
        else
        {
            AddProp("Target");
            switch (skillType)
            {
                case SkillType.Atk: AddProp("DamageMultiplier"); break;
                case SkillType.Heal: AddProp("HealAmount"); break;
                case SkillType.Buff:
                case SkillType.Debuff:
                case SkillType.Enforce:
                    AddProp("EffectType");
                    AddProp("EffectValue");
                    AddProp("EffectDuration");
                    break;
            }
            AddProp("StatusEffects");
        }

        return total;
    }
}
#endif