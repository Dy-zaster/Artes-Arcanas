using Laa.Content.Core.Monsters;
using Laa.Content.Core.Repositories;
using Laa.Content.Json.Internal;
using System.Text.Json;

namespace Laa.Content.Json.Repositories;

public sealed class JsonMonsterRepository : IMonsterRepository
{
    private readonly object _sync = new();
    private readonly string _filePath;
    private readonly JsonSerializerOptions _options;
    private MonsterDocument? _cache;

    public JsonMonsterRepository(string filePath, JsonSerializerOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("Path cannot be empty", nameof(filePath));
        _filePath = Path.GetFullPath(filePath);
        _options = options ?? JsonDefaults.CreateOptions();
    }

    public MonsterDocument GetMonsters()
    {
        if (_cache is MonsterDocument cached) return cached;
        lock (_sync)
        {
            _cache ??= Load();
            return _cache;
        }
    }

    private MonsterDocument Load()
    {
        if (!File.Exists(_filePath))
        {
            throw new FileNotFoundException("Monster file not found.", _filePath);
        }

        var document = JsonFileLoader.Load<MonsterDocument>(_filePath, _options);
        JsonValidation.Ensure(document.Monsters.Count > 0, "Monster catalog is empty.");
        return document;
    }
}
