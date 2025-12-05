namespace LegacyDataExtractor.Models;

public sealed record MerchantRecord(
    byte Type,
    byte X,
    byte Y,
    byte MonsterCode,
    IReadOnlyList<ArtefactSlot> Inventory,
    IReadOnlyList<byte> Inflation,
    string Text
);
