#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// ItemSO 전용 커스텀 인스펙터.
/// - category가 Consumable일 때만 소모품 전용 설정을 표시합니다.
/// - 능력치 헤더에 Add(+) / Subtraction(-) 여부를 실시간으로 반영합니다.
/// - 0이 아닌 능력치는 하이라이트로 표시합니다.
/// </summary>
[CustomEditor(typeof(ItemSO))]
public class ItemSOEditor : Editor
{
    // 색상 상수
    private static readonly Color ColorWeapon     = new Color(1.0f, 0.85f, 0.4f, 0.25f);  // 황금
    private static readonly Color ColorArmor      = new Color(0.4f, 0.8f,  1.0f, 0.25f);  // 하늘
    private static readonly Color ColorConsumable = new Color(0.5f, 1.0f,  0.5f, 0.25f);  // 녹색
    private static readonly Color ColorStat       = new Color(0.9f, 0.9f,  1.0f, 0.35f);  // 연보라

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        ItemSO item = (ItemSO)target;

        // ── 기본 정보 ─────────────────────────────────────────────
        DrawSectionHeader("기본 정보", GetCategoryColor(item.category));

        EditorGUILayout.PropertyField(serializedObject.FindProperty("itemName"),    new GUIContent("아이템 이름"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("icon"),        new GUIContent("아이콘 이미지"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("description"), new GUIContent("설명"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("price"),        new GUIContent("가격"));

        EditorGUILayout.Space(6);

        // ── 종류 선택 ─────────────────────────────────────────────
        DrawSectionHeader("아이템 종류", GetCategoryColor(item.category));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("category"), new GUIContent("Category"));

        EditorGUILayout.Space(6);

        // ── 소모품 전용 ───────────────────────────────────────────
        if (item.category == ItemCategory.Consumable)
        {
            DrawSectionHeader("소모품 설정", ColorConsumable);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("operation"),
                new GUIContent("효과 방향", "Add = 회복·버프  /  Subtraction = 피해·디버프"));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("targetSide"),
                new GUIContent("대상 진영", "Ally = 아군  /  Enemy = 적군"));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("targetScope"),
                new GUIContent("대상 범위", "Single = 단일  /  All = 전체  /  Self = 자기 자신"));

