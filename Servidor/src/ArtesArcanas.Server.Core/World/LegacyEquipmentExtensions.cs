using System;
using System.Runtime.InteropServices;
using ArtesArcanas.Server.Core.Legacy;

namespace ArtesArcanas.Server.Core.World;

internal static class LegacyEquipmentExtensions
{
    private const int SlotSize = 2;

    public static bool TryReadEquipmentSlot(this ref LegacyPlayerSnapshot snapshot, int slot, out LegacyInventorySlot value)
    {
        value = LegacyInventorySlot.Empty;
        if ((uint)slot >= LegacyConstants.EquipmentSlots)
        {
            return false;
        }

        var span = GetEquipmentSpan(ref snapshot);
        var index = slot * SlotSize;
        value = new LegacyInventorySlot(span[index], span[index + 1]);
        return true;
    }

    public static bool TryWriteEquipmentSlot(this ref LegacyPlayerSnapshot snapshot, int slot, LegacyInventorySlot value)
    {
        if ((uint)slot >= LegacyConstants.EquipmentSlots)
        {
            return false;
        }

        var span = GetEquipmentSpan(ref snapshot);
        var index = slot * SlotSize;
        span[index] = value.ItemId;
        span[index + 1] = value.Modifier;
        return true;
    }

    private static unsafe Span<byte> GetEquipmentSpan(ref LegacyPlayerSnapshot snapshot)
    {
        return MemoryMarshal.CreateSpan(ref snapshot.Usando[0], LegacyConstants.EquipmentSlots * SlotSize);
    }
}
