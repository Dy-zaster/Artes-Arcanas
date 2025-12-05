using System.Collections.Generic;

namespace Laa.Content.Core.Graphics;

public sealed record GraphicDocument(
    IReadOnlyList<string> Names,
    IReadOnlyList<GraphicDescriptor> Descriptors,
    int Checksum,
    TerrainTileSet? TerrainTileSet = null
);
