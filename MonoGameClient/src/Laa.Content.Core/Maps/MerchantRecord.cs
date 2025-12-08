using Laa.Content.Core.Inventory;

namespace Laa.Content.Core.Maps;

public sealed record MerchantRecord(
    byte Type,
    byte X,
    byte Y,
    byte MonsterCode,
    IReadOnlyList<ArtefactSlot> Inventory,
    IReadOnlyList<byte> Inflation,
    string Text
);
