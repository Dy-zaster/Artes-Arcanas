using Laa.Content.Core.Graphics;
using Laa.Content.Core.Repositories;
using Laa.Content.Json.Internal;
using System.Text.Json;

namespace Laa.Content.Json.Repositories;

public sealed class JsonGraphicRepository : IGraphicRepository
{
    private readonly object _sync = new();
    private readonly string _filePath;
    private readonly JsonSerializerOptions _options;
    private GraphicDocument? _cache;

    public JsonGraphicRepository(string filePath, JsonSerializerOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("Path cannot be empty", nameof(filePath));
        _filePath = Path.GetFullPath(filePath);
        _options = options ?? JsonDefaults.CreateOptions();
    }

    public GraphicDocument GetGraphics()
    {
        if (_cache is GraphicDocument cached) return cached;
        lock (_sync)
        {
            _cache ??= Load();
            return _cache;
        }
    }

    private GraphicDocument Load()
    {
        if (!File.Exists(_filePath))
        {
            throw new FileNotFoundException("Graphic descriptor file not found.", _filePath);
        }

        var document = JsonFileLoader.Load<GraphicDocument>(_filePath, _options);
        JsonValidation.Ensure(document.Descriptors.Count == document.Names.Count, "Graphic descriptor mismatch between names and entries.");
        return document;
    }
}
