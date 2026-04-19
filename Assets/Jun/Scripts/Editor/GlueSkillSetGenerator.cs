using UnityEngine;
using UnityEditor;
using System.IO;

namespace Jun.EditorTools
{
    public static class GlueSkillSetGenerator
    {
        private const string OutputFolder = "Assets/Jun/SO/SkillSets";
        private const string AssetPath = OutputFolder + "/SkillSet_Glue.asset";

        [MenuItem("Rose/Generate/Glue SkillSet")]
        public static void Generate()
        {
            if (!Directory.Exists(OutputFolder))
                Directory.CreateDirectory(OutputFolder);

            var so = ScriptableObject.CreateInstance<CharacterSkillSetSO>();
            so.characterName = "Glue";
            so.skills = new SkillData[4]
            {
                BuildSkill1_BodyGuard(),
                BuildSkill2_Cover(),
                BuildSkill3_TrenchRush(),
                BuildSkill4_HeadButt(),
            };

            AssetDatabase.CreateAsset(so, AssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = so;

            Debug.Log($"[GlueSkillSetGenerator] 생성 완료: {AssetPath}");
        }

        private static SkillTierData Tier(string skillName, string desc, int gold) =>
            new SkillTierData { skillName = skillName, description = desc, goldCost = gold, statValues = new float[0] };

        // 스킬 1: 육탄 방어
        private static SkillData BuildSkill1_BodyGuard() => new SkillData
        {
            tier1 = Tier("육탄 방어", "아군 1명의 다음 공격을 대신 맞아줌. 피해 경감 없음.", 0),
            branch = new SkillBranchData
            {
                optionA   = Tier("육탄 방어 (강화)", "피해 20% 경감 추가", 300),
                optionB   = Tier("연속 방어", "같은 턴 내 2번까지 대신 맞을 수 있음", 300),
                optionA_A = Tier("강철 방패", "경감 35%로 증가", 600),
                optionA_B = Tier("반격 방어", "대신 맞을 때 반격 20% 확률", 600),
                optionB_A = Tier("군중 방어", "아군 전체에 1턴간 방어 부여", 600),
                optionB_B = Tier("희생 돌격", "대신 맞을 때 상대에게 출혈 부여", 600),
            }
        };

        // 스킬 2: 엄호
        private static SkillData BuildSkill2_Cover() => new SkillData
        {
            tier1 = Tier("엄호", "자신 앞에 엄폐물 설치. 자신과 후방 전체 다음 공격 무효.", 0),
            branch = new SkillBranchData
            {
                optionA   = Tier("강화 엄폐", "엄폐물 2턴 지속", 300),
                optionB   = Tier("광역 엄폐", "엄폐 범위 +1열 확장", 300),
                optionA_A = Tier("철벽 엄폐", "3턴 지속, 공격 무효 2회로 증가", 600),
                optionA_B = Tier("역습 진지", "엄폐 중 피해 받을 시 반격 발동", 600),
                optionB_A = Tier("전방 돌출", "엄폐 중 아군 ACC +15% 부여", 600),
                optionB_B = Tier("참호 요새", "엄폐 중 아군 PROT +20 부여", 600),
            }
        };

        // 스킬 3: 참호 돌격
        private static SkillData BuildSkill3_TrenchRush() => new SkillData
        {
            tier1 = Tier("참호 돌격", "넉백으로 밀려났을 때 1열로 즉시 복귀. 복귀 시 피해 경감 없음.", 0),
            branch = new SkillBranchData
            {
                optionA   = Tier("분노 돌격", "복귀 시 ATK +25% 1턴 버프", 300),
                optionB   = Tier("방패 돌격", "복귀 시 주변 적에게 소량 피해", 300),
                optionA_A = Tier("광기 돌격", "ATK 버프 +40%, SAN -5 (자기 손상)", 600),
                optionA_B = Tier("전술 돌격", "복귀 시 아군 전체 SPD +2 버프", 600),
                optionB_A = Tier("충격 돌격", "복귀 시 적 1열 스턴 확률 30%", 600),
                optionB_B = Tier("돌진 쇄도", "복귀 피해 2배, 쿨타임 +1턴", 600),
            }
        };

        // 스킬 4: 얼굴 박치기
        private static SkillData BuildSkill4_HeadButt() => new SkillData
        {
            tier1 = Tier("얼굴 박치기", "얼굴로 적 1명 타격. 2턴간 스턴(행동 불가).", 0),
            branch = new SkillBranchData
            {
                optionA   = Tier("박치기 강타", "스턴 3턴으로 연장", 300),
                optionB   = Tier("박치기 광역", "인접한 적 1명에게도 1턴 스턴 전이", 300),
                optionA_A = Tier("절명 박치기", "스턴 + 출혈 동시 부여", 600),
                optionA_B = Tier("뇌진탕", "스턴 해제 후 ACC -20% 2턴 디버프", 600),
                optionB_A = Tier("연쇄 박치기", "적 3명까지 연쇄 스턴 가능 (확률 감소)", 600),
                optionB_B = Tier("위협 박치기", "스턴 + SAN 피해 추가", 600),
            }
        };
    }
}
