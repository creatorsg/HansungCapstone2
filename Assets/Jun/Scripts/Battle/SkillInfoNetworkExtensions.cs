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
            icon        = null  
        };
    }
}