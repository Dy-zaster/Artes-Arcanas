using System;

namespace ArtesArcanas.Server.Core.World;

internal enum LegacyItemStackType
{
    Single = 0,
    Stack60,
    Stack250
}

internal static class LegacyItemStacking
{
    private const byte QuantityMask = 0x3F;
    private const byte DescriptorMask = 0xC0;
    private const byte MaxStack60 = 60;
    private const byte MaxStack250 = 250;
    private const byte FeatherPenId = 141; // ihPlumaMagica

    public static LegacyItemStackType GetStackType(byte itemId)
    {
        if (itemId >= 48 && itemId <= 55)
        {
            return LegacyItemStackType.Stack60;
        }

        if ((itemId >= 4 && itemId <= 15) ||
            itemId == FeatherPenId ||
            (itemId >= 144 && itemId <= 175) ||
            (itemId >= 192 && itemId <= 231))
        {
            return LegacyItemStackType.Stack250;
        }

        return LegacyItemStackType.Single;
    }

    public static byte GetCount(LegacyInventorySlot slot)
    {
        return GetStackType(slot.ItemId) switch
        {
            LegacyItemStackType.Stack60 => ExtractCount(slot.Modifier, QuantityMask),
            LegacyItemStackType.Stack250 => slot.Modifier == 0 ? (byte)1 : slot.Modifier,
            _ => 1
        };
    }

    public static byte GetMaxCount(LegacyInventorySlot slot) =>
        GetStackType(slot.ItemId) switch
        {
            LegacyItemStackType.Stack60 => MaxStack60,
            LegacyItemStackType.Stack250 => MaxStack250,
            _ => 1
        };

    public static bool TrySplit(
        LegacyInventorySlot source,
        byte requested,
        out LegacyInventorySlot extracted,
        out LegacyInventorySlot? remainder,
        out byte actualAmount)
    {
        extracted = source;
        remainder = null;
        actualAmount = 0;

        if (source.IsEmpty)
        {
            return false;
        }

        var available = GetCount(source);
        if (available == 0)
        {
            return false;
        }

        var desired = requested == 0 || requested >= available ? available : requested;
        actualAmount = desired;

        if (desired >= available || GetStackType(source.ItemId) == LegacyItemStackType.Single)
        {
            return true;
        }

        extracted = WithCount(source, desired);
        remainder = WithCount(source, (byte)(available - desired));
        return true;
    }

    public static LegacyInventorySlot WithCount(LegacyInventorySlot slot, byte count)
    {
        if (count == 0)
        {
            return LegacyInventorySlot.Empty;
        }

        return GetStackType(slot.ItemId) switch
        {
            LegacyItemStackType.Stack60 => new LegacyInventorySlot(slot.ItemId, (byte)((slot.Modifier & DescriptorMask) | Math.Min(count, MaxStack60))),
            LegacyItemStackType.Stack250 => new LegacyInventorySlot(slot.ItemId, (byte)Math.Min(count, MaxStack250)),
            _ => slot
        };
    }

    private static byte ExtractCount(byte modifier, byte mask)
    {
        var value = (byte)(modifier & mask);
        return value == 0 ? (byte)1 : value;
    }
}
