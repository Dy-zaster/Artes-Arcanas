using System;
using System.Collections.Generic;
using ArtesArcanas.Server.Core.World.Maps;

namespace ArtesArcanas.Server.Core.World;

/// <summary>
///     Representa el estado vivo de un mapa individual (jugadores presentes y consultas de área).
///     Su objetivo es acercarse al comportamiento de <c>TTableroControlado</c> del servidor Pascal.
/// </summary>
public sealed class LegacyMapRuntime
{
    private static readonly LegacyPlayerContext[] EmptyPlayers = Array.Empty<LegacyPlayerContext>();

    private readonly object _sync = new();
    private readonly Dictionary<ushort, LegacyPlayerContext> _players = new();
    private readonly Dictionary<int, Occupant> _occupants = new();

    public LegacyMapRuntime(LegacyMapDefinition definition)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        MapId = Definition.Id;
    }

    public byte MapId { get; }
    public LegacyMapDefinition Definition { get; }

    public void AddOrUpdatePlayer(LegacyPlayerContext player)
    {
        if (player is null)
        {
            throw new ArgumentNullException(nameof(player));
        }

        lock (_sync)
        {
            _players[player.Code] = player;
        }
    }

    public void RemovePlayer(ushort code)
    {
        lock (_sync)
        {
            _players.Remove(code);
        }
    }

    public IReadOnlyList<LegacyPlayerContext> SnapshotPlayers()
    {
        lock (_sync)
        {
            if (_players.Count == 0)
            {
                return EmptyPlayers;
            }

            var result = new LegacyPlayerContext[_players.Count];
            _players.Values.CopyTo(result, 0);
            return result;
        }
    }

    public IReadOnlyList<LegacyPlayerContext> SnapshotPlayersInArea(byte centerX, byte centerY, int radiusX, int radiusY)
    {
        lock (_sync)
        {
            if (_players.Count == 0)
            {
                return EmptyPlayers;
            }

            var result = new List<LegacyPlayerContext>(_players.Count);
            foreach (var player in _players.Values)
            {
                var snapshot = player.Snapshot;
                if (snapshot.CodigoMapa != MapId)
                {
                    continue;
                }

                var dx = Math.Abs(snapshot.CoordenadaX - centerX);
                var dy = Math.Abs(snapshot.CoordenadaY - centerY);
                if (dx > radiusX || dy > radiusY)
                {
                    continue;
                }

                result.Add(player);
            }

            if (result.Count == 0)
            {
                return EmptyPlayers;
            }

            return result.ToArray();
        }
    }

    public bool TryOccupy(byte x, byte y, ushort code, LegacyTerrainFlags allowedTerrain)
    {
        lock (_sync)
        {
            if (!IsWalkableInternal(x, y, allowedTerrain))
            {
                return false;
            }

            var key = ComposeKey(x, y);
            if (_occupants.TryGetValue(key, out var existing) && existing.Code != code)
            {
                return false;
            }

            _occupants[key] = new Occupant(code);
            return true;
        }
    }

    public void Release(byte x, byte y, ushort code)
    {
        lock (_sync)
        {
            var key = ComposeKey(x, y);
            if (_occupants.TryGetValue(key, out var existing) && existing.Code == code)
            {
                _occupants.Remove(key);
            }
        }
    }

    public bool HasOccupant(byte x, byte y)
    {
        lock (_sync)
        {
            return _occupants.ContainsKey(ComposeKey(x, y));
        }
    }

    public bool IsWalkable(byte x, byte y, LegacyTerrainFlags allowedTerrain) =>
        IsWalkableInternal(x, y, allowedTerrain);

    private bool IsWalkableInternal(byte x, byte y, LegacyTerrainFlags allowedTerrain)
    {
        var terrain = Definition.Grid.GetTerrain(x, y);
        return (terrain & allowedTerrain) != 0;
    }

    private static int ComposeKey(byte x, byte y) => (y << 8) | x;

    private readonly record struct Occupant(ushort Code);
}
