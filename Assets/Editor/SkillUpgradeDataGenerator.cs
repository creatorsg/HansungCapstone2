#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using Lsy;
using Jun;

public static class SkillUpgradeDataGenerator
{
    private const string OutDir = "Assets/Resources/SkillUpgrades";

    [MenuItem("Tools/Project Rose/Generate Skill Upgrade SOs")]
    public static void Generate()
    {
        if (!Directory.Exists(OutDir))
            Directory.CreateDirectory(OutDir);

        CreateC001();
        CreateC002();
        CreateC003();
        CreateC004();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[SkillUpgradeDataGenerator] 완료");
    }

    private static void CreateC001()
    {
        var d = New("글루 스킬 강화", "C001");

        // 0 육탄 방어 (Buff/DefUp40, dur1) -> 턴
        d.nodes.Add(N(0, 2, "강화 방어막", dur: 2, price: 1000, npc: 1));
        d.nodes.Add(N(0, 3, "강철 방어막", dur: 3, price: 2000, npc: 2));
        d.nodes.Add(N(0, 4, "불괴의 방벽", dur: 4, price: 3000, npc: 3));

        // 1 엄호는 BattleLogic 특수분기(blockNext bool)라 EffectDuration이 적용되지 않아 생략.

        // 2 참호 돌격 (Atk 0.9) -> 딜량
        d.nodes.Add(N(2, 2, "분노 돌격", dmg: 1.1f, price: 1000, npc: 1));
        d.nodes.Add(N(2, 3, "광기 돌격", dmg: 1.3f, price: 2000, npc: 2));
        d.nodes.Add(N(2, 4, "결전 돌격", dmg: 1.5f, price: 3000, npc: 3));

        // 3 얼굴 박치기 (Atk 0.9 + Stunned dur2) -> 턴(스턴)
        d.nodes.Add(N(3, 2, "강타 박치기", dur: 3, price: 1000, npc: 1));
        d.nodes.Add(N(3, 3, "뇌진탕", dur: 4, price: 2000, npc: 2));
        d.nodes.Add(N(3, 4, "두개골 분쇄", dur: 5, price: 3000, npc: 3));

        Save(d, "C001_Glue");
    }

    private static void CreateC002()
    {
        var d = New("칼쟁이 스킬 강화", "C002");

        // 0 나이프 투척 (Atk 1.3) -> 딜량
        d.nodes.Add(N(0, 2, "예리한 투척", dmg: 1.5f, price: 1000, npc: 1));
        d.nodes.Add(N(0, 3, "관통 투척", dmg: 1.7f, price: 2000, npc: 2));
        d.nodes.Add(N(0, 4, "처형 투척", dmg: 1.9f, price: 3000, npc: 3));

        // 1 급소 노리기 (Atk 1.1) -> 딜량
        d.nodes.Add(N(1, 2, "정밀 급소", dmg: 1.3f, price: 1000, npc: 1));
        d.nodes.Add(N(1, 3, "치명 급소", dmg: 1.5f, price: 2000, npc: 2));
        d.nodes.Add(N(1, 4, "절명 급소", dmg: 1.7f, price: 3000, npc: 3));

        // 2 연속 투척 (Atk 0.8, TargetNum2) -> 딜량
        d.nodes.Add(N(2, 2, "쾌속 연사", dmg: 1.0f, price: 1000, npc: 1));
        d.nodes.Add(N(2, 3, "난무 연사", dmg: 1.2f, price: 2000, npc: 2));
        d.nodes.Add(N(2, 4, "폭풍 연사", dmg: 1.4f, price: 3000, npc: 3));

        // 3 무기 회수는 현재 강화 3차원에 적용할 전투 수치가 없어 생략.

        Save(d, "C002_Knife");
    }

