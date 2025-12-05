namespace LegacyDataExtractor.Models;

public sealed record GraphicFile(IReadOnlyList<string> Names, IReadOnlyList<GraphicDescriptor> Descriptors, int Checksum);
