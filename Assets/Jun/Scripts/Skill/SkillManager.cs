namespace Jun
{
    public static class SkillManager
    {
        public static SkillTierData GetCurrentSkill(CharacterSkillSetSO so,
                                                   SkillTreeSaveData save, int skillIndex)
        {
            var skill = so.skills[skillIndex];
            var branch = skill.branch;
            int t2 = save.tier2Choice[skillIndex];
            int t3 = save.tier3Choice[skillIndex];

            if (t2 == 0 && t3 == 0) return branch.optionA_A;
            if (t2 == 0 && t3 == 1) return branch.optionA_B;
            if (t2 == 1 && t3 == 0) return branch.optionB_A;
            if (t2 == 1 && t3 == 1) return branch.optionB_B;
            if (t2 == 0) return branch.optionA;
            if (t2 == 1) return branch.optionB;

            return skill.tier1;
        }
    }
}
