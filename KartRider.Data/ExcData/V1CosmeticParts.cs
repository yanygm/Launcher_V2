using KartRider.IO.Packet;

namespace ExcData
{
    // P3553 compares inventory grade/value before sending PqEquipXPartsItem.
    // Its comparison uses category - 63, while cosmetics actually occupy slots 4/5
    // (see the client's category-to-slot mapping). Zero metadata therefore matches
    // an empty slot, and equal values across cosmetic categories can also collide.
    // Use distinct cosmetic identity ranges; visual resources still use the real ID.
    public static class V1CosmeticParts
    {
        public static bool IsCosmetic(int category) => category == 68 || category == 69;

        public static byte GetGrade(short id) => (byte)(id == 0 ? 0 : 4);

        public static short GetValue(short category, short id) =>
            id == 0 ? (short)0 : checked((short)(category * 256 + id));

        public static void WriteSlot(OutPacket packet, short category, short id)
        {
            packet.WriteShort(id);
            packet.WriteByte(GetGrade(id));
            packet.WriteShort(GetValue(category, id));
        }
    }
}
