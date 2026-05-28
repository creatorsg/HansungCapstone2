// ItemInfoNetworkExtensions.cs
using Mirror;
using Lsy;

namespace Jun
{
    public static class ItemInfoNetworkExtensions
    {
        // ── ConsumableInfo 직렬화 ──────────────────────────────────────────────
        public static void WriteConsumableInfo(this NetworkWriter writer, ConsumableInfo value)
        {
            writer.WriteString(value.Name ?? "");
            writer.WriteInt((int)value.Type);
            writer.WriteFloat(value.HealRate);
            writer.WriteString(value.anim ?? "");
            writer.WriteInt(value.TagetNum);
            writer.WriteString(value.description ?? "");
            writer.WriteInt((int)value.Target);
            writer.WriteInt(value.amount);           
            writer.WriteInt(value.EffectDuration);  
            writer.WriteFloat(value.EffectValue);    
            writer.WriteFloat(value.FixedDamage);    
            // icon(Sprite)은 네트워크 전송 불가 → 클라이언트에서 ItemManager로 별도 조회
        }

        public static ConsumableInfo ReadConsumableInfo(this NetworkReader reader)
        {
            return new ConsumableInfo
            {
                Name        = reader.ReadString(),
                Type        = (ConsumableType)reader.ReadInt(),
                HealRate    = reader.ReadFloat(),
                anim        = reader.ReadString(),
                TagetNum    = reader.ReadInt(),
                description = reader.ReadString(),
                Target      = (TargetType)reader.ReadInt(),
                amount = reader.ReadInt(),       
                EffectDuration = reader.ReadInt(),       
                EffectValue = reader.ReadFloat(),      
                FixedDamage = reader.ReadFloat(),     
                icon        = null  // 클라이언트에서 ItemManager로 별도 조회
            };
        }

        // ── EqpInfo 직렬화 (icon 제외 — Sprite는 네트워크 전송 불가) ──────────
        public static void WriteEqpInfo(this NetworkWriter writer, EqpInfo value)
        {
            if (value == null) { writer.WriteBool(false); return; }
            writer.WriteBool(true);
            writer.WriteString(value.Name ?? "");
            writer.WriteInt(value.EqpId);
            writer.WriteFloat(value.Hp);
            writer.WriteInt(value.San);
            writer.WriteInt(value.Atk);
            writer.WriteInt(value.Def);
            writer.WriteInt(value.Spd);
            writer.WriteInt(value.Crit);
            writer.WriteInt(value.Ctm);
            writer.WriteInt(value.Dodge);
            writer.WriteInt(value.Acc);
            writer.WriteInt(value.Res);
            writer.WriteString(value.anim ?? "");
            writer.WriteString(value.description ?? "");
        }

        public static EqpInfo ReadEqpInfo(this NetworkReader reader)
        {
            if (!reader.ReadBool()) return null;
            return new EqpInfo
            {
                Name        = reader.ReadString(),
                EqpId       = reader.ReadInt(),
                Hp          = reader.ReadFloat(),
                San         = reader.ReadInt(),
                Atk         = reader.ReadInt(),
                Def         = reader.ReadInt(),
                Spd         = reader.ReadInt(),
                Crit        = reader.ReadInt(),
                Ctm         = reader.ReadInt(),
                Dodge       = reader.ReadInt(),
                Acc         = reader.ReadInt(),
                Res         = reader.ReadInt(),
                anim        = reader.ReadString(),
                description = reader.ReadString(),
                icon        = null  // 클라이언트에서 ItemManager로 별도 조회
            };
        }

        // ── InventoryItem 직렬화 ───────────────────────────────────────────────
        public static void WriteInventoryItem(this NetworkWriter writer, InventoryItem value)
        {
            writer.WriteString(value.itemName);
            writer.WriteInt((int)value.Type);
            writer.WriteInt(value.amount);

            // ConsumInfo (소모품)
            bool hasConsumInfo = value.ConsumInfo != null;
            writer.WriteBool(hasConsumInfo);
            if (hasConsumInfo)
                writer.WriteConsumableInfo(value.ConsumInfo);

            // EquipInfo (무기/방어구) — 이전에 누락됐던 부분
            bool hasEquipInfo = value.EquipInfo != null;
            writer.WriteBool(hasEquipInfo);
            if (hasEquipInfo)
                writer.WriteEqpInfo(value.EquipInfo);
        }

        public static InventoryItem ReadInventoryItem(this NetworkReader reader)
        {
            var item = new InventoryItem
            {
                itemName = reader.ReadString(),
                Type     = (ItemType)reader.ReadInt(),
                amount   = reader.ReadInt()
            };

            bool hasConsumInfo = reader.ReadBool();
            if (hasConsumInfo)
                item.ConsumInfo = reader.ReadConsumableInfo();

            // EquipInfo 읽기 — 이전에 누락됐던 부분
            bool hasEquipInfo = reader.ReadBool();
            if (hasEquipInfo)
                item.EquipInfo = reader.ReadEqpInfo();

            return item;
        }
    }
}