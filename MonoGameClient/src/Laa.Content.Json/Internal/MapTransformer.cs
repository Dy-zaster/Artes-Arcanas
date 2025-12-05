using System;
using System.Collections.Generic;
using System.Linq;
using Laa.Content.Core.Maps;

namespace Laa.Content.Json.Internal;

/// <summary>
/// Normalizes map data to the runtime shape/orientation expected by the MonoGame client.
/// Expands the legacy 64×64 compressed terrain into a 256×256 grid and rotates coordinates
/// so the visual corners match the original Delphi client (bottom-left no longer maps to top-right).
/// </summary>
internal static class MapTransformer
{
    private const int CompressedSize = 64;
    private const int ExpandedSize = 256;
    private const int ExpansionFactor = ExpandedSize / CompressedSize;
    private const bool ApplyRotation180 = false;
    private const bool ApplyTranspose = true;
    private const bool ApplyEntityRotation180 = false;
    private const bool ApplyEntityTranspose = false;

    public static MapDocument Normalize(MapDocument document)
    {
        if (document is null) throw new ArgumentNullException(nameof(document));

        var orientedTerrain = ExpandTerrain(document.Terrain);
        if (ApplyRotation180)
        {
            orientedTerrain = RotateTerrain180(orientedTerrain);
        }

        if (ApplyTranspose)
        {
            orientedTerrain = TransposeTerrain(orientedTerrain);
        }

        var width = orientedTerrain.Count > 0 ? orientedTerrain[0].Count : 0;
        var height = orientedTerrain.Count;

        var graphics = TransformGraphics(document.Graphics, width, height);
        var sensors = TransformSensors(document.Sensors, width, height);
        var nests = TransformNests(document.Nests, width, height);
        var merchants = TransformMerchants(document.Merchants, width, height);
        var extended = TransformExtendedData(document.ExtendedData, width, height);

        return new MapDocument(
            document.Header,
            orientedTerrain,
            graphics,
            sensors,
            nests,
            merchants,
            extended);
    }

    private static IReadOnlyList<IReadOnlyList<byte>> TransposeTerrain(IReadOnlyList<IReadOnlyList<byte>> terrain)
    {
        if (terrain.Count == 0)
        {
            return terrain;
        }

        var originalHeight = terrain.Count;
        var originalWidth = terrain[0].Count;
        if (terrain.Any(row => row.Count != originalWidth))
        {
            return terrain;
        }

        var transposed = new List<IReadOnlyList<byte>>(originalWidth);
        for (var y = 0; y < originalWidth; y++)
        {
            var row = new byte[originalHeight];
            for (var x = 0; x < originalHeight; x++)
            {
                row[x] = terrain[x][y];
            }
            transposed.Add(Array.AsReadOnly(row));
        }

        return transposed;
    }

    private static IReadOnlyList<IReadOnlyList<byte>> ExpandTerrain(IReadOnlyList<IReadOnlyList<byte>> source)
    {
        if (source is null || source.Count == 0)
        {
            return Array.Empty<IReadOnlyList<byte>>();
        }

        var height = source.Count;
        var width = source[0].Count;

        // Already expanded or an unexpected shape.
        if (height == ExpandedSize && width == ExpandedSize)
        {
            return source;
        }

        if (height != CompressedSize || width != CompressedSize || source.Any(row => row.Count != width))
        {
            return source;
        }

        static int GetTerrain(IReadOnlyList<IReadOnlyList<byte>> src, int x, int y)
        {
            if (x < 0) x = 0;
            else if (x >= CompressedSize) x = CompressedSize - 1;

            if (y < 0) y = 0;
            else if (y >= CompressedSize) y = CompressedSize - 1;

            return src[y][x];
        }

        var expandedGrid = new byte[ExpandedSize, ExpandedSize];

        for (var j = 0; j < CompressedSize; j++)
        {
            for (var i = 0; i < CompressedSize; i++)
            {
                var terrain = GetTerrain(source, i, j);

                var n = terrain;
                if ((GetTerrain(source, i - 1, j) == GetTerrain(source, i - 1, j - 1) &&
                     GetTerrain(source, i, j - 1) != GetTerrain(source, i, j)) ||
                    (GetTerrain(source, i, j - 1) == GetTerrain(source, i - 1, j - 1) &&
                     GetTerrain(source, i - 1, j) != GetTerrain(source, i, j)))
                {
                    // NoEsPiso returns true in the final client, so we always adopt the corner when conditions match.
                    n = GetTerrain(source, i - 1, j - 1);
                }
                else if (GetTerrain(source, i - 1, j) == GetTerrain(source, i, j - 1) &&
                         (GetTerrain(source, i, j) < GetTerrain(source, i - 1, j) ||
                          GetTerrain(source, i, j) != GetTerrain(source, i - 1, j - 1)))
                {
                    n = GetTerrain(source, i - 1, j);
                }

                var e = terrain;
                if ((GetTerrain(source, i + 1, j) == GetTerrain(source, i + 1, j - 1) &&
                     GetTerrain(source, i, j - 1) != GetTerrain(source, i, j)) ||
                    (GetTerrain(source, i, j - 1) == GetTerrain(source, i + 1, j - 1) &&
                     GetTerrain(source, i + 1, j) != GetTerrain(source, i, j)))
                {
                    e = GetTerrain(source, i + 1, j - 1);
                }
                else if (GetTerrain(source, i + 1, j) == GetTerrain(source, i, j - 1) &&
                         (GetTerrain(source, i, j) < GetTerrain(source, i + 1, j) ||
                          GetTerrain(source, i, j) != GetTerrain(source, i + 1, j - 1)))
                {
                    e = GetTerrain(source, i + 1, j);
                }

                var s = terrain;
                if ((GetTerrain(source, i - 1, j) == GetTerrain(source, i - 1, j + 1) &&
                     GetTerrain(source, i, j + 1) != GetTerrain(source, i, j)) ||
                    (GetTerrain(source, i, j + 1) == GetTerrain(source, i - 1, j + 1) &&
                     GetTerrain(source, i - 1, j) != GetTerrain(source, i, j)))
                {
                    s = GetTerrain(source, i - 1, j + 1);
                }
                else if (GetTerrain(source, i - 1, j) == GetTerrain(source, i, j + 1) &&
                         (GetTerrain(source, i, j) < GetTerrain(source, i - 1, j) ||
                          GetTerrain(source, i, j) != GetTerrain(source, i - 1, j + 1)))
                {
                    s = GetTerrain(source, i - 1, j);
                }

                for (var a = 0; a < ExpansionFactor; a++)
                {
                    for (var b = 0; b < ExpansionFactor; b++)
                    {
                        var code = terrain;
                        if (a + b <= 1)
                        {
                            code = n;
                        }
                        else if (a - b >= 3)
                        {
                            code = e;
                        }
                        else if (b - a >= 3)
                        {
                            code = s;
                        }

                        var ex = i * ExpansionFactor + a;
                        var ey = j * ExpansionFactor + b;
                        expandedGrid[ey, ex] = (byte)code;
                    }
                }
            }
        }

        var expandedRows = new List<IReadOnlyList<byte>>(ExpandedSize);
        for (var y = 0; y < ExpandedSize; y++)
        {
            var row = new byte[ExpandedSize];
            for (var x = 0; x < ExpandedSize; x++)
            {
                row[x] = expandedGrid[y, x];
            }

            expandedRows.Add(Array.AsReadOnly(row));
        }

        return expandedRows;
    }

