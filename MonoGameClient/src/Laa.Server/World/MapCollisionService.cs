using Laa.Content.Core.Graphics;
using Laa.Content.Core.Maps;
using Laa.Content.Json.Repositories;

namespace Laa.Server.World;

public sealed class MapCollisionService
{
    private readonly JsonMapRepository _mapRepo;
    private readonly GraphicDocument _graphics;
    private readonly Dictionary<byte, bool[,]> _cache = new();

    public MapCollisionService(string dataRoot)
    {
        var mapRoot = Path.Combine(dataRoot, "data", "maps");
        var mapPath = Directory.Exists(mapRoot)
            ? mapRoot
            : Path.Combine(AppContext.BaseDirectory, "..", "..", "content", "data", "maps");

        _mapRepo = new JsonMapRepository(mapPath);

        var graphicsPath = Path.Combine(dataRoot, "data", "graphics.json");
        if (!File.Exists(graphicsPath))
        {
            graphicsPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "content", "data", "graphics.json");
        }

        var graphicsRepo = new JsonGraphicRepository(graphicsPath);
        _graphics = graphicsRepo.GetGraphics();
    }

    public bool IsWalkable(byte mapId, byte x, byte y)
    {
        var grid = GetGrid(mapId);
        if (grid.GetLength(0) == 0) return true;
        if (x < 0 || y < 0 || x >= grid.GetLength(0) || y >= grid.GetLength(1))
        {
            return false;
        }

        return !grid[x, y];
    }

    private bool[,] GetGrid(byte mapId)
    {
        if (_cache.TryGetValue(mapId, out var cached))
        {
            return cached;
        }

        var map = _mapRepo.GetMap($"map_{mapId}");
        var width = map.Terrain[0].Count;
        var height = map.Terrain.Count;
        var grid = new bool[width, height];

        for (var y = 0; y < height; y++)
        {
            var row = map.Terrain[y];
            for (var x = 0; x < width; x++)
            {
                if (IsTerrainBlocked(row[x]))
                {
                    grid[x, y] = true;
                }
            }
        }

        foreach (var graphic in map.Graphics)
        {
            var descriptor = ResolveDescriptor(graphic);
            if (descriptor is null)
            {
                continue;
            }

            var baseX = graphic.X - 4;
            var baseY = graphic.Y - descriptor.AlignY;
            for (var row = 0; row < descriptor.OccupiedMask.Count; row++)
            {
                var mask = descriptor.OccupiedMask[row];
                if (mask == 0) continue;
                for (var bit = 0; bit < 8; bit++)
                {
                    if ((mask & (1 << bit)) == 0) continue;
                    var tx = baseX + bit;
                    var ty = baseY + row;
                    if (tx >= 0 && ty >= 0 && tx < width && ty < height)
                    {
                        grid[tx, ty] = true;
                    }
                }
            }
        }

        _cache[mapId] = grid;
        return grid;
    }

    private GraphicDescriptor? ResolveDescriptor(StaticGraphic graphic)
    {
        var index = graphic.CodeFlags & 0x03FF;
        if (index < 0 || index >= _graphics.Descriptors.Count)
        {
            return null;
        }

        return _graphics.Descriptors[index];
    }

    private static bool IsTerrainBlocked(byte code)
    {
        return code >= 28;
    }
}
