// PlayerInfoNetworkExtensions.cs
// Jun.PlayerInfo SyncVar 커스텀 직렬화기.
// Mirror Weaver 자동 생성은 List<SkillInfo> 등 중첩 class 타입을 불완전하게 처리하므로
// WritePlayerInfo / ReadPlayerInfo 를 명시적으로 정의합니다.

using Mirror;
using Jun;
using System.Collections.Generic;

public static class PlayerInfoNetworkExtensions
{
    // ── ActiveStatus ──────────────────────────────────────────────────
    public static void WriteActiveStatus(this NetworkWriter writer, ActiveStatus value)
    {
        if (value == null) { writer.WriteBool(false); return; }
        writer.WriteBool(true);
        writer.WriteInt((int)value.Type);
        writer.WriteInt(value.RemainingTurns);
        writer.WriteInt(value.Value);
    }
    public static ActiveStatus ReadActiveStatus(this NetworkReader reader)
    {
        if (!reader.ReadBool()) return null;
        return new ActiveStatus
        {
            Type           = (StatusType)reader.ReadInt(),
            RemainingTurns = reader.ReadInt(),
            Value          = reader.ReadInt(),
        };
    }

    // ── PlayerInfo ────────────────────────────────────────────────────
    public static void WritePlayerInfo(this NetworkWriter writer, Jun.PlayerInfo value)
    {
        if (value == null) { writer.WriteBool(false); return; }
        writer.WriteBool(true);

        writer.WriteInt(value.Id);
        writer.WriteString(value.Name  ?? "");
        writer.WriteString(value.Type  ?? "");

        // Skills
        int skillCount = value.Skills?.Count ?? 0;
        writer.WriteInt(skillCount);
        if (value.Skills != null)
            foreach (var s in value.Skills)
                writer.WriteSkillInfo(s);

        // Items (미장착 인벤토리) — Lsy.InventoryItem
        int itemCount = value.Items?.Count ?? 0;
        writer.WriteInt(itemCount);
        if (value.Items != null)
            foreach (var i in value.Items)
                writer.WriteInventoryItem(i);

        // Expendables (장착 소모품)
        int expCount = value.Expendables?.Count ?? 0;
        writer.WriteInt(expCount);
        if (value.Expendables != null)
            foreach (var e in value.Expendables)
                writer.WriteConsumableInfo(e);

        writer.WriteInt(value.Lvl);
        writer.WriteInt(value.Exp);
        writer.WriteInt(value.Gold);

        writer.WriteFloat(value.Hp);
        writer.WriteFloat(value.MaxHp);
        writer.WriteInt(value.San);
        writer.WriteInt(value.MaxSan);

        writer.WriteInt(value.Atk);
        writer.WriteInt(value.Def);
        writer.WriteInt(value.Spd);
        writer.WriteInt(value.Crit);
        writer.WriteInt(value.Ctm);
        writer.WriteInt(value.Dodge);
        writer.WriteInt(value.Acc);
        writer.WriteInt(value.Res);

        writer.WriteEqpInfo(value.Weapon);
        writer.WriteEqpInfo(value.Armor);

        writer.WriteInt(value.Trk1);
        writer.WriteInt(value.Trk2);
        writer.WriteInt(value.UniqueTraitLv);

        // Statuses
        int statusCount = value.Statuses?.Count ?? 0;
        writer.WriteInt(statusCount);
        if (value.Statuses != null)
            foreach (var s in value.Statuses)
                writer.WriteActiveStatus(s);
    }

    public static Jun.PlayerInfo ReadPlayerInfo(this NetworkReader reader)
    {
        if (!reader.ReadBool()) return null;

        var info = new Jun.PlayerInfo
        {
            Id   = reader.ReadInt(),
            Name = reader.ReadString(),
            Type = reader.ReadString(),
        };

        int skillCount = reader.ReadInt();
        info.Skills = new List<Jun.SkillInfo>(skillCount);
        for (int i = 0; i < skillCount; i++)
            info.Skills.Add(reader.ReadSkillInfo());

        int itemCount = reader.ReadInt();
        info.Items = new List<Lsy.InventoryItem>(itemCount);
        for (int i = 0; i < itemCount; i++)
            info.Items.Add(reader.ReadInventoryItem());

        int expCount = reader.ReadInt();
        info.Expendables = new List<Jun.ConsumableInfo>(expCount);
        for (int i = 0; i < expCount; i++)
            info.Expendables.Add(reader.ReadConsumableInfo());

        info.Lvl  = reader.ReadInt();
        info.Exp  = reader.ReadInt();
        info.Gold = reader.ReadInt();

        info.Hp     = reader.ReadFloat();
        info.MaxHp  = reader.ReadFloat();
        info.San    = reader.ReadInt();
        info.MaxSan = reader.ReadInt();

        info.Atk   = reader.ReadInt();
        info.Def   = reader.ReadInt();
        info.Spd   = reader.ReadInt();
        info.Crit  = reader.ReadInt();
        info.Ctm   = reader.ReadInt();
        info.Dodge = reader.ReadInt();
        info.Acc   = reader.ReadInt();
        info.Res   = reader.ReadInt();

        info.Weapon = reader.ReadEqpInfo();
        info.Armor  = reader.ReadEqpInfo();

        info.Trk1          = reader.ReadInt();
        info.Trk2          = reader.ReadInt();
        info.UniqueTraitLv = reader.ReadInt();

        int statusCount = reader.ReadInt();
        info.Statuses = new List<Jun.ActiveStatus>(statusCount);
        for (int i = 0; i < statusCount; i++)
            info.Statuses.Add(reader.ReadActiveStatus());

        return info;
    }
}
