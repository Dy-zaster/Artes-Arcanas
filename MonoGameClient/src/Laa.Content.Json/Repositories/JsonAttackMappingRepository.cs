using System;
using System.IO;
using System.Text.Json;
using Laa.Content.Core.Mappings;
using Laa.Content.Core.Repositories;
using Laa.Content.Json.Internal;

namespace Laa.Content.Json.Repositories;

public sealed class JsonAttackMappingRepository : IAttackMappingRepository
{
    private readonly object _sync = new();
    private readonly string _filePath;
    private readonly JsonSerializerOptions _options;
    private AttackMappingDocument? _cache;

    public JsonAttackMappingRepository(string filePath, JsonSerializerOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("Path cannot be empty", nameof(filePath));
        _filePath = Path.GetFullPath(filePath);
        _options = options ?? JsonDefaults.CreateOptions();
    }

    public AttackMappingDocument GetMappings()
    {
        if (_cache is AttackMappingDocument cached) return cached;
        lock (_sync)
        {
            _cache ??= Load();
            return _cache;
        }
    }

    private AttackMappingDocument Load()
    {
        if (!File.Exists(_filePath))
        {
            throw new FileNotFoundException("Attack mapping file not found.", _filePath);
        }

        var document = JsonFileLoader.Load<AttackMappingDocument>(_filePath, _options);
        JsonValidation.Ensure(document.Entries.Count > 0, "Attack mapping file is empty.");
        return document;
    }
}
