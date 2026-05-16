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
        float lineHeight = EditorGUIUtility.singleLineHeight + 2;
        float y = position.y;

        // 공통 필드
        EditorGUI.PropertyField(new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight), property.FindPropertyRelative("Name"), new GUIContent("스킬 이름"));
        y += lineHeight;
        EditorGUI.PropertyField(new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight), property.FindPropertyRelative("anim"), new GUIContent("애니메이션"));
        y += lineHeight;
        EditorGUI.PropertyField(new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight), property.FindPropertyRelative("TagetNum"), new GUIContent("타겟 수"));
        y += lineHeight;

        var typeProp = property.FindPropertyRelative("Type");
        EditorGUI.PropertyField(new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight), typeProp, new GUIContent("스킬 타입"));
        y += lineHeight;

        // 타입에 따라 해당 필드만 표시
        SkillType skillType = (SkillType)typeProp.enumValueIndex;
        switch (skillType)
        {
            case SkillType.Atk:
                EditorGUI.PropertyField(new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight), property.FindPropertyRelative("DamageRate"), new GUIContent("데미지 계수"));
                break;

            case SkillType.Heal:
                EditorGUI.PropertyField(new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight), property.FindPropertyRelative("HealRate"), new GUIContent("힐량 계수"));
                break;

            case SkillType.Buff:
            case SkillType.Debuff:
            case SkillType.Enforce:
                EditorGUI.PropertyField(new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight), property.FindPropertyRelative("EffectType"), new GUIContent("효과 종류"));
                y += lineHeight;
                EditorGUI.PropertyField(new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight), property.FindPropertyRelative("EffectValue"), new GUIContent("효과 수치"));
                y += lineHeight;
                EditorGUI.PropertyField(new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight), property.FindPropertyRelative("EffectDuration"), new GUIContent("지속 턴수"));
                break;
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float lineHeight = EditorGUIUtility.singleLineHeight + 2;
        int lines = 4; // 공통 필드 4개 (이름, 애니메이션, 타겟수, 타입)

        var typeProp = property.FindPropertyRelative("Type");
        if (typeProp != null)
        {
            SkillType skillType = (SkillType)typeProp.enumValueIndex;
            switch (skillType)
            {
                case SkillType.Buff:
                case SkillType.Debuff:
                case SkillType.Enforce:
                    lines += 3; // 효과종류 + 수치 + 지속턴수
                    break;
                default:
                    lines += 1; // DamageRate 또는 HealRate
                    break;
            }
        }

        return lineHeight * lines;
    }
}
#endif