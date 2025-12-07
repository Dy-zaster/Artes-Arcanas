using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ArtesArcanas.Server.Core.Legacy;
using ArtesArcanas.Server.Core.Logging;
using ArtesArcanas.Server.Core.Networking;
using ArtesArcanas.Server.Core.World.Maps;

namespace ArtesArcanas.Server.Core.World;

internal sealed class LegacyWeatherTicker : ILegacyWorldTicker
{
    private const int TicksPerDay = 57600;
    private const int NightStartTick = TicksPerDay / 2;
    private const int CalmBuffer = 512;
    private const int EventMask = 0x7F;

    private readonly LegacySessionManager _sessions;
    private readonly LegacyWorldState _world;
    private readonly IServerLogger _logger;
    private readonly Random _random = new();

    private int _universalTick;
    private LegacyWeatherType _currentType = LegacyWeatherType.Normal;
    private byte _intensity;
    private sbyte _drift;

    public LegacyWeatherTicker(LegacySessionManager sessions, LegacyWorldState world, IServerLogger logger)
    {
        _sessions = sessions;
        _world = world;
        _logger = logger;
        _universalTick = _random.Next(2) == 0 ? 32 : NightStartTick - 32;
    }

    public async ValueTask TickAsync(LegacyWorldState world, CancellationToken cancellationToken)
    {
        await AdvanceCycleAsync().ConfigureAwait(false);
    }

    private async Task AdvanceCycleAsync()
    {
        if (_universalTick == 0)
        {
            await FinalizeWeatherAsync().ConfigureAwait(false);
        }

        var isDay = _universalTick < NightStartTick;

        if (isDay)
        {
            if (_universalTick > CalmBuffer && _universalTick < NightStartTick - CalmBuffer && (_universalTick & EventMask) == 0)
            {
                await HandleDayEventsAsync().ConfigureAwait(false);
            }
            else if (_universalTick == NightStartTick - CalmBuffer)
            {
                await FinalizeWeatherAsync().ConfigureAwait(false);
            }
        }
        else
        {
            if (_universalTick == NightStartTick)
            {
                await StartWeatherAsync(LegacyWeatherType.Night).ConfigureAwait(false);
            }
            else if (_universalTick > NightStartTick + CalmBuffer && _universalTick < TicksPerDay - CalmBuffer && (_universalTick & EventMask) == 0)
            {
                await HandleNightEventsAsync().ConfigureAwait(false);
            }
            else if (_universalTick == TicksPerDay - CalmBuffer)
            {
                await FinalizeWeatherAsync().ConfigureAwait(false);
            }
        }

        UpdateIntensity();
        _world.SetGlobalWeather(_currentType, _intensity, _drift);

        _universalTick++;
        if (_universalTick >= TicksPerDay)
        {
            _universalTick = 0;
        }
    }

    private async Task HandleDayEventsAsync()
    {
        if (_currentType == LegacyWeatherType.Normal)
        {
            var roll = _random.Next(20);
            if (roll == 0)
            {
                await StartWeatherAsync(LegacyWeatherType.Rain).ConfigureAwait(false);
            }
            else if (roll == 1)
            {
                await StartWeatherAsync(LegacyWeatherType.Fog).ConfigureAwait(false);
            }
        }
        else if (_intensity == byte.MaxValue)
        {
            if (_random.Next(4) == 0)
            {
                await FinalizeWeatherAsync().ConfigureAwait(false);
            }
            else if (_currentType == LegacyWeatherType.Rain && _random.Next(8) == 0)
            {
                await TriggerThunderAsync().ConfigureAwait(false);
            }
        }
    }

    private async Task HandleNightEventsAsync()
    {
        if (_currentType == LegacyWeatherType.Night)
        {
            if (_random.Next(20) == 0)
            {
                await StartWeatherAsync(LegacyWeatherType.RainNight).ConfigureAwait(false);
            }
        }
        else if (_intensity == byte.MaxValue)
        {
            if (_random.Next(9) == 0)
            {
                await FinalizeWeatherAsync().ConfigureAwait(false);
            }
            else if (_currentType == LegacyWeatherType.RainNight && _random.Next(2) == 0)
            {
                await TriggerThunderAsync().ConfigureAwait(false);
            }
        }
    }

    private async Task StartWeatherAsync(LegacyWeatherType type)
    {
        if (_currentType == type)
        {
            return;
        }

        _currentType = type;
        _intensity = 0;
        _drift = GetDrift(type);
        await BroadcastWeatherAsync(type).ConfigureAwait(false);
    }

    private async Task FinalizeWeatherAsync()
    {
        if (_currentType == LegacyWeatherType.Normal || _drift < 0)
        {
            return;
        }

        _drift = (sbyte)-GetDrift(_currentType);
        await BroadcastTerminationAsync().ConfigureAwait(false);
    }

    private async Task BroadcastWeatherAsync(LegacyWeatherType type)
    {
        var payload = new[] { (byte)'X', (byte)type };
        await BroadcastPerPlayerAsync(payload, type).ConfigureAwait(false);
    }

    private Task BroadcastTerminationAsync()
    {
        var payload = new[] { (byte)'X', (byte)'T' };
        return SafeBroadcastAsync(payload);
    }

    private async Task TriggerThunderAsync()
    {
        var payload = new[] { (byte)'X', (byte)'R' };
        await BroadcastPerPlayerAsync(payload, _currentType).ConfigureAwait(false);
    }

    private async Task BroadcastPerPlayerAsync(ReadOnlyMemory<byte> payload, LegacyWeatherType type)
    {
        var players = _world.GetPlayers();
        if (players.Count == 0)
        {
            return;
        }

        var tasks = new List<Task>(players.Count);
        foreach (var player in players)
        {
            if (!ShouldApplyWeather(player.Snapshot.CodigoMapa, type))
            {
                continue;
            }

            tasks.Add(_sessions.SendToSessionAsync(player.Code, payload));
        }

        if (tasks.Count == 0)
        {
            return;
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private Task SafeBroadcastAsync(ReadOnlyMemory<byte> payload)
    {
        try
        {
            return _sessions.BroadcastAsync(payload);
        }
        catch (Exception ex)
        {
            _logger.Error("Error difundiendo cambio global de clima.", ex);
            return Task.CompletedTask;
        }
    }

    private bool ShouldApplyWeather(byte mapId, LegacyWeatherType type)
    {
        if (!_world.TryGetMapDefinition(mapId, out var definition) || definition is null)
        {
            return true;
        }

        var flags = definition.MapFlags;
        if ((flags & LegacyMapFlags.Indoor) != 0)
        {
            return false;
        }

        if ((type == LegacyWeatherType.Rain || type == LegacyWeatherType.RainNight) &&
            (flags & LegacyMapFlags.NoRain) != 0)
        {
            return false;
        }

        if (type == LegacyWeatherType.Fog && (flags & LegacyMapFlags.NoFog) != 0)
        {
            return false;
        }

        return true;
    }

    private void UpdateIntensity()
    {
        if (_drift == 0)
        {
            return;
        }

        var next = _intensity + _drift;
        if (next <= 0)
        {
            _drift = 0;
            if (_currentType == LegacyWeatherType.RainNight)
            {
                _currentType = LegacyWeatherType.Night;
                _intensity = byte.MaxValue;
            }
            else
            {
                _currentType = LegacyWeatherType.Normal;
                _intensity = 0;
            }
        }
        else if (next >= byte.MaxValue)
        {
            _intensity = byte.MaxValue;
            _drift = 0;
        }
        else
        {
            _intensity = (byte)next;
        }
    }

    private static sbyte GetDrift(LegacyWeatherType type) =>
        type switch
        {
            LegacyWeatherType.Rain => 4,
            LegacyWeatherType.RainNight => 4,
            LegacyWeatherType.Fog => 1,
            LegacyWeatherType.Night => 1,
            _ => 1
        };
}
