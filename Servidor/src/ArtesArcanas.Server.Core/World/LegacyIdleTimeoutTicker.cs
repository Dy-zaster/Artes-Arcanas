using ArtesArcanas.Server.Core.Logging;
using ArtesArcanas.Server.Core.Networking;

namespace ArtesArcanas.Server.Core.World;

public sealed class LegacyIdleTimeoutTicker : ILegacyWorldTicker
{
    private readonly LegacySessionManager _sessions;
    private readonly IServerLogger _logger;
    private readonly TimeSpan _kickThreshold;
    private readonly TimeSpan _warningThreshold;

    public LegacyIdleTimeoutTicker(LegacySessionManager sessions, IServerLogger logger, TimeSpan? warningThreshold = null, TimeSpan? kickThreshold = null)
    {
        _sessions = sessions;
        _logger = logger;
        _warningThreshold = warningThreshold ?? TimeSpan.FromMinutes(3);
        _kickThreshold = kickThreshold ?? TimeSpan.FromMinutes(5);
    }

    public ValueTask TickAsync(LegacyWorldState world, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var players = world.GetPlayers();

        foreach (var player in players)
        {
            var idle = now - player.LastActivityUtc;
            if (idle < _warningThreshold)
            {
                continue;
            }

            if (idle >= _kickThreshold)
            {
                if (world.TryGetPlayer(player.Code, out _))
                {
                    _logger.Warning($"[{player.Code}] Desconectado por inactividad ({idle.TotalSeconds:F0}s).");
                    try
                    {
                        _sessions.Unregister(player.Code);
                    }
                    catch
                    {
                        // ignore
                    }
                }
                continue;
            }

            if (!player.KeepAliveWarningSent)
            {
                _ = _sessions.SendToSessionAsync(player.Code, new byte[] { (byte)'I', 0 });
                player.MarkKeepAliveWarningSent();
            }
        }

        return ValueTask.CompletedTask;
    }
}