    private static IReadOnlyList<IReadOnlyList<byte>> RotateTerrain180(IReadOnlyList<IReadOnlyList<byte>> terrain)
    {
        if (terrain.Count == 0)
        {
            return terrain;
        }

        var height = terrain.Count;
        var width = terrain[0].Count;
        if (terrain.Any(row => row.Count != width))
        {
            return terrain;
        }

        var rotated = new List<IReadOnlyList<byte>>(height);
        for (var y = 0; y < height; y++)
        {
            var sourceRow = terrain[height - 1 - y];
            var row = new byte[width];
            for (var x = 0; x < width; x++)
            {
                row[x] = sourceRow[width - 1 - x];
            }
            rotated.Add(Array.AsReadOnly(row));
        }

        return rotated;
    }

    private static IReadOnlyList<StaticGraphic> TransformGraphics(IReadOnlyList<StaticGraphic> graphics, int width, int height) =>
        graphics.Select(g =>
        {
            var (x, y) = TransformCoord(g.X, g.Y, width, height, isEntity: true);
            return g with { X = x, Y = y };
        }).ToArray();

    private static IReadOnlyList<SensorRecord> TransformSensors(IReadOnlyList<SensorRecord> sensors, int width, int height) =>
        sensors.Select(s =>
        {
            var (x, y) = TransformCoord(s.X, s.Y, width, height, isEntity: true);
            return s with { X = x, Y = y };
        }).ToArray();

    private static IReadOnlyList<NestRecord> TransformNests(IReadOnlyList<NestRecord> nests, int width, int height) =>
        nests.Select(n =>
        {
            var (x, y) = TransformCoord(n.X, n.Y, width, height, isEntity: true);
            return n with { X = x, Y = y };
        }).ToArray();

    private static IReadOnlyList<MerchantRecord> TransformMerchants(IReadOnlyList<MerchantRecord> merchants, int width, int height) =>
        merchants.Select(m =>
        {
            var (x, y) = TransformCoord(m.X, m.Y, width, height, isEntity: true);
            return m with { X = x, Y = y };
        }).ToArray();

    private static MapExtendedData TransformExtendedData(MapExtendedData data, int width, int height)
    {
        if (width == 0 || height == 0)
        {
            return data;
        }

        var (x, y) = TransformCoord(data.RespawnX, data.RespawnY, width, height, isEntity: true);
        return data with { RespawnX = x, RespawnY = y };
    }

    private static (byte X, byte Y) TransformCoord(byte x, byte y, int width, int height, bool isEntity)
    {
        if (width == 0 || height == 0)
        {
            return (x, y);
        }

        int tx = x;
        int ty = y;

        if (ApplyRotation180 && !isEntity || (isEntity && ApplyEntityRotation180))
        {
            tx = width - 1 - tx;
            ty = height - 1 - ty;
        }

        if (ApplyTranspose && !isEntity || (isEntity && ApplyEntityTranspose))
        {
            (tx, ty) = (ty, tx);
        }

        var clampedX = Math.Clamp(tx, byte.MinValue, byte.MaxValue);
        var clampedY = Math.Clamp(ty, byte.MinValue, byte.MaxValue);
        return ((byte)clampedX, (byte)clampedY);
    }
}
