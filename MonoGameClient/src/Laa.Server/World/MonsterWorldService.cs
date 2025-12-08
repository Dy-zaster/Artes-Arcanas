using Laa.Content.Core.Maps;
using Laa.Content.Core.Monsters;
using Laa.Content.Json.Repositories;
using System.Buffers.Binary;

namespace Laa.Server.World;

public sealed class MonsterWorldService
{
    private readonly JsonMonsterRepository _monsterRepo;
    private readonly JsonMapRepository _mapRepo;
    private readonly Dictionary<byte, List<MonsterSpawn>> _cache = new();
    private int _nextEntityId = 1;

    public MonsterWorldService(string dataRoot)
    {
        var root = string.IsNullOrWhiteSpace(dataRoot)
            ? AppContext.BaseDirectory
            : dataRoot;

        var mapRoot = ResolvePath(root, Path.Combine("data", "maps"));
        var monsterPath = ResolvePath(root, Path.Combine("data", "monsters.json"));

        _monsterRepo = new JsonMonsterRepository(monsterPath);
        _mapRepo = new JsonMapRepository(mapRoot);
    }

    public byte[] BuildSnapshotPayload(byte mapId)
    {
        var spawns = GetSpawns(mapId);
        var count = Math.Min(255, spawns.Count);
        var entrySize = 16; // id(int) + type(ushort) + map(byte) + x(byte) + y(byte) + dir(byte) + mirror(byte) + action(byte) + hp(ushort) + maxHp(ushort)
        var buffer = new byte[1 + count * entrySize];
        buffer[0] = (byte)count;

        var span = buffer.AsSpan(1);
        for (var i = 0; i < count; i++)
        {
            var s = spawns[i];
            BinaryPrimitives.WriteInt32LittleEndian(span, s.EntityId);
            BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(4), s.TypeId);
            span[6] = s.MapId;
            span[7] = s.X;
            span[8] = s.Y;
            span[9] = s.Direction;
            span[10] = s.Mirror ? (byte)1 : (byte)0;
            span[11] = (byte)s.Action;
            BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(12), s.Health);
            BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(14), s.MaxHealth);
            span = span.Slice(entrySize);
        }

        return buffer;
    }

    private List<MonsterSpawn> GetSpawns(byte mapId)
    {
        if (_cache.TryGetValue(mapId, out var cached))
        {
            return cached;
        }

        var mapDoc = LoadMap(mapId);
        var monsters = _monsterRepo.GetMonsters().Monsters;
        var lookup = new Dictionary<int, MonsterDescriptor>(monsters.Count);
        foreach (var m in monsters)
        {
            lookup[m.TypeId] = m;
        }

        var list = new List<MonsterSpawn>();
        foreach (var nest in mapDoc.Nests)
        {
            if (!lookup.TryGetValue(nest.Type, out var desc))
            {
                continue;
            }

            var entityId = _nextEntityId++;
            var facing = (byte)(entityId % 8);
            var mirror = (entityId & 1) == 0;
            var hp = (ushort)Math.Max((short)1, desc.AverageHp);
            list.Add(new MonsterSpawn(
                entityId,
                desc.TypeId,
                mapId,
                (byte)nest.X,
                (byte)nest.Y,
                facing,
                mirror,
                MonsterSpawnAction.Idle,
                hp,
                hp));
        }

        _cache[mapId] = list;
        return list;
    }

    private MapDocument LoadMap(byte mapId)
    {
        var key = $"map_{mapId}";
        return _mapRepo.GetMap(key);
    }

    private static string ResolvePath(string root, string relative)
    {
        var candidate = Path.GetFullPath(Path.Combine(root, relative));
        if (File.Exists(candidate) || Directory.Exists(candidate))
        {
            return candidate;
        }

        // Fallback to project layout (../../content/data/...)
        var fallback = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "content", "data"));
        if (relative.EndsWith("monsters.json", StringComparison.OrdinalIgnoreCase))
        {
            var alt = Path.Combine(fallback, "monsters.json");
            return alt;
        }

        return Path.Combine(fallback, "maps");
    }
}

public enum MonsterSpawnAction : byte
{
    Idle = 0,
    Attack = 1,
    Moving = 2,
    Dead = 3
}

public sealed record MonsterSpawn(
    int EntityId,
    ushort TypeId,
    byte MapId,
    byte X,
    byte Y,
    byte Direction,
    bool Mirror,
    MonsterSpawnAction Action,
    ushort Health,
    ushort MaxHealth);
