using ArtesArcanas.Server.Core.Legacy;

namespace ArtesArcanas.Server.Core.World;

internal static class LegacyInventoryPacketFactory
{
    private const int EquipmentOpcodeBase = 208;
    private const int InventoryOpcodeBase = 216; // 8 equipment packets reserved at 208..215

    public static byte[]? BuildInventorySlotPacket(int slot, byte itemId, byte modifier)
    {
        if ((uint)slot >= LegacyConstants.InventoryArtifactSlots)
        {
            return null;
        }

        var opcode = (byte)(InventoryOpcodeBase + slot);
        return new[] { opcode, itemId, modifier };
    }

    public static byte[]? BuildEquipmentSlotPacket(int slot, byte itemId, byte modifier)
    {
        if ((uint)slot >= LegacyConstants.EquipmentSlots)
        {
            return null;
        }

        var opcode = (byte)(EquipmentOpcodeBase + slot);
        return new[] { opcode, itemId, modifier };
    }
}
