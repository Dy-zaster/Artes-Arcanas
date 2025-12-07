using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using ArtesArcanas.Server.Core.Legacy;
using ArtesArcanas.Server.Core.Logging;
using ArtesArcanas.Server.Core.World.Maps;

namespace ArtesArcanas.Server.Core.World;

/// <summary>
///     Minimal port of the Pascal server slot allocator. Handles reservation and release of player codes (0..255).
/// </summary>
public sealed class LegacyWorldState
{
    private const int MaxSearchRadius = 12;

    private readonly object _sync = new();
    private readonly bool[] _slots = new bool[Legacy.LegacyConstants.MaxPlayers + 1];
    private readonly HashSet<string> _activeLogins = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<ushort, LegacyPlayerContext> _players = new();
    private readonly Dictionary<byte, LegacyMapRuntime> _mapRuntimes;
    private readonly LegacyMapSurface _mapSurface;
    private readonly object _monsterSync = new();
    private readonly object _mapStateSync = new();
    private readonly Dictionary<ushort, LegacyMonsterInstance> _monstersByCode = new();
    private readonly Dictionary<byte, List<LegacyMonsterInstance>> _monstersByMap = new();
    private readonly Dictionary<byte, List<LegacyMonsterInstance>> _merchantMonstersByMap = new();
    private readonly Dictionary<uint, ushort> _monsterOccupancy = new();
    private readonly Dictionary<byte, int> _mapFlags = new();
    private ushort _nextMonsterCode = 0x4000;
    private readonly LegacyGroundItemStore _groundItems;
    private readonly LegacyMonsterCatalog _monsterCatalog;
    private readonly IServerLogger _logger;
    private readonly HashSet<byte> _missingMonsterTypes = new();
    private LegacyWeatherType _globalWeatherType = LegacyWeatherType.Normal;
    private byte _globalWeatherIntensity;
    private sbyte _globalWeatherDrift;

    public LegacyWorldState(LegacyMapSurface mapSurface, LegacyMapManager mapManager, LegacyMonsterCatalog monsters, IServerLogger logger)
    {
        _mapSurface = mapSurface ?? throw new ArgumentNullException(nameof(mapSurface));
        if (mapManager is null)
        {
            throw new ArgumentNullException(nameof(mapManager));
        }

        _groundItems = new LegacyGroundItemStore();
        _monsterCatalog = monsters ?? LegacyMonsterCatalog.Empty;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _mapRuntimes = BuildMapRuntimes(mapManager);
        InitializeMapFlags(mapManager);
        InitializeMonsters(mapManager);
    }

    public bool TryReserveSlot(IPEndPoint remoteEndPoint, out ushort code)
    {
        _ = remoteEndPoint;
        lock (_sync)
        {
            for (ushort i = 0; i < _slots.Length; i++)
            {
                if (_slots[i])
                {
                    continue;
                }

                _slots[i] = true;
                code = i;
                return true;
            }
        }

        code = 0;
        return false;
    }

    public void ReleaseSlot(ushort code)
    {
        if (code >= _slots.Length)
        {
            return;
        }

        lock (_sync)
        {
            _slots[code] = false;
        }
    }

    public bool IsLoginActive(string login)
    {
        lock (_sync)
        {
            return _activeLogins.Contains(login);
        }
    }

    public void RegisterLogin(string login)
    {
        lock (_sync)
        {
            _activeLogins.Add(login);
        }
    }

    public void ReleaseLogin(string? login)
    {
        if (string.IsNullOrWhiteSpace(login))
        {
            return;
        }

        lock (_sync)
        {
            _activeLogins.Remove(login);
        }
    }

