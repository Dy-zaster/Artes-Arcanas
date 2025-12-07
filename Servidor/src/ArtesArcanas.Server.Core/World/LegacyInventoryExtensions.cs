using System.Runtime.InteropServices;
using ArtesArcanas.Server.Core.Legacy;

namespace ArtesArcanas.Server.Core.World;

internal static class LegacyInventoryExtensions
{
    private const int SlotSize = 2;

    public static bool TryReadSlot(this ref LegacyPlayerSnapshot snapshot, int slot, out LegacyInventorySlot value)
    {
        value = LegacyInventorySlot.Empty;
        if ((uint)slot >= LegacyConstants.InventoryArtifactSlots)
        {
            return false;
        }

        var span = GetInventorySpan(ref snapshot);
        var index = slot * SlotSize;
        value = new LegacyInventorySlot(span[index], span[index + 1]);
        return true;
    }

    public static bool TryClearSlot(this ref LegacyPlayerSnapshot snapshot, int slot, out LegacyInventorySlot removed)
    {
        removed = LegacyInventorySlot.Empty;
        if (!snapshot.TryReadSlot(slot, out removed))
        {
            return false;
        }

        var span = GetInventorySpan(ref snapshot);
        var index = slot * SlotSize;
        span[index] = 0;
        span[index + 1] = 0;
        return true;
    }

    public static bool TryWriteSlot(this ref LegacyPlayerSnapshot snapshot, int slot, LegacyInventorySlot value)
    {
        if ((uint)slot >= LegacyConstants.InventoryArtifactSlots)
        {
            return false;
        }

        var span = GetInventorySpan(ref snapshot);
        var index = slot * SlotSize;
        span[index] = value.ItemId;
        span[index + 1] = value.Modifier;
        return true;
    }

    public static bool TryFindEmptySlot(this ref LegacyPlayerSnapshot snapshot, out int slot)
    {
        var span = GetInventorySpan(ref snapshot);
        for (var index = 0; index < LegacyConstants.InventoryArtifactSlots; index++)
        {
            var offset = index * SlotSize;
            if (span[offset] == 0 && span[offset + 1] == 0)
            {
                slot = index;
                return true;
            }
        }

        slot = -1;
        return false;
    }

    private static unsafe Span<byte> GetInventorySpan(ref LegacyPlayerSnapshot snapshot)
    {
        return MemoryMarshal.CreateSpan(ref snapshot.Inventario[0], LegacyConstants.InventoryArtifactSlots * SlotSize);
    }
}
