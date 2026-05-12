using UnityEngine;

[CreateAssetMenu(fileName = "CharacterSkillTree", menuName = "Skill Tree/Character Tree")]
public class CharacterSkillTreeSO : ScriptableObject
{
    public string characterCode;
    public SkillTreeNodeSO[] allNodes;

    public SkillTreeNodeSO FindNode(string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId) || allNodes == null) return null;

        foreach (SkillTreeNodeSO node in allNodes)
            if (node != null && node.nodeId == nodeId)
                return node;

        return null;
    }

    /// <summary>
    /// UI 버튼이 자기 위치(skillIndex, level, branchIndex)에 해당하는 노드를 찾을 때 사용.
    /// </summary>
    public SkillTreeNodeSO FindByPosition(int skillIndex, int level, int branchIndex)
    {
        if (allNodes == null) return null;

        foreach (SkillTreeNodeSO node in allNodes)
        {
            if (node == null) continue;
            if (node.skillIndex != skillIndex) continue;
            if (node.level != level) continue;
            if (node.branchIndex != branchIndex) continue;
            return node;
        }
        return null;
    }
}
