using UnityEngine;

[CreateAssetMenu(fileName = "SkillTreeNode", menuName = "Skill Tree/Node")]
public class SkillTreeNodeSO : ScriptableObject
{
    [Header("Identity")]
    public string nodeId;

    [Header("Display")]
    public string displayName;
    public Sprite icon;
    [TextArea] public string description;

    [Header("Tree Position")]
    [Min(0)] public int skillIndex;
    [Range(1, 3)] public int level = 1;
    [Tooltip("Lv1은 0, Lv2/Lv3은 0(a) 또는 1(b)")]
    [Range(0, 1)] public int branchIndex;
    public SkillTreeNodeSO prerequisite;

    [Header("Unlock")]
    [Min(0)] public int unlockCost;

    [Header("Skill Effect (최고 레벨 노드의 값이 적용 — overwrite 방식)")]
    [Min(0)] public int damage;
    [Min(0)] public int healAmount;
    [Min(0)] public int range = 1;
    [Min(0)] public int durationTurns;
    [Range(0f, 1f)] public float debuffPotency;
}
