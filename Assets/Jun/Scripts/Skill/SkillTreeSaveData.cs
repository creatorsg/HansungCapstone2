namespace Jun
{
    [System.Serializable]
    public class SkillTreeSaveData
    {
        public string characterName;
        public int[] tier2Choice = new int[4] { -1, -1, -1, -1 };
        public int[] tier3Choice = new int[4] { -1, -1, -1, -1 };
    }
}
