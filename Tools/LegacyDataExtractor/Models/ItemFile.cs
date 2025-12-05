namespace LegacyDataExtractor.Models;

public sealed record ItemFile(
    IReadOnlyList<string> Names,
    IReadOnlyList<ItemDescriptor> Items,
    int Checksum
);
