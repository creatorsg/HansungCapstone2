using System.Collections.Generic;
using UnityEngine;

public static class SkillTreeRegistry
{
    private const string ResourcesPath = "SkillTree";

    private static readonly Dictionary<string, CharacterSkillTreeSO> _treesByCharacterCode = new Dictionary<string, CharacterSkillTreeSO>();
    private static bool _initialized;

    public static IReadOnlyDictionary<string, CharacterSkillTreeSO> All => _treesByCharacterCode;

    public static void Initialize(IEnumerable<CharacterSkillTreeSO> trees)
    {
        _treesByCharacterCode.Clear();

        if (trees == null)
        {
            _initialized = true;
            return;
        }

        foreach (CharacterSkillTreeSO tree in trees)
        {
            if (tree == null || string.IsNullOrEmpty(tree.characterCode)) continue;

            if (_treesByCharacterCode.ContainsKey(tree.characterCode))
            {
                Debug.LogWarning($"[SkillTreeRegistry] Duplicate characterCode ignored: {tree.characterCode}");
                continue;
            }

            _treesByCharacterCode[tree.characterCode] = tree;
        }

        _initialized = true;
        Debug.Log($"[SkillTreeRegistry] Loaded {_treesByCharacterCode.Count} character skill trees.");
    }

    public static void EnsureLoaded()
    {
        if (_initialized) return;

        CharacterSkillTreeSO[] trees = Resources.LoadAll<CharacterSkillTreeSO>(ResourcesPath);
        Initialize(trees);
    }

    public static CharacterSkillTreeSO GetTree(string characterCode)
    {
        EnsureLoaded();
        if (string.IsNullOrEmpty(characterCode)) return null;

        _treesByCharacterCode.TryGetValue(characterCode, out CharacterSkillTreeSO tree);
        return tree;
    }

    public static SkillTreeNodeSO Find(string characterCode, string nodeId)
    {
        CharacterSkillTreeSO tree = GetTree(characterCode);
        return tree != null ? tree.FindNode(nodeId) : null;
    }

    public static bool TryFind(string characterCode, string nodeId, out SkillTreeNodeSO node)
    {
        node = Find(characterCode, nodeId);
        return node != null;
    }
}
