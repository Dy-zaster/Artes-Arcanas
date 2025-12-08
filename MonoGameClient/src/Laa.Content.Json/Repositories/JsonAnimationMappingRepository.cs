using Laa.Content.Core.Mappings;
using Laa.Content.Core.Repositories;
using Laa.Content.Json.Internal;
using System.Text.Json;

namespace Laa.Content.Json.Repositories;

public sealed class JsonAnimationMappingRepository : IAnimationMappingRepository
{
    private readonly object _sync = new();
    private readonly string _filePath;
    private readonly JsonSerializerOptions _options;
    private AnimationMappingDocument? _cache;

    public JsonAnimationMappingRepository(string filePath, JsonSerializerOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("Path cannot be empty", nameof(filePath));
        _filePath = Path.GetFullPath(filePath);
        _options = options ?? JsonDefaults.CreateOptions();
    }

    public AnimationMappingDocument GetMappings()
    {
        if (_cache is AnimationMappingDocument cached) return cached;
        lock (_sync)
        {
            _cache ??= Load();
            return _cache;
        }
    }

    private AnimationMappingDocument Load()
    {
        if (!File.Exists(_filePath))
        {
            throw new FileNotFoundException("Animation mapping file not found.", _filePath);
        }

        var document = JsonFileLoader.Load<AnimationMappingDocument>(_filePath, _options);
        JsonValidation.Ensure(document.AnimationIds.Count > 0, "Animation mapping file is empty.");
        return document;
    }
}
