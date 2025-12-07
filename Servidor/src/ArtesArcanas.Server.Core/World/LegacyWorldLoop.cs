using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ArtesArcanas.Server.Core.Logging;

namespace ArtesArcanas.Server.Core.World;

public sealed class LegacyWorldLoop : IAsyncDisposable
{
    private static readonly TimeSpan DefaultInterval = TimeSpan.FromMilliseconds(100);

    private readonly LegacyWorldState _world;
    private readonly ILegacyWorldActionHandler _actionHandler;
    private readonly IReadOnlyList<ILegacyWorldTicker> _tickers;
    private readonly IServerLogger _logger;
    private readonly TimeSpan _tickInterval;

    private CancellationTokenSource? _cts;
    private Task? _loopTask;

    public LegacyWorldLoop(
        LegacyWorldState world,
        ILegacyWorldActionHandler actionHandler,
        IServerLogger logger,
        IEnumerable<ILegacyWorldTicker>? tickers = null,
        TimeSpan? tickInterval = null)
    {
        _world = world;
        _actionHandler = actionHandler;
        _tickers = tickers?.ToArray() ?? Array.Empty<ILegacyWorldTicker>();
        _logger = logger;
        _tickInterval = tickInterval is { } interval && interval > TimeSpan.Zero ? interval : DefaultInterval;
    }

    public void Start()
    {
        if (_loopTask is not null)
        {
            return;
        }

        _cts = new CancellationTokenSource();
        _loopTask = Task.Run(() => RunAsync(_cts.Token), CancellationToken.None);
    }

    public async Task StopAsync()
    {
        if (_cts is not null)
        {
            _cts.Cancel();
            _cts = null;
        }

        if (_loopTask is not null)
        {
            try
            {
                await _loopTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // ignored
            }
            finally
            {
                _loopTask = null;
            }
        }
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(_tickInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                await TickAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // cancellation expected when stopping
        }
    }

    private async Task TickAsync(CancellationToken cancellationToken)
    {
        var players = _world.GetPlayers();
        foreach (var player in players)
        {
            var pending = RentBuffer();
            try
            {
                DrainPlayerActions(player, pending);
                if (pending.Count == 0)
                {
                    continue;
                }

                await _actionHandler.HandleAsync(player, pending, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.Error($"[{player.Code}] Error procesando acciones del mundo.", ex);
            }
            finally
            {
                ReturnBuffer(pending);
            }
        }

        foreach (var ticker in _tickers)
        {
            try
            {
                await ticker.TickAsync(_world, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.Error("Error en ticker del mundo legacy.", ex);
            }
        }
    }

    private static void DrainPlayerActions(LegacyPlayerContext player, List<LegacyPlayerAction> buffer)
    {
        buffer.Clear();
        while (player.TryDequeueAction(out var action))
        {
            buffer.Add(action);
        }
    }

    private static List<LegacyPlayerAction> RentBuffer() => new(capacity: 16);

    private static void ReturnBuffer(List<LegacyPlayerAction> buffer)
    {
        buffer.Clear();
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
    }
}