    private static void CreateC003()
    {
        var d = New("피리부는 사나이 스킬 강화", "C003");

        // 0 구슬리는 선율 (Debuff/Stunned dur1, TargetNum2) -> 턴
        d.nodes.Add(N(0, 2, "최면 선율", dur: 2, price: 1000, npc: 1));
        d.nodes.Add(N(0, 3, "몽환 선율", dur: 3, price: 2000, npc: 2));
        d.nodes.Add(N(0, 4, "심연 선율", dur: 4, price: 3000, npc: 3));

        // 1 행진곡 (Buff/AllAllies SpdUp5 dur1) -> 턴
        d.nodes.Add(N(1, 2, "고양 행진곡", dur: 2, price: 1000, npc: 1));
        d.nodes.Add(N(1, 3, "질주 행진곡", dur: 3, price: 2000, npc: 2));
        d.nodes.Add(N(1, 4, "전쟁 행진곡", dur: 4, price: 3000, npc: 3));

        // 2 불협화음 (Debuff/AccDown20 dur2, TargetNum2) -> 턴
        d.nodes.Add(N(2, 2, "소음 공해", dur: 3, price: 1000, npc: 1));
        d.nodes.Add(N(2, 3, "이명", dur: 4, price: 2000, npc: 2));
        d.nodes.Add(N(2, 4, "광기의 소음", dur: 5, price: 3000, npc: 3));

        // 3 피리부는 사나이 (Debuff/Stunned dur2, SingleEnemy) -> 턴, Lv4 적수
        d.nodes.Add(N(3, 2, "유혹의 가락", dur: 3, price: 1000, npc: 1));
        d.nodes.Add(N(3, 3, "지배의 가락", dur: 4, price: 2000, npc: 2));
        d.nodes.Add(N(3, 4, "파멸의 합주", widen: true, price: 4000, npc: 3));

        Save(d, "C003_Piper");
    }

    private static void CreateC004()
    {
        var d = New("핀 스킬 강화", "C004");

        // 0 실험 투척 (Buff/Bleeding3 dur2, SingleEnemy) -> 턴(출혈)
        d.nodes.Add(N(0, 2, "독성 강화", dur: 3, price: 1000, npc: 1));
        d.nodes.Add(N(0, 3, "맹독 배합", dur: 4, price: 2000, npc: 2));
        d.nodes.Add(N(0, 4, "괴저 시약", dur: 5, price: 3000, npc: 3));

        // 1 버프제 주입 (Buff/AtkUp30 dur1) -> 턴
        d.nodes.Add(N(1, 2, "지속 주입", dur: 2, price: 1000, npc: 1));
        d.nodes.Add(N(1, 3, "과다 주입", dur: 3, price: 2000, npc: 2));
        d.nodes.Add(N(1, 4, "광폭 주입", dur: 4, price: 3000, npc: 3));

        // 2 폭발 실험 (Atk/AllEnemies 0.8, TargetNum2) -> 딜량
        d.nodes.Add(N(2, 2, "강력 폭발", dmg: 1.0f, price: 1000, npc: 1));
        d.nodes.Add(N(2, 3, "연쇄 폭발", dmg: 1.2f, price: 2000, npc: 2));
        d.nodes.Add(N(2, 4, "대폭발", dmg: 1.4f, price: 3000, npc: 3));

        // 3 자가 실험 (Enforce/DodgeUp10 dur2) -> 턴
        d.nodes.Add(N(3, 2, "안정 시약", dur: 3, price: 1000, npc: 1));
        d.nodes.Add(N(3, 3, "각성 시약", dur: 4, price: 2000, npc: 2));
        d.nodes.Add(N(3, 4, "초월 시약", dur: 5, price: 3000, npc: 3));

        Save(d, "C004_Finn");
    }

    private static SkillUpgradeData New(string group, string code)
    {
        var data = ScriptableObject.CreateInstance<SkillUpgradeData>();
        data.groupName = group;
        data.characterCode = code;
        return data;
    }

    private static SkillUpgradeNode N(
        int idx,
        int lv,
        string name,
        float dmg = 0f,
        int dur = 0,
        bool widen = false,
        int price = 0,
        int npc = 1)
    {
        return new SkillUpgradeNode
        {
            skillId = $"{idx}_{lv}",
            skillName = name,
            skillIndex = idx,
            targetLevel = lv,
            DamageRate = dmg,
            HealRate = 0f,
            EffectValue = 0f,
            EffectDuration = dur,
            useTargetOverride = widen,
            Target = widen ? TargetType.AllEnemies : default,
            useEffectOverride = false,
            price = price,
            requiredNpcLevel = npc
        };
    }

    private static void Save(SkillUpgradeData data, string file)
    {
        string path = $"{OutDir}/{file}.asset";
        if (File.Exists(path))
            AssetDatabase.DeleteAsset(path);

        AssetDatabase.CreateAsset(data, path);
        Debug.Log($"  {path} (노드 {data.nodes.Count})");
    }
}
#endif
