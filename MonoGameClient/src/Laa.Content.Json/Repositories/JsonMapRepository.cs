using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Laa.Content.Core.Maps;
using Laa.Content.Core.Repositories;
using Laa.Content.Json.Internal;

namespace Laa.Content.Json.Repositories;

public sealed class JsonMapRepository : IMapRepository
{
    private readonly string _mapRoot;
    private readonly JsonSerializerOptions _options;
    private readonly ConcurrentDictionary<string, MapDocument> _cache = new(StringComparer.OrdinalIgnoreCase);

    public JsonMapRepository(string mapRoot, JsonSerializerOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(mapRoot)) throw new ArgumentException("Path cannot be empty", nameof(mapRoot));
        _mapRoot = Path.GetFullPath(mapRoot);
        _options = options ?? JsonDefaults.CreateOptions();
    }

    public IReadOnlyList<string> ListMaps()
    {
        if (!Directory.Exists(_mapRoot))
        {
            return Array.Empty<string>();
        }

        var files = Directory.GetFiles(_mapRoot, "map_*.json", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return files;
    }

    public MapDocument GetMap(string mapId)
    {
        if (string.IsNullOrWhiteSpace(mapId)) throw new ArgumentException("Map id cannot be empty", nameof(mapId));
        return _cache.GetOrAdd(mapId, LoadMap);
    }

    private MapDocument LoadMap(string mapId)
    {
        var candidate = ResolvePath(mapId);
        if (!File.Exists(candidate))
        {
            throw new FileNotFoundException($"Map '{mapId}' was not found under '{_mapRoot}'.", candidate);
        }

        var document = JsonFileLoader.Load<MapDocument>(candidate, _options);
        JsonValidation.Ensure(document.Terrain.Count > 0, $"Map '{mapId}' is missing terrain data.");
        return document;
    }

    private string ResolvePath(string mapId)
    {
        if (Directory.Exists(_mapRoot))
        {
            var fileName = Path.HasExtension(mapId) ? mapId : mapId + ".json";
            return Path.Combine(_mapRoot, fileName);
        }

        return _mapRoot;
    }
}
