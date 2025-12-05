namespace LegacyDataExtractor.Models;

public sealed record CommerceFile(
    int Version,
    IReadOnlyList<CommerceInventory> Inventories
);
