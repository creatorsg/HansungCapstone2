using UnityEngine;

namespace Jun
{
    [CreateAssetMenu(fileName = "SkillSet_", menuName = "Rose/SkillSet")]
    public class CharacterSkillSetSO : ScriptableObject
    {
        public string characterName;
        public SkillData[] skills = new SkillData[4];

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (skills == null || skills.Length != 4)
            {
                Debug.LogError($"[{name}] skills 배열은 정확히 4개여야 합니다.", this);
                return;
            }

            for (int i = 0; i < 4; i++)
            {
                var s = skills[i];
                if (s == null) { Debug.LogError($"[{name}] skills[{i}]가 비어있습니다.", this); continue; }
                if (s.tier1 == null) Debug.LogError($"[{name}] skills[{i}].tier1이 비어있습니다.", this);
                if (s.branch == null) Debug.LogError($"[{name}] skills[{i}].branch가 비어있습니다.", this);
            }
        }
#endif
    }
}
