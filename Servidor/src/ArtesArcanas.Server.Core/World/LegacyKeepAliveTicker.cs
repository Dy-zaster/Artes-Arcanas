using ArtesArcanas.Server.Core.Logging;
using ArtesArcanas.Server.Core.Networking;

namespace ArtesArcanas.Server.Core.World;

public sealed class LegacyKeepAliveTicker : ILegacyWorldTicker
{
    private readonly LegacySessionManager _sessions;
    private readonly IServerLogger _logger;
    private readonly TimeSpan _threshold;
    private readonly byte[] _keepAlivePayload = { (byte)'I', 0 };

    public LegacyKeepAliveTicker(LegacySessionManager sessions, IServerLogger logger, TimeSpan? threshold = null)
    {
        _sessions = sessions;
        _logger = logger;
        _threshold = threshold is { } value && value > TimeSpan.Zero ? value : TimeSpan.FromMinutes(1);
    }

    public async ValueTask TickAsync(LegacyWorldState world, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var players = world.GetPlayers();
        foreach (var player in players)
        {
            if (player.KeepAliveWarningSent)
            {
                continue;
            }

            if (now - player.LastActivityUtc < _threshold)
            {
                continue;
            }

            try
            {
                await _sessions.SendToSessionAsync(player.Code, _keepAlivePayload).ConfigureAwait(false);
                player.MarkKeepAliveWarningSent();
            }
            catch (Exception ex)
            {
                _logger.Error($"[{player.Code}] Error enviando keep-alive.", ex);
            }
        }
    }
}
