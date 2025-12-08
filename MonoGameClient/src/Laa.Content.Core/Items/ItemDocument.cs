namespace Laa.Content.Core.Items;

public sealed record ItemDocument(
    IReadOnlyList<string> Names,
    IReadOnlyList<ItemDescriptor> Items,
    int Checksum
);
