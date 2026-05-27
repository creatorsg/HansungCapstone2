using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

[CustomEditor(typeof(UniqueTraitSO))]
public class UniqueTraitSOEditor : Editor
{
    private static readonly (string field, string label)[] Stats =
    {
        ("hp",    "HP"),        ("san",   "정신력"),
        ("atk",   "공격력"),   ("def",   "방어력"),
        ("spd",   "속도"),      ("crit",  "치명타%"),
        ("ctm",   "치명타배율%"), ("dodge", "회피%"),
        ("acc",   "명중%"),     ("res",   "저항%"),
    };

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // ── 기본 정보 ──────────────────────────────────────────────
        SectionHeader("기본 정보");
        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(serializedObject.FindProperty("itemName"),    new GUIContent("아이템 이름"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("icon"),        new GUIContent("아이콘"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("description"), new GUIContent("설명"));
        EditorGUI.indentLevel--;

        EditorGUILayout.Space(8);

        // ── 단계별 ─────────────────────────────────────────────────
        DrawLevel("▶ 1단계", "level1");
        EditorGUILayout.Space(4);
        DrawLevel("▶ 2단계", "level2");
        EditorGUILayout.Space(4);
        DrawLevel("▶ 3단계 (최대)", "level3");

        EditorGUILayout.Space(10);

        // ── 단계 요약 ──────────────────────────────────────────────
        DrawSummary();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawLevel(string title, string propName)
    {
        SerializedProperty lvProp = serializedObject.FindProperty(propName);

        // 헤더 + 강화 금액 (한 줄)
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel, GUILayout.Width(150));
        EditorGUILayout.LabelField("강화 금액", GUILayout.Width(56));
        SerializedProperty priceProp = lvProp.FindPropertyRelative("price");
        priceProp.intValue = EditorGUILayout.IntField(priceProp.intValue, GUILayout.Width(70));
        EditorGUILayout.LabelField("G", GUILayout.Width(16));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(2);

        // 능력치 — 2열로 배치
        EditorGUI.indentLevel++;
        for (int i = 0; i < Stats.Length; i += 2)
        {
            EditorGUILayout.BeginHorizontal();
            DrawStatCell(lvProp, Stats[i].field, Stats[i].label);
            GUILayout.Space(8);
            if (i + 1 < Stats.Length)
                DrawStatCell(lvProp, Stats[i + 1].field, Stats[i + 1].label);
            EditorGUILayout.EndHorizontal();
        }
        EditorGUI.indentLevel--;
    }

    private static void DrawStatCell(SerializedProperty lvProp, string fieldName, string label)
    {
        SerializedProperty sp = lvProp.FindPropertyRelative(fieldName);

        Color prev = GUI.contentColor;
        if (sp.intValue == 0)
            GUI.contentColor = new Color(0.5f, 0.5f, 0.5f, 1f); // 0이면 회색

        EditorGUILayout.LabelField(label, GUILayout.Width(76));
        sp.intValue = EditorGUILayout.IntField(sp.intValue, GUILayout.Width(52));

        GUI.contentColor = prev;
    }

    private static void SectionHeader(string title)
    {
        EditorGUILayout.Space(2);
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        Rect r = GUILayoutUtility.GetLastRect();
        r.y      += EditorGUIUtility.singleLineHeight - 1;
        r.height  = 1f;
        EditorGUI.DrawRect(r, new Color(0.4f, 0.4f, 0.4f, 0.6f));
        EditorGUILayout.Space(2);
    }

    private void DrawSummary()
    {
        SectionHeader("단계별 요약");
        var trait = (UniqueTraitSO)target;

        for (int lv = 1; lv <= UniqueTraitSO.MaxLevel; lv++)
        {
            TraitLevelData d = trait.GetLevel(lv);
            var parts = new List<string>();
            if (d.hp    != 0) parts.Add($"HP+{d.hp}");
            if (d.san   != 0) parts.Add($"정신+{d.san}");
            if (d.atk   != 0) parts.Add($"공+{d.atk}");
            if (d.def   != 0) parts.Add($"방+{d.def}");
            if (d.spd   != 0) parts.Add($"속+{d.spd}");
            if (d.crit  != 0) parts.Add($"치{d.crit}%");
            if (d.ctm   != 0) parts.Add($"배율+{d.ctm}%");
            if (d.dodge != 0) parts.Add($"회피+{d.dodge}%");
            if (d.acc   != 0) parts.Add($"명중+{d.acc}%");
            if (d.res   != 0) parts.Add($"저항+{d.res}%");

            string stats = parts.Count > 0 ? string.Join("  ", parts) : "(미설정)";
            EditorGUILayout.LabelField($"  Lv{lv}  {d.price}G  →  {stats}", EditorStyles.miniLabel);
        }
    }
}