    public LegacyPlayerContext RegisterPlayer(
        ushort code,
        string login,
        string avatarName,
        LegacyUserState state,
        ref LegacyPlayerSnapshot snapshot,
        out bool spawnAdjusted)
    {
        if (!TryReserveInitialPosition(code, ref snapshot, out spawnAdjusted))
        {
            throw new InvalidOperationException($"No se pudo ubicar al jugador {login} (mapa {snapshot.CodigoMapa}).");
        }

        var context = new LegacyPlayerContext(code, login, avatarName, state, snapshot);
        _players[code] = context;
        AttachPlayerToMap(context, snapshot.CodigoMapa);
        return context;
    }

    public void RemovePlayer(ushort code)
    {
        if (_players.TryRemove(code, out var context))
        {
            ReleaseOccupancy(context.Snapshot.CodigoMapa, context.Snapshot.CoordenadaX, context.Snapshot.CoordenadaY, code);
            DetachPlayerFromMap(context);
        }
    }

    public bool TryGetPlayer(ushort code, out LegacyPlayerContext? context)
    {
        if (_players.TryGetValue(code, out var existing))
        {
            context = existing;
            return true;
        }

        context = null;
        return false;
    }

    public void EnqueueAction(ushort code, LegacyPlayerAction action)
    {
        if (_players.TryGetValue(code, out var player))
        {
            player.EnqueueAction(action);
        }
    }

    public bool IsWalkable(byte mapId, int x, int y) =>
        _mapSurface.IsWalkable(mapId, x, y);

    public bool TryMovePlayer(
        ushort code,
        byte fromMap,
        byte fromX,
        byte fromY,
        byte toMap,
        byte toX,
        byte toY)
    {
        if (fromMap == toMap && fromX == toX && fromY == toY)
        {
            return true;
        }

        if (!_mapRuntimes.TryGetValue(fromMap, out var sourceRuntime))
        {
            return false;
        }

        var destinationRuntime = sourceRuntime;
        if (fromMap != toMap)
        {
            if (!_mapRuntimes.TryGetValue(toMap, out destinationRuntime))
            {
                return false;
            }
        }

        if (!destinationRuntime.TryOccupy(toX, toY, code, LegacyMapSurface.PlayerTerrainMask))
        {
            return false;
        }

        sourceRuntime.Release(fromX, fromY, code);
        if (fromMap != toMap)
        {
            MovePlayerBetweenMaps(code, fromMap, toMap);
        }

        return true;
    }

    public bool TryGetMapDefinition(byte mapId, out LegacyMapDefinition? definition) =>
        _mapSurface.TryGetDefinition(mapId, out definition);

    public bool TryGetRespawn(byte mapId, out byte respawnMap, out byte respawnX, out byte respawnY) =>
        _mapSurface.TryGetRespawn(mapId, out respawnMap, out respawnX, out respawnY);

    internal LegacyGroundItemStore GroundItems => _groundItems;

    public IReadOnlyCollection<LegacyPlayerContext> GetPlayers() => _players.Values.ToArray();

    public IReadOnlyList<LegacyPlayerContext> GetPlayersOnMap(byte mapId)
    {
        if (_mapRuntimes.TryGetValue(mapId, out var runtime))
        {
            return runtime.SnapshotPlayers();
        }

        return Array.Empty<LegacyPlayerContext>();
    }

    public IReadOnlyList<LegacyPlayerContext> GetPlayersInArea(byte mapId, byte x, byte y, int radiusX, int radiusY)
    {
        if (_mapRuntimes.TryGetValue(mapId, out var runtime))
        {
            return runtime.SnapshotPlayersInArea(x, y, radiusX, radiusY);
        }

        return Array.Empty<LegacyPlayerContext>();
    }

    public IReadOnlyList<LegacyMonsterInstance> GetMonsters(byte mapId)
    {
        lock (_monsterSync)
        {
            if (_monstersByMap.TryGetValue(mapId, out var list))
            {
                return list.ToArray();
            }
        }

        return Array.Empty<LegacyMonsterInstance>();
    }

