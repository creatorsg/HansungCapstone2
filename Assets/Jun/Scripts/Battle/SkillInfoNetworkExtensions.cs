using Mirror;
using Jun;
using System.Collections.Generic;

public static class SkillInfoNetworkExtensions
{
    // ── StatusApply 직렬화 ────────────────────────────────────────
    public static void WriteStatusApply(this NetworkWriter writer, StatusApply value)
    {
        writer.WriteInt((int)value.Type);
        writer.WriteFloat(value.Chance);
        writer.WriteInt(value.Duration);
        writer.WriteInt(value.Value);
    }

    public static StatusApply ReadStatusApply(this NetworkReader reader)
    {
        return new StatusApply
        {
            Type     = (StatusType)reader.ReadInt(),
            Chance   = reader.ReadFloat(),
            Duration = reader.ReadInt(),
            Value    = reader.ReadInt(),
        };
    }

    // ── SkillInfo 직렬화 ──────────────────────────────────────────
    public static void WriteSkillInfo(this NetworkWriter writer, SkillInfo value)
    {
        writer.WriteString(value.Name);
        writer.WriteInt((int)value.User);
        writer.WriteInt((int)value.Type);
        writer.WriteString(value.anim ?? "");
        writer.WriteInt(value.TagetNum);
        writer.WriteString(value.description ?? "");
        writer.WriteInt((int)value.Target);
        writer.WriteFloat(value.DamageRate);
        writer.WriteFloat(value.DamageMultiplier);
        writer.WriteFloat(value.HealRate);
        writer.WriteInt(value.HealAmount);
        writer.WriteInt((int)value.EffectType);
        writer.WriteFloat(value.EffectValue);
        writer.WriteInt(value.EffectDuration);

        // StatusEffects (List<StatusApply>)
        int count = value.StatusEffects?.Count ?? 0;
        writer.WriteInt(count);
        if (value.StatusEffects != null)
            foreach (var s in value.StatusEffects)
                writer.WriteStatusApply(s);
    }

    public static SkillInfo ReadSkillInfo(this NetworkReader reader)
    {
        var skill = new SkillInfo
        {
            Name             = reader.ReadString(),
            User             = (SkillUser)reader.ReadInt(),
            Type             = (SkillType)reader.ReadInt(),
            anim             = reader.ReadString(),
            TagetNum         = reader.ReadInt(),
            description      = reader.ReadString(),
            Target           = (TargetType)reader.ReadInt(),
            DamageRate       = reader.ReadFloat(),
            DamageMultiplier = reader.ReadFloat(),
            HealRate         = reader.ReadFloat(),
            HealAmount       = reader.ReadInt(),
            EffectType       = (EffectType)reader.ReadInt(),
            EffectValue      = reader.ReadFloat(),
            EffectDuration   = reader.ReadInt(),
            icon             = null,
        };

        int count = reader.ReadInt();
        skill.StatusEffects = new List<StatusApply>(count);
        for (int i = 0; i < count; i++)
            skill.StatusEffects.Add(reader.ReadStatusApply());

        return skill;
    }
}
