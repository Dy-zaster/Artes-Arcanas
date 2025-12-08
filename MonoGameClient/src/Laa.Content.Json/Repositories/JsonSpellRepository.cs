using Laa.Content.Core.Repositories;
using Laa.Content.Core.Spells;
using Laa.Content.Json.Internal;
using System.Text.Json;

namespace Laa.Content.Json.Repositories;

public sealed class JsonSpellRepository : ISpellRepository
{
    private readonly object _sync = new();
    private readonly string _filePath;
    private readonly JsonSerializerOptions _options;
    private SpellDocument? _cache;

    public JsonSpellRepository(string filePath, JsonSerializerOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("Path cannot be empty", nameof(filePath));
        _filePath = Path.GetFullPath(filePath);
        _options = options ?? JsonDefaults.CreateOptions();
    }

    public SpellDocument GetSpells()
    {
        if (_cache is SpellDocument cached) return cached;
        lock (_sync)
        {
            _cache ??= Load();
            return _cache;
        }
    }

    private SpellDocument Load()
    {
        if (!File.Exists(_filePath))
        {
            throw new FileNotFoundException("Spell catalog not found.", _filePath);
        }

        var document = JsonFileLoader.Load<SpellDocument>(_filePath, _options);
        JsonValidation.Ensure(document.Spells.Count == document.Names.Count, "Spell catalog mismatch between names and descriptors.");
        return document;
    }
}