    public IReadOnlyList<LegacyMonsterInstance> GetMerchantMonsters(byte mapId)
    {
        lock (_monsterSync)
        {
            if (_merchantMonstersByMap.TryGetValue(mapId, out var list))
            {
                return list.ToArray();
            }
        }

        return Array.Empty<LegacyMonsterInstance>();
    }

    internal IReadOnlyCollection<LegacyMonsterInstance> GetAllMonsters()
    {
        lock (_monsterSync)
        {
            return _monstersByCode.Values.ToArray();
        }
    }

    public (LegacyWeatherType Type, byte Intensity, sbyte Drift) GetGlobalWeather()
    {
        lock (_sync)
        {
            return (_globalWeatherType, _globalWeatherIntensity, _globalWeatherDrift);
        }
    }

    public void SetGlobalWeather(LegacyWeatherType type, byte intensity, sbyte drift)
    {
        lock (_sync)
        {
            _globalWeatherType = type;
            _globalWeatherIntensity = intensity;
            _globalWeatherDrift = drift;
        }
    }

    public int GetMapFlags(byte mapId)
    {
        lock (_mapStateSync)
        {
            if (_mapFlags.TryGetValue(mapId, out var flags))
            {
                return flags;
            }

            return 0;
        }
    }

    public bool TryUpdateMapFlags(byte mapId, int setMask, int clearMask, out int newValue)
    {
        lock (_mapStateSync)
        {
            if (!_mapFlags.TryGetValue(mapId, out var flags))
            {
                flags = 0;
            }

            var updated = (flags | setMask) & ~clearMask;
            newValue = updated;
            if (updated == flags)
            {
                return false;
            }

            _mapFlags[mapId] = updated;
            return true;
        }
    }

    public bool TryToggleMapFlags(byte mapId, int mask, out int newValue)
    {
        lock (_mapStateSync)
        {
            if (!_mapFlags.TryGetValue(mapId, out var flags))
            {
                flags = 0;
            }

            var updated = flags ^ mask;
            newValue = updated;
            if (updated == flags)
            {
                return false;
            }

            _mapFlags[mapId] = updated;
            return true;
        }
    }

    internal bool TryMoveMonster(LegacyMonsterInstance monster, byte newX, byte newY, byte direction)
    {
        lock (_monsterSync)
        {
            var allowedTerrain = ResolveAllowedTerrain(monster.Descriptor.TerrainFlags);
            _mapRuntimes.TryGetValue(monster.MapId, out var runtime);
            var canWalk = runtime?.IsWalkable(newX, newY, allowedTerrain) ??
                          _mapSurface.IsWalkable(monster.MapId, newX, newY, allowedTerrain);
            if (!canWalk)
            {
                return false;
            }

            var targetKey = ComposeKey(monster.MapId, newX, newY);
            if (_monsterOccupancy.TryGetValue(targetKey, out var occupant) && occupant != monster.Code)
            {
                return false;
            }

            if (runtime is not null && runtime.HasOccupant(newX, newY))
            {
                return false;
            }

            var sourceKey = ComposeKey(monster.MapId, monster.X, monster.Y);
            _monsterOccupancy.Remove(sourceKey);
            _monsterOccupancy[targetKey] = monster.Code;
            monster.SetPosition(newX, newY, direction);
            return true;
        }
    }

    public bool TryGetMapRuntime(byte mapId, out LegacyMapRuntime? runtime) =>
        _mapRuntimes.TryGetValue(mapId, out runtime);

    private static LegacyTerrainFlags ResolveAllowedTerrain(ushort descriptorFlags)
    {
        var mask = (LegacyTerrainFlags)descriptorFlags;
        return mask == LegacyTerrainFlags.None ? LegacyMapSurface.PlayerTerrainMask : mask;
    }

