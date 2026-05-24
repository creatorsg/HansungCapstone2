// ItemInfoNetworkExtensions.cs
using Mirror;
using Lsy;

namespace Jun
{
    public static class ItemInfoNetworkExtensions
    {
        // ── ConsumableInfo 직렬화 (기존 코드 이름만 수정) ──────────────
        public static void WriteConsumableInfo(this NetworkWriter writer, ConsumableInfo value)
        {
            writer.WriteString(value.Name);
            writer.WriteInt((int)value.Type);       // ConsumableType
            writer.WriteFloat(value.HealRate);
            writer.WriteString(value.anim ?? "");
            writer.WriteInt(value.TagetNum);
            writer.WriteString(value.description ?? "");
            writer.WriteInt((int)value.Target);
            // icon(Sprite)은 네트워크 전송 불가 → null로 처리, 클라이언트에서 로컬 조회
        }

        public static ConsumableInfo ReadConsumableInfo(this NetworkReader reader)
        {
            return new ConsumableInfo
            {
                Name = reader.ReadString(),
                Type = (ConsumableType)reader.ReadInt(),
                HealRate = reader.ReadFloat(),
                anim = reader.ReadString(),
                TagetNum = reader.ReadInt(),
                description = reader.ReadString(),
                Target = (TargetType)reader.ReadInt(),
                icon = null  // 클라이언트에서 ItemData ScriptableObject로 로컬 조회
            };
        }

        // ── InventoryItem 직렬화 (신규 추가) ───────────────────────────
        public static void WriteInventoryItem(this NetworkWriter writer, InventoryItem value)
        {
            writer.WriteString(value.itemName);
            writer.WriteInt((int)value.Type);   // Lsy.ItemType (Equipment/Consumable)
            writer.WriteInt(value.amount);

            // ConsumableInfo는 Consumable 타입일 때만 존재
            bool hasInfo = value.ConsumInfo != null;
            writer.WriteBool(hasInfo);
            if (hasInfo)
                writer.WriteConsumableInfo(value.ConsumInfo);
        }

        public static InventoryItem ReadInventoryItem(this NetworkReader reader)
        {
            var item = new InventoryItem
            {
                itemName = reader.ReadString(),
                Type = (ItemType)reader.ReadInt(),
                amount = reader.ReadInt()
            };

            bool hasInfo = reader.ReadBool();
            if (hasInfo)
                item.ConsumInfo = reader.ReadConsumableInfo();

            return item;
        }
    }
}