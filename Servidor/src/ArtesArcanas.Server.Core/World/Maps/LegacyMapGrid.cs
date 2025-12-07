using System;

namespace ArtesArcanas.Server.Core.World.Maps;

public sealed class LegacyMapGrid
{
    public const int TileWidth = 64;
    public const int TileHeight = 64;
    public const int ExpandedWidth = TileWidth * 4;
    public const int ExpandedHeight = TileHeight * 4;

    private readonly LegacyTerrainFlags[,] _terrain = new LegacyTerrainFlags[ExpandedWidth, ExpandedHeight];

    private LegacyMapGrid()
    {
    }

    public static LegacyMapGrid FromBaseTerrain(ReadOnlySpan<byte> baseTerrain)
    {
        if (baseTerrain.Length != TileWidth * TileHeight)
        {
            throw new ArgumentException("Mapa base incompleto (64x64).", nameof(baseTerrain));
        }

        var grid = new LegacyMapGrid();
        for (var tileY = 0; tileY < TileHeight; tileY++)
        {
            for (var tileX = 0; tileX < TileWidth; tileX++)
            {
                var value = baseTerrain[tileY * TileWidth + tileX];
                var flags = ClassifyTerrain(value);
                var originX = tileX * 4;
                var originY = tileY * 4;
                for (var y = 0; y < 4; y++)
                {
                    for (var x = 0; x < 4; x++)
                    {
                        grid._terrain[originX + x, originY + y] = flags;
                    }
                }
            }
        }

        return grid;
    }

    public LegacyTerrainFlags GetTerrain(int x, int y)
    {
        if ((uint)x >= ExpandedWidth || (uint)y >= ExpandedHeight)
        {
            return LegacyTerrainFlags.None;
        }

        return _terrain[x, y];
    }

    private static LegacyTerrainFlags ClassifyTerrain(byte code) =>
        code switch
        {
            0 => LegacyTerrainFlags.None,
            3 => LegacyTerrainFlags.Solid | LegacyTerrainFlags.Wilderness,
            >= 13 and <= 15 => LegacyTerrainFlags.Wilderness | LegacyTerrainFlags.Covered,
            >= 17 and <= 24 => LegacyTerrainFlags.Solid | LegacyTerrainFlags.Soft,
            >= 25 and <= 27 => LegacyTerrainFlags.Solid,
            28 => LegacyTerrainFlags.None,
            29 => LegacyTerrainFlags.Fire,
            30 => LegacyTerrainFlags.Water | LegacyTerrainFlags.Wilderness,
            31 => LegacyTerrainFlags.Water,
            _ => LegacyTerrainFlags.Wilderness
        };
}
