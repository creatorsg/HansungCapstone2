using UnityEngine;

public enum UpgradeAxis
{
    Base,
    Attack,
    Speed,
    Range
}

[CreateAssetMenu(fileName = "SkillTreeNode", menuName = "Skill Tree/Node")]
public class SkillTreeNodeSO : ScriptableObject
{
    [Header("Identity")]
    public string characterCode;
    public string characterName;
    public int skillNumber;
    public string skillId;
    public string skillName;
    public string nodeId;

    [Header("Display")]
    public string displayName;
    public Sprite icon;
    [TextArea] public string description;

    [Header("Tree Position")]
    [Min(0)] public int skillIndex;
    [Range(1, 3)] public int level = 1;
    [Tooltip("Lv1 uses 0. Lv2/Lv3 use 0 for A and 1 for B.")]
    [Range(0, 1)] public int branchIndex;
    public SkillTreeNodeSO prerequisite;

    [Header("Unlock")]
    [Min(0)] public int unlockCost;

    [Header("Modifier")]
    public UpgradeAxis axis = UpgradeAxis.Base;
    public int damageDelta;
    public int dotDamageDelta;
    public int durationDelta;
    public int addTargetPosition;
    public int maxTargetsDelta;
    public bool grantsExtraAction;

    [Header("Legacy Skill Effect")]
    [Min(0)] public int damage;
    [Min(0)] public int healAmount;
    [Min(0)] public int range = 1;
    [Min(0)] public int durationTurns;
    [Range(0f, 1f)] public float debuffPotency;
}
