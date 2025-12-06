using System;
using System.IO;
using System.Text.Json;
using Laa.Content.Core.Animations;
using Laa.Content.Core.Repositories;
using Laa.Content.Json.Internal;

namespace Laa.Content.Json.Repositories;

public sealed class JsonAnimationRepository : IAnimationRepository
{
    private readonly object _sync = new();
    private readonly string _filePath;
    private readonly JsonSerializerOptions _options;
    private AnimationDocument? _cache;

    public JsonAnimationRepository(string filePath, JsonSerializerOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("Path cannot be empty", nameof(filePath));
        _filePath = Path.GetFullPath(filePath);
        _options = options ?? JsonDefaults.CreateOptions();
    }

    public AnimationDocument GetAnimations()
    {
        if (_cache is AnimationDocument cached) return cached;
        lock (_sync)
        {
            _cache ??= Load();
            return _cache;
        }
    }

    private AnimationDocument Load()
    {
        if (!File.Exists(_filePath))
        {
            throw new FileNotFoundException("Animation descriptor file not found.", _filePath);
        }

        var document = JsonFileLoader.Load<AnimationDocument>(_filePath, _options);
        JsonValidation.Ensure(document.Animations.Count > 0, "Animation descriptor file is empty.");
        return document;
    }
}
