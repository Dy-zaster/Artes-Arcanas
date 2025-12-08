using Laa.Content.Core.Commerce;
using Laa.Content.Core.Repositories;
using Laa.Content.Json.Internal;
using System.Text.Json;

namespace Laa.Content.Json.Repositories;

public sealed class JsonCommerceRepository : ICommerceRepository
{
    private readonly object _sync = new();
    private readonly string _filePath;
    private readonly JsonSerializerOptions _options;
    private CommerceDocument? _cache;

    public JsonCommerceRepository(string filePath, JsonSerializerOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("Path cannot be empty", nameof(filePath));
        _filePath = Path.GetFullPath(filePath);
        _options = options ?? JsonDefaults.CreateOptions();
    }

    public CommerceDocument GetCommerce()
    {
        if (_cache is CommerceDocument cached) return cached;
        lock (_sync)
        {
            _cache ??= Load();
            return _cache;
        }
    }

    private CommerceDocument Load()
    {
        if (!File.Exists(_filePath))
        {
            throw new FileNotFoundException("Commerce file not found.", _filePath);
        }

        var document = JsonFileLoader.Load<CommerceDocument>(_filePath, _options);
        JsonValidation.Ensure(document.Inventories.Count > 0, "Commerce file contains no inventories.");
        return document;
    }
}
