using UnityEngine;

/// <summary>
/// 캐릭터 고유 특성 ScriptableObject.
///
/// ▶ 사용법 (Unity Editor)
///   - Project 창 우클릭 → Create → Items → UniqueTraitSO
///   - 각 캐릭터의 CharacterCard(또는 캐릭터 데이터)에 연결하세요.
///   - 1~3단계마다 강화 금액(Price)과 능력치가 독립적으로 설정됩니다.
///   - 0인 항목은 적용되지 않습니다.
///
/// ▶ ItemSO와 다른 점
///   - 아이템 종류(Category) 없음 — 캐릭터 고유 특성이므로 고정
///   - 3단계 강화 구조 — 단계별로 Price와 능력치가 달라집니다
/// </summary>
[CreateAssetMenu(fileName = "NewUniqueTrait", menuName = "Items/UniqueTraitSO")]
public class UniqueTraitSO : ScriptableObject
{
    // ─── 기본 정보 ────────────────────────────────────────────────
    [Header("기본 정보")]
    public string itemName;
    public Sprite icon;
    [TextArea(2, 4)]
    public string description;

    // ─── 단계별 데이터 ────────────────────────────────────────────
    [Header("1단계")]
    public TraitLevelData level1;

    [Header("2단계")]
    public TraitLevelData level2;

    [Header("3단계")]
    public TraitLevelData level3;

    // ─────────────────────────────────────────────────────────────
    //  헬퍼
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// 지정한 단계(1~3)의 데이터를 반환합니다.
    /// 범위를 벗어나면 가장 가까운 유효 단계로 클램프합니다.
    /// </summary>
    public TraitLevelData GetLevel(int level)
    {
        return level switch
        {
            1 => level1,
            2 => level2,
            3 => level3,
            _ => level < 1 ? level1 : level3,
        };
    }

    /// <summary>현재 단계에서 다음 단계로 강화할 때의 금액을 반환합니다.</summary>
    public int GetUpgradePrice(int currentLevel)
    {
        // currentLevel 0 → 1단계 강화 비용, 1 → 2단계, 2 → 3단계
        return GetLevel(currentLevel + 1).price;
    }

    /// <summary>최대 단계 수 (항상 3)</summary>
    public const int MaxLevel = 3;
}

// ─── 단계 데이터 구조체 ──────────────────────────────────────────
[System.Serializable]
public struct TraitLevelData
{
    [Tooltip("이 단계로 강화할 때 필요한 금액")]
    public int price;

    [Header("능력치 변화량 (0이면 미적용)")]
    [Tooltip("최대 HP 증가량")]
    public int hp;
    [Tooltip("최대 정신력 증가량")]
    public int san;
    [Tooltip("공격력")]
    public int atk;
    [Tooltip("방어력")]
    public int def;
    [Tooltip("속도 (턴 순서)")]
    public int spd;
    [Tooltip("치명타 확률 (%)")]
    public int crit;
    [Tooltip("치명타 배율 추가 (%)")]
    public int ctm;
    [Tooltip("회피율 (%)")]
    public int dodge;
    [Tooltip("명중률 (%)")]
    public int acc;
    [Tooltip("상태이상 저항 (%)")]
    public int res;
}
