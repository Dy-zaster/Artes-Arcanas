namespace ArtesArcanas.Server.Core.World;

public readonly record struct LegacyInventorySlot(byte ItemId, byte Modifier)
{
    public bool IsEmpty => ItemId == 0 && Modifier == 0;

    public static LegacyInventorySlot Empty => new(0, 0);
}
