namespace LegacyDataExtractor.Models;

public sealed record CommerceInventory(
    IReadOnlyList<ArtefactSlot> Items
);
