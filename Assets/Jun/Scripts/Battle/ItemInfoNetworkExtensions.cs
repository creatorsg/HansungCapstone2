// ItemInfoNetworkExtensions.cs
using Mirror;

namespace Jun
{
    public static class ItemInfoNetworkExtensions
    {
        public static void WriteItemInfo(this NetworkWriter writer, ItemInfo value)
        {
            writer.WriteString(value.Name);
            writer.WriteInt((int)value.Type);
            writer.WriteFloat(value.HealRate);
            writer.WriteString(value.anim);
            // icon은 SkillInfo와 동일하게 전송 제외
        }

        public static ItemInfo ReadItemInfo(this NetworkReader reader)
        {
            return new ItemInfo
            {
                Name = reader.ReadString(),
                Type = (ItemType)reader.ReadInt(),
                HealRate = reader.ReadFloat(),
                anim = reader.ReadString(),
                icon = null
            };
        }
    }
}