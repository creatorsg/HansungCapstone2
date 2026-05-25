using Mirror;
using Jun;

public static class ItemNetworkExtensions
{
    // 1. 장비(EqpInfo) 네트워크 확장
    public static void WriteEqpInfo(this NetworkWriter writer, EqpInfo value)
    {
        // null 체크를 위해 bool 한 칸을 먼저 씁니다. (WriteBool 사용)
        if (value == null)
        {
            writer.WriteBool(false);
            return;
        }
        writer.WriteBool(true);

        writer.WriteString(value.Name ?? "");
        writer.WriteInt(value.EqpId);
        writer.WriteFloat(value.Hp);
        writer.WriteInt(value.Atk);
        // icon(Sprite)은 전송하지 않음으로써 오류 원천 차단
    }

    public static EqpInfo ReadEqpInfo(this NetworkReader reader)
    {
        // 데이터가 있는지 확인 (ReadBool 사용)
        if (!reader.ReadBool()) return null;

        return new EqpInfo
        {
            Name = reader.ReadString(),
            EqpId = reader.ReadInt(),
            Hp = reader.ReadFloat(),
            Atk = reader.ReadInt(),
            icon = null // 클라이언트가 나중에 이름으로 로컬에서 채움
        };
    }

    // 2. 소비품(ConsumableInfo) 네트워크 확장
    public static void WriteConsumableInfo(this NetworkWriter writer, ConsumableInfo value)
    {
        if (value == null)
        {
            writer.WriteBool(false);
            return;
        }
        writer.WriteBool(true);

        writer.WriteString(value.Name ?? "");
        writer.WriteFloat(value.HealRate);
    }

    public static ConsumableInfo ReadConsumableInfo(this NetworkReader reader)
    {
        if (!reader.ReadBool()) return null;

        return new ConsumableInfo
        {
            Name = reader.ReadString(),
            HealRate = reader.ReadFloat(),
            icon = null
        };
    }
}
