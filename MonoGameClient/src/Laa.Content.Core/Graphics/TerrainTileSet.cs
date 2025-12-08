namespace Laa.Content.Core.Graphics;

public sealed record TerrainTileSet(
    string Atlas,
    IReadOnlyDictionary<byte, TerrainTileDefinition> Tiles
);

public sealed record TerrainTileDefinition(
    string Key,
    int FrameIndex
);