    private static Dictionary<byte, LegacyMapRuntime> BuildMapRuntimes(LegacyMapManager mapManager)
    {
        var runtimes = new Dictionary<byte, LegacyMapRuntime>();
        foreach (var definition in mapManager.GetAll())
        {
            runtimes[definition.Id] = new LegacyMapRuntime(definition);
        }

        return runtimes;
    }

    private void AttachPlayerToMap(LegacyPlayerContext player, byte mapId)
    {
        if (_mapRuntimes.TryGetValue(mapId, out var runtime))
        {
            runtime.AddOrUpdatePlayer(player);
        }
    }

    private void DetachPlayerFromMap(LegacyPlayerContext player)
    {
        if (_mapRuntimes.TryGetValue(player.Snapshot.CodigoMapa, out var runtime))
        {
            runtime.RemovePlayer(player.Code);
        }
    }

    private void MovePlayerBetweenMaps(ushort code, byte fromMap, byte toMap)
    {
        if (!_players.TryGetValue(code, out var player))
        {
            return;
        }

        if (_mapRuntimes.TryGetValue(fromMap, out var current))
        {
            current.RemovePlayer(code);
        }

        if (_mapRuntimes.TryGetValue(toMap, out var next))
        {
            next.AddOrUpdatePlayer(player);
        }
    }

    private bool TryReserveInitialPosition(ushort code, ref LegacyPlayerSnapshot snapshot, out bool adjusted)
    {
        adjusted = false;
        var mapId = snapshot.CodigoMapa;
        var x = snapshot.CoordenadaX;
        var y = snapshot.CoordenadaY;

        if (!_mapRuntimes.TryGetValue(mapId, out var runtime))
        {
            return false;
        }

        if (runtime.TryOccupy(x, y, code, LegacyMapSurface.PlayerTerrainMask))
        {
            return true;
        }

        if (TryReserveNearbyPosition(runtime, x, y, code, ref snapshot))
        {
            adjusted = true;
            return true;
        }

        return false;
    }

    private void ReleaseOccupancy(byte mapId, byte x, byte y, ushort code)
    {
        if (_mapRuntimes.TryGetValue(mapId, out var runtime))
        {
            runtime.Release(x, y, code);
        }
    }

    private bool TryReserveNearbyPosition(LegacyMapRuntime runtime, byte startX, byte startY, ushort code, ref LegacyPlayerSnapshot snapshot)
    {
        for (var radius = 1; radius <= MaxSearchRadius; radius++)
        {
            for (var dx = -radius; dx <= radius; dx++)
            {
                for (var dy = -radius; dy <= radius; dy++)
                {
                    var candX = (int)startX + dx;
                    var candY = (int)startY + dy;
                    if (candX < 0 || candX >= LegacyMapGrid.ExpandedWidth ||
                        candY < 0 || candY >= LegacyMapGrid.ExpandedHeight)
                    {
                        continue;
                    }

                    var bx = (byte)candX;
                    var by = (byte)candY;
                    if (!runtime.TryOccupy(bx, by, code, LegacyMapSurface.PlayerTerrainMask))
                    {
                        continue;
                    }

                    snapshot.CoordenadaX = bx;
                    snapshot.CoordenadaY = by;
                    snapshot.DestinoX = bx;
                    snapshot.DestinoY = by;
                    return true;
                }
            }
        }

        return false;
    }
    private void InitializeMapFlags(LegacyMapManager mapManager)
    {
        lock (_mapStateSync)
        {
            foreach (var definition in mapManager.GetAll())
            {
                _mapFlags[definition.Id] = definition.ExtendedData.InitialFlags;
            }
        }
    }

    private void InitializeMonsters(LegacyMapManager mapManager)
    {
        foreach (var definition in mapManager.GetAll())
        {
            foreach (var nest in definition.Nests)
            {
                for (var i = 0; i < nest.Quantity; i++)
                {
                    SpawnMonster(definition.Id, nest.X, nest.Y, nest.Type, isMerchant: false);
                }
            }

            foreach (var merchant in definition.Merchants)
            {
                SpawnMonster(definition.Id, merchant.X, merchant.Y, merchant.MonsterId, isMerchant: true);
            }
        }
    }