            // Self일 때는 targetCount 숨김
            if (item.targetScope != ConsumableTargetScope.Self)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("targetCount"),
                    new GUIContent("대상 수"));
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("animName"),
                new GUIContent("애니메이션 이름"));

            EditorGUILayout.Space(6);
        }

        // ── 능력치 ────────────────────────────────────────────────
        string statHeader = item.category switch
        {
            ItemCategory.Consumable when item.operation == ConsumableOperation.Add
                => "능력치  [ + 적용 ]",
            ItemCategory.Consumable
                => "능력치  [ - 적용 ]",
            ItemCategory.Weapon
                => "능력치  [ 장착 시 합산 ]",
            _   => "능력치  [ 장착 시 합산 ]",
        };

        DrawSectionHeader(statHeader, ColorStat);

        DrawStatField("hp",    "HP",         item.hp    != 0);
        DrawStatField("san",   "정신력",     item.san   != 0);
        DrawStatField("atk",   "공격력",     item.atk   != 0);
        DrawStatField("def",   "방어력",     item.def   != 0);
        DrawStatField("spd",   "속도",       item.spd   != 0);
        DrawStatField("crit",  "치명타 확률",item.crit  != 0);
        DrawStatField("ctm",   "치명타 배율",item.ctm   != 0);
        DrawStatField("dodge", "회피율",     item.dodge != 0);
        DrawStatField("acc",   "명중률",     item.acc   != 0);
        DrawStatField("res",   "상태이상 저항", item.res != 0);

        EditorGUILayout.Space(4);

        // ── 미리보기 요약 ─────────────────────────────────────────
        DrawPreviewSummary(item);

        serializedObject.ApplyModifiedProperties();
    }

    // ─────────────────────────────────────────────────────────────
    //  헬퍼
    // ─────────────────────────────────────────────────────────────

    private void DrawSectionHeader(string title, Color bgColor)
    {
        EditorGUILayout.Space(2);
        var rect = EditorGUILayout.GetControlRect(false, 22);
        EditorGUI.DrawRect(rect, bgColor);
        var style = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize  = 12,
            alignment = TextAnchor.MiddleLeft,
        };
        EditorGUI.LabelField(new Rect(rect.x + 6, rect.y, rect.width, rect.height), title, style);
        EditorGUILayout.Space(2);
    }

    private void DrawStatField(string propName, string label, bool highlighted)
    {
        var prop = serializedObject.FindProperty(propName);
        if (prop == null) return;

        if (highlighted)
        {
            var rect = EditorGUILayout.GetControlRect();
            EditorGUI.DrawRect(rect, ColorStat);   // rect 딱 맞게 — 넘치지 않음
            EditorGUI.PropertyField(rect, prop, new GUIContent(label));
        }
        else
        {
            EditorGUILayout.PropertyField(prop, new GUIContent(label));
        }
    }

    /// <summary>적용될 효과를 텍스트로 요약해서 보여줍니다.</summary>
    private void DrawPreviewSummary(ItemSO item)
    {
        var sb = new System.Text.StringBuilder();

        // Consumable: operation으로 부호 결정 / Weapon·Armor: 입력값 부호 그대로
        bool isSubtraction = item.category == ItemCategory.Consumable &&
                             item.operation == ConsumableOperation.Subtraction;

        string Fmt(int v)  => isSubtraction ? $"-{Mathf.Abs(v)}"  : (v >= 0 ? $"+{v}"  : $"{v}");
        string FmtH(int v) => isSubtraction ? $"-{Mathf.Abs(v)}"  : (v >= 0 ? $"+{v}"  : $"{v}");

        if (item.hp    != 0) sb.AppendLine($"  HP {FmtH(item.hp)}");
        if (item.san   != 0) sb.AppendLine($"  정신력 {Fmt(item.san)}");
        if (item.atk   != 0) sb.AppendLine($"  공격력 {Fmt(item.atk)}");
        if (item.def   != 0) sb.AppendLine($"  방어력 {Fmt(item.def)}");
        if (item.spd   != 0) sb.AppendLine($"  속도 {Fmt(item.spd)}");
        if (item.crit  != 0) sb.AppendLine($"  치명타 확률 {Fmt(item.crit)}%");
        if (item.ctm   != 0) sb.AppendLine($"  치명타 배율 {Fmt(item.ctm)}%");
        if (item.dodge != 0) sb.AppendLine($"  회피율 {Fmt(item.dodge)}%");
        if (item.acc   != 0) sb.AppendLine($"  명중률 {Fmt(item.acc)}%");
        if (item.res   != 0) sb.AppendLine($"  저항 {Fmt(item.res)}%");

        if (sb.Length == 0) return;

        EditorGUILayout.Space(4);
        DrawSectionHeader("적용 효과 미리보기", new Color(0.3f, 0.3f, 0.3f, 0.2f));

        if (item.category == ItemCategory.Consumable)
        {
            string side  = item.targetSide  == ConsumableTargetSide.Enemy ? "적군" : "아군";
            string scope = item.targetScope switch
            {
                ConsumableTargetScope.All  => "전체",
                ConsumableTargetScope.Self => "자신",
                _                          => $"{item.targetCount}명",
            };
            EditorGUILayout.HelpBox($"[{side} {scope}에 적용]\n{sb}", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox($"[장착 시 합산]\n{sb}", MessageType.Info);
        }
    }

    private static Color GetCategoryColor(ItemCategory cat) => cat switch
    {
        ItemCategory.Weapon     => ColorWeapon,
        ItemCategory.Armor      => ColorArmor,
        ItemCategory.Consumable => ColorConsumable,
        _                       => Color.clear,
    };
}
#endif
