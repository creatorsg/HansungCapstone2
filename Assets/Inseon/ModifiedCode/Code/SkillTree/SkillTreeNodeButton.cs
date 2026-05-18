using Lsy;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 스킬트리에서 노드 1개에 해당하는 버튼입니다.
/// Inspector에서 고정한 위치(skillIndex, level, branchIndex)에 맞는 노드를 현재 캐릭터의 스킬트리에서 찾아 표시합니다.
/// </summary>
public class SkillTreeNodeButton : MonoBehaviour
{
    [Header("노드 위치")]
    [Min(0)] public int skillIndex;
    [Range(1, 3)] public int level = 1;
    [Tooltip("Lv1은 0, Lv2/Lv3는 0(a) 또는 1(b)")]
    [Range(0, 1)] public int branchIndex;

    [Header("UI 참조")]
    [SerializeField] private Button button;
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text displayNameText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private GameObject ownedMark;
    [SerializeField] private GameObject lockedMark;

    [Header("상태별 색상")]
    [SerializeField] private Color colorBuyable = Color.white;
    [SerializeField] private Color colorLocked = new Color(0.4f, 0.4f, 0.4f, 1f);
    [SerializeField] private Color colorOwned = new Color(0.6f, 1f, 0.6f, 1f);
    [SerializeField] private Color colorNoGold = new Color(0.95f, 0.65f, 0.3f, 1f);

    private SkillTreeNodeSO _resolvedNode;
    private CharacterUnit _subscribedUnit;
    private Coroutine _refreshAfterSwitchRoutine;

