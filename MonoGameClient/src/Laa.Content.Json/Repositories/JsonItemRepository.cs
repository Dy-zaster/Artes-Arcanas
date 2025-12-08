using Laa.Content.Core.Items;
using Laa.Content.Core.Repositories;
using Laa.Content.Json.Internal;
using System.Text.Json;

namespace Laa.Content.Json.Repositories;

public sealed class JsonItemRepository : IItemRepository
{
    private readonly object _sync = new();
    private readonly string _filePath;
    private readonly JsonSerializerOptions _options;
    private ItemDocument? _cache;

    public JsonItemRepository(string filePath, JsonSerializerOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("Path cannot be empty", nameof(filePath));
        _filePath = Path.GetFullPath(filePath);
        _options = options ?? JsonDefaults.CreateOptions();
    }

    public ItemDocument GetItems()
    {
        if (_cache is ItemDocument cached)
        {
            return cached;
        }

        lock (_sync)
        {
            _cache ??= Load();
            return _cache;
        }
    }

    private ItemDocument Load()
    {
        if (!File.Exists(_filePath))
        {
            throw new FileNotFoundException("Item catalog not found.", _filePath);
        }

        var document = JsonFileLoader.Load<ItemDocument>(_filePath, _options);
        JsonValidation.Ensure(document.Items.Count == document.Names.Count, "Item catalog mismatch between names and descriptors.");
        return document;
    }
}
