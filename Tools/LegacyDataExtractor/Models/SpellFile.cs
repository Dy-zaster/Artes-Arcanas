namespace LegacyDataExtractor.Models;

public sealed record SpellFile(
    IReadOnlyList<string> Names,
    IReadOnlyList<SpellDescriptor> Spells,
    int Checksum
);
