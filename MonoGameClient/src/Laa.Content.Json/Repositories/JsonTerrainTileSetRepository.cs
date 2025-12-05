using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Laa.Content.Core.Graphics;
using Laa.Content.Core.Repositories;
using Laa.Content.Json.Internal;

namespace Laa.Content.Json.Repositories;

public sealed class JsonTerrainTileSetRepository : ITerrainTileSetRepository
{
    private readonly string _filePath;
    private readonly JsonSerializerOptions _options;
    private readonly ConcurrentDictionary<string, TerrainTileSet> _cache = new(StringComparer.OrdinalIgnoreCase);

    public JsonTerrainTileSetRepository(string filePath, JsonSerializerOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("Path cannot be empty", nameof(filePath));
        _filePath = Path.GetFullPath(filePath);
        _options = options ?? JsonDefaults.CreateOptions();
    }

    public TerrainTileSet GetTileSet(string mapId)
    {
        if (string.IsNullOrWhiteSpace(mapId)) throw new ArgumentException("Map id cannot be empty", nameof(mapId));
        return _cache.GetOrAdd(mapId, Load);
    }

    private TerrainTileSet Load(string mapId)
    {
        if (!File.Exists(_filePath))
        {
            throw new FileNotFoundException("Terrain tile definition file not found.", _filePath);
        }

        var manifest = JsonFileLoader.Load<Dictionary<string, TerrainTileSet>>(_filePath, _options);
        if (!manifest.TryGetValue(mapId, out var tileSet))
        {
            throw new KeyNotFoundException($"Terrain tile set not found for map '{mapId}'.");
        }

        return tileSet;
    }
}
