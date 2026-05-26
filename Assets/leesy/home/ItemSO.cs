using UnityEngine;
using Jun;
using Lsy;

/// <summary>
/// 무기 / 방어구 / 소모품을 하나의 ScriptableObject로 통합 관리합니다.
///
/// ▶ 사용법 (Unity Editor)
///   - Project 창 우클릭 → Create → Items → ItemSO
///   - Category를 선택하면 Inspector가 해당 타입에 맞게 자동 정리됩니다.
///   - 능력치 필드는 장착(Weapon/Armor) 또는 사용(Consumable) 시 캐릭터에 합산됩니다.
///   - 0인 항목은 적용되지 않으므로 필요한 항목만 채우면 됩니다.
/// </summary>
[CreateAssetMenu(fileName = "NewItem", menuName = "Items/ItemSO")]
public class ItemSO : ScriptableObject
{
    // ─── 기본 정보 ────────────────────────────────────────────────
    [Header("기본 정보")]
    public string       itemName;
    public Sprite       icon;
    [TextArea(2, 4)]
    public string       description;
    public int          price;

    // ─── 아이템 종류 ──────────────────────────────────────────────
    [Header("아이템 종류")]
    public ItemCategory category;

    // ─── 소모품 전용 ──────────────────────────────────────────────
    [Header("소모품 전용 설정 (Consumable만 해당)")]
    [Tooltip("Add = 회복·버프 / Subtraction = 피해·디버프")]
    public ConsumableOperation operation;

    [Tooltip("적용 대상: Ally = 아군 / Enemy = 적군")]
    public ConsumableTargetSide targetSide;

    [Tooltip("단일(Single) 또는 전체(All) / 자기 자신(Self)")]
    public ConsumableTargetScope targetScope;

    [Tooltip("동시에 선택할 대상 수 (Single이면 1, All이면 0)")]
    public int targetCount = 1;

    [Tooltip("사용 시 재생할 애니메이션 이름")]
    public string animName;

    // ─── 능력치 ───────────────────────────────────────────────────
    [Tooltip("HP 변화량 (Weapon/Armor는 최대 HP 증가, Consumable은 현재 HP 회복/피해)")]
    public int hp;
    public int   san;
    public int   atk;
    public int   def;
    public int   spd;
    public int   crit;
    public int   ctm;
    public int   dodge;
    public int   acc;
    public int   res;

    // ─────────────────────────────────────────────────────────────
    //  기존 시스템 변환 (EqpInfo / ConsumableInfo / InventoryItem)
    // ─────────────────────────────────────────────────────────────

    /// <summary>EqpInfo 변환 (Weapon / Armor용)</summary>
    public EqpInfo ToEqpInfo()
    {
        return new EqpInfo
        {
            Name        = itemName,
            icon        = icon,
            description = description,
            Hp          = hp,
            San         = san,
            Atk         = atk,
            Def         = def,
            Spd         = spd,
            Crit        = crit,
            Ctm         = ctm,
            Dodge       = dodge,
            Acc         = acc,
            Res         = res,
        };
    }

    /// <summary>ConsumableInfo 변환 (Consumable용)</summary>
    public ConsumableInfo ToConsumableInfo()
    {
        // Add/Subtraction 부호 적용
        float sign = operation == ConsumableOperation.Add ? 1f : -1f;

        // HP가 있으면 HPHeal, San이 있으면 SanHeal, 둘 다 없으면 기본 HPHeal
        ConsumableType type = ConsumableType.HPHeal;
        float healRate = 0f;

        if (hp != 0)
        {
            type     = ConsumableType.HPHeal;
            healRate = sign * Mathf.Abs(hp) / 100f;
        }
        else if (san != 0)
        {
            type     = ConsumableType.SanHeal;
            healRate = sign * Mathf.Abs(san) / 100f;
        }

        return new ConsumableInfo
        {
            Name        = itemName,
            icon        = icon,
            description = description,
            Type        = type,
            HealRate    = healRate,
            TagetNum    = targetCount,
            Target      = ResolveTargetType(),
            anim        = animName,
        };
    }

    /// <summary>InventoryItem 변환 (myInventory / EquipmentSlot 직접 사용)</summary>
    public InventoryItem ToInventoryItem(int amount = 1)
    {
        ItemType invType = category switch
        {
            ItemCategory.Weapon     => ItemType.Weapon,
            ItemCategory.Armor      => ItemType.Armor,
            _                       => ItemType.Consumable,
        };

        return new InventoryItem
        {
            itemName   = itemName,
            Type       = invType,
            EquipInfo  = (category != ItemCategory.Consumable) ? ToEqpInfo()       : null,
            ConsumInfo = (category == ItemCategory.Consumable)  ? ToConsumableInfo(): null,
            amount     = amount,
        };
    }

    /// <summary>
    /// 아이템 능력치를 PlayerInfo에 직접 합산합니다.
    /// Consumable: operation(Add/Sub)에 따라 부호 적용
    /// Weapon/Armor: 항상 양수 합산
    /// </summary>
    public void ApplyStatsTo(ref Jun.PlayerInfo info)
    {
        float sign = (category == ItemCategory.Consumable && operation == ConsumableOperation.Subtraction)
                     ? -1f : 1f;

        info.Hp    += hp    * sign;
        info.San   += (int)(san  * sign);
        info.Atk   += (int)(atk  * sign);
        info.Def   += (int)(def  * sign);
        info.Spd   += (int)(spd  * sign);
        info.Crit  += (int)(crit * sign);
        info.Ctm   += (int)(ctm  * sign);
        info.Dodge += (int)(dodge* sign);
        info.Acc   += (int)(acc  * sign);
        info.Res   += (int)(res  * sign);
    }

    // ─────────────────────────────────────────────────────────────
    //  내부 헬퍼
    // ─────────────────────────────────────────────────────────────

    private TargetType ResolveTargetType()
    {
        if (targetScope == ConsumableTargetScope.Self)   return TargetType.Self;
        if (targetSide  == ConsumableTargetSide.Enemy)
            return targetScope == ConsumableTargetScope.All ? TargetType.AllEnemies : TargetType.SingleEnemy;
        // Ally
        return targetScope == ConsumableTargetScope.All ? TargetType.AllAllies : TargetType.SingleAlly;
    }
}

// ─── 보조 열거형 ──────────────────────────────────────────────────

public enum ItemCategory
{
    Weapon,
    Armor,
    Consumable,
}

public enum ConsumableOperation
{
    Add,        // 회복 / 버프
    Subtraction // 피해 / 디버프
}

public enum ConsumableTargetSide
{
    Ally,
    Enemy,
}

public enum ConsumableTargetScope
{
    Single,   // 단일 대상
    All,      // 전체 대상
    Self,     // 자기 자신
}