    void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        if (icon == null) icon = GetComponent<Image>();
        if (button != null) button.onClick.AddListener(OnClick);
    }

    void OnEnable()
    {
        PlayerAccount.OnCharacterSwitched += HandleCharacterSwitched;
        CharacterUnit.OnLocalUnitSpawned += HandleLocalUnitReady;
        ResubscribeToUnit();
        HookGoldChanged();
        RefreshWithNetworkDelay();
    }

    void OnDisable()
    {
        PlayerAccount.OnCharacterSwitched -= HandleCharacterSwitched;
        CharacterUnit.OnLocalUnitSpawned -= HandleLocalUnitReady;
        UnsubscribeFromUnit();
        UnhookGoldChanged();
        if (_refreshAfterSwitchRoutine != null)
        {
            StopCoroutine(_refreshAfterSwitchRoutine);
            _refreshAfterSwitchRoutine = null;
        }
    }

    private void HandleCharacterSwitched()
    {
        UnsubscribeFromUnit();
        ResubscribeToUnit();
        RefreshWithNetworkDelay();
    }

    private void HandleLocalUnitReady(CharacterUnit _)
    {
        UnsubscribeFromUnit();
        ResubscribeToUnit();
        RefreshWithNetworkDelay();
    }

    private void RefreshWithNetworkDelay()
    {
        Refresh();

        if (!isActiveAndEnabled) return;

        if (_refreshAfterSwitchRoutine != null)
            StopCoroutine(_refreshAfterSwitchRoutine);

        _refreshAfterSwitchRoutine = StartCoroutine(RefreshAfterNetworkSync());
    }

    private IEnumerator RefreshAfterNetworkSync()
    {
        yield return null;
        yield return null;
        Refresh();
        _refreshAfterSwitchRoutine = null;
    }

    private void ResubscribeToUnit()
    {
        var unit = ResolveCurrentUnit();
        if (unit == null) return;

        _subscribedUnit = unit;
        CharacterUnit.OnLocalUpgradeStateChanged += Refresh;
    }

    private void UnsubscribeFromUnit()
    {
        if (_subscribedUnit != null)
        {
            CharacterUnit.OnLocalUpgradeStateChanged -= Refresh;
            _subscribedUnit = null;
        }
    }

    private void HookGoldChanged()
    {
        var account = PlayerAccount.LocalInstance;
        if (account == null) return;
        account.OnGoldChanged -= OnGoldChanged;
        account.OnGoldChanged += OnGoldChanged;
    }

    private void UnhookGoldChanged()
    {
        var account = PlayerAccount.LocalInstance;
        if (account == null) return;
        account.OnGoldChanged -= OnGoldChanged;
    }

    private void OnGoldChanged(int _) => Refresh();

    public void Refresh()
    {
        var account = PlayerAccount.LocalInstance;
        var unit = ResolveCurrentUnit();
        if (unit == null)
        {
            Debug.LogWarning($"[SkillTree][Button] ({skillIndex},{level},{branchIndex}) - unit이 null이라 빈 상태로 표시합니다.");
            ApplyEmpty();
            return;
        }

        string code = !string.IsNullOrEmpty(unit.heroCode) ? unit.heroCode : unit.characterName;
        var tree = SkillTreeRegistry.GetTree(code);

        if (tree == null)
        {
            Debug.LogWarning($"[SkillTree][Button] ({skillIndex},{level},{branchIndex}) - 스킬트리를 찾지 못했습니다. code={code}");
        }

        _resolvedNode = tree != null ? tree.FindByPosition(skillIndex, level, branchIndex) : null;

        if (_resolvedNode == null)
        {
            Debug.LogWarning($"[SkillTree][Button] ({skillIndex},{level},{branchIndex}) - 매칭되는 노드가 없습니다. code={code}");
            ApplyEmpty();
            return;
        }

        if (icon != null && _resolvedNode.icon != null) icon.sprite = _resolvedNode.icon;
        if (displayNameText != null) displayNameText.text = _resolvedNode.displayName;
        if (costText != null)
            costText.text = _resolvedNode.unlockCost > 0 ? $"{_resolvedNode.unlockCost}G" : "";

        bool owned = unit.unlockedNodeIds.Contains(_resolvedNode.nodeId);
        bool prereqOk = _resolvedNode.prerequisite == null
                        || unit.unlockedNodeIds.Contains(_resolvedNode.prerequisite.nodeId);
        bool branchTaken = !owned && IsOtherBranchTaken(unit, code);
        bool canAfford = account != null && account.currentGold >= _resolvedNode.unlockCost;

        Color tint;
        bool interactable;
        bool showOwned = false;
        bool showLocked = false;

        if (owned)
        {
            tint = colorOwned;
            interactable = false;
            showOwned = true;
        }
        else if (!prereqOk || branchTaken)
        {
            tint = colorLocked;
            interactable = false;
            showLocked = true;
        }
        else if (!canAfford)
        {
            tint = colorNoGold;
            interactable = true;
        }
        else
        {
            tint = colorBuyable;
            interactable = true;
        }

        if (icon != null) icon.color = tint;
        if (button != null) button.interactable = interactable;
        if (ownedMark != null) ownedMark.SetActive(showOwned);
        if (lockedMark != null) lockedMark.SetActive(showLocked);
    }

    private CharacterUnit ResolveCurrentUnit()
    {
        var account = PlayerAccount.LocalInstance;
        if (account != null && account.currentSelectedCharacter != null)
            return account.currentSelectedCharacter;

        if (CharacterUnit.LocalOwnedUnit != null)
        {
            if (account != null)
                account.currentSelectedCharacter = CharacterUnit.LocalOwnedUnit;
            return CharacterUnit.LocalOwnedUnit;
        }

        foreach (var unit in FindObjectsByType<CharacterUnit>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (unit == null || !unit.isOwned) continue;

            if (account != null)
                account.currentSelectedCharacter = unit;
            return unit;
        }

        return null;
    }

    private bool IsOtherBranchTaken(CharacterUnit unit, string code)
    {
        foreach (string id in unit.unlockedNodeIds)
        {
            var n = SkillTreeRegistry.Find(code, id);
            if (n == null) continue;
            if (n.skillIndex == skillIndex && n.level == level && n.branchIndex != branchIndex)
                return true;
        }
        return false;
    }

    private void ApplyEmpty()
    {
        if (icon != null) { icon.sprite = null; icon.color = colorLocked; }
        if (displayNameText != null) displayNameText.text = "";
        if (costText != null) costText.text = "";
        if (ownedMark != null) ownedMark.SetActive(false);
        if (lockedMark != null) lockedMark.SetActive(false);
        if (button != null) button.interactable = false;
    }

    private void OnClick()
    {
        Debug.Log($"<color=orange>[SkillTree][Button] 클릭 - ({skillIndex},{level},{branchIndex})</color>");
        if (_resolvedNode == null) { Debug.LogWarning("[SkillTree][Button] 클릭 무시: resolvedNode가 null입니다."); return; }
        var account = PlayerAccount.LocalInstance;
        if (account == null) { Debug.LogWarning("[SkillTree][Button] 클릭 무시: LocalInstance가 null입니다."); return; }
        Debug.Log($"[SkillTree][Button] CmdPurchaseTreeNode 호출 - nodeId={_resolvedNode.nodeId}");
        account.CmdPurchaseTreeNode(_resolvedNode.nodeId);
    }
}