    private LegacyMonsterInstance? SpawnMonster(byte mapId, byte spawnX, byte spawnY, byte monsterType, bool isMerchant)
    {
        if (!_monsterCatalog.TryGetDescriptor(monsterType, out var descriptor) || descriptor is null)
        {
            ReportMissingMonster(monsterType);
            return null;
        }

        var allowedTerrain = ResolveAllowedTerrain(descriptor.TerrainFlags);
        if (!TryFindMonsterPosition(mapId, spawnX, spawnY, allowedTerrain, out var x, out var y))
        {
            return null;
        }

        lock (_monsterSync)
        {
            var code = NextMonsterCode();
            var instance = new LegacyMonsterInstance(code, mapId, x, y, monsterType, isMerchant, descriptor);
            _monstersByCode[code] = instance;
            if (!_monstersByMap.TryGetValue(mapId, out var list))
            {
                list = new List<LegacyMonsterInstance>();
                _monstersByMap[mapId] = list;
            }

            list.Add(instance);

            if (isMerchant)
            {
                if (!_merchantMonstersByMap.TryGetValue(mapId, out var merchants))
                {
                    merchants = new List<LegacyMonsterInstance>();
                    _merchantMonstersByMap[mapId] = merchants;
                }

                merchants.Add(instance);
            }

            var key = ComposeKey(mapId, x, y);
            _monsterOccupancy[key] = code;
            return instance;
        }
    }

    private bool TryFindMonsterPosition(byte mapId, byte spawnX, byte spawnY, LegacyTerrainFlags allowedTerrain, out byte x, out byte y)
    {
        lock (_monsterSync)
        {
            var offsets = new (sbyte dx, sbyte dy)[]
            {
                (0, 0),
                (1, 0),
                (0, 1),
                (-1, 0),
                (0, -1),
                (1, 1),
                (-1, -1),
                (2, 0),
                (0, 2),
                (-2, 0),
                (0, -2)
            };
            _mapRuntimes.TryGetValue(mapId, out var runtime);

            foreach (var (dx, dy) in offsets)
            {
                var candidateX = (int)spawnX + dx;
                var candidateY = (int)spawnY + dy;
                if (candidateX < 0 || candidateX >= LegacyMapGrid.ExpandedWidth ||
                    candidateY < 0 || candidateY >= LegacyMapGrid.ExpandedHeight)
                {
                    continue;
                }

                var bx = (byte)candidateX;
                var by = (byte)candidateY;
                var key = ComposeKey(mapId, bx, by);
                var canWalk = runtime?.IsWalkable(bx, by, allowedTerrain) ??
                              _mapSurface.IsWalkable(mapId, bx, by, allowedTerrain);
                if (!canWalk)
                {
                    continue;
                }

                if (_monsterOccupancy.ContainsKey(key))
                {
                    continue;
                }

                if (runtime is not null && runtime.HasOccupant(bx, by))
                {
                    continue;
                }

                x = bx;
                y = by;
                return true;
            }

            x = spawnX;
            y = spawnY;
            return runtime?.IsWalkable(spawnX, spawnY, allowedTerrain) ??
                   _mapSurface.IsWalkable(mapId, spawnX, spawnY, allowedTerrain);
        }
    }

    private void ReportMissingMonster(byte monsterType)
    {
        if (_logger is null)
        {
            return;
        }

        if (_missingMonsterTypes.Add(monsterType))
        {
            _logger.Warning($"No se encontró el monstruo legacy #{monsterType} en std.mon.");
        }
    }

    private ushort NextMonsterCode() => _nextMonsterCode++;

    private static uint ComposeKey(byte mapId, byte x, byte y) =>
        (uint)((mapId << 16) | (y << 8) | x);
}
