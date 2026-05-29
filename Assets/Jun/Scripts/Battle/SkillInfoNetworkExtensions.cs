using Mirror;
using Jun;

public static class SkillInfoNetworkExtensions
{
    public static void WriteSkillInfo(this NetworkWriter writer, SkillInfo value)
    {
        writer.WriteString(value.Name);
        writer.WriteInt((int)value.Type);
        writer.WriteString(value.anim ?? "");
        writer.WriteInt(value.TagetNum);
        writer.WriteString(value.description ?? "");
        writer.WriteInt((int)value.Target);
        writer.WriteFloat(value.DamageRate);
        writer.WriteFloat(value.HealRate);
        writer.WriteInt((int)value.EffectType);
        writer.WriteFloat(value.EffectValue);
        writer.WriteInt(value.EffectDuration);
    }

    public static SkillInfo ReadSkillInfo(this NetworkReader reader)
    {
        return new SkillInfo
        {
            Name        = reader.ReadString(),
            Type        = (SkillType)reader.ReadInt(),
            anim        = reader.ReadString(),
            TagetNum    = reader.ReadInt(),
            description = reader.ReadString(),
            Target         = (TargetType)reader.ReadInt(),
            DamageRate     = reader.ReadFloat(),
            HealRate       = reader.ReadFloat(),
            EffectType     = (EffectType)reader.ReadInt(),
            EffectValue    = reader.ReadFloat(),
            EffectDuration = reader.ReadInt(),
            icon        = null
        };
    }
}