namespace Laa.Content.Core.Spells;

public sealed record SpellDocument(
    IReadOnlyList<string> Names,
    IReadOnlyList<SpellDescriptor> Spells,
    int Checksum
);
