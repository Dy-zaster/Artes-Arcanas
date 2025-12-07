using ArtesArcanas.Server.Core.Logging;
using ArtesArcanas.Server.Core.World;

namespace ArtesArcanas.Server.Core.Networking;

public sealed class LegacyCommandDispatcher : IAsyncDisposable
{
    private readonly LegacyCommandRouter _router;
    private readonly IServerLogger _logger;
    private readonly LegacyCommandInterpreter _interpreter;
    private CancellationTokenSource? _cts;
    private Task? _loop;

    public LegacyCommandDispatcher(LegacyCommandRouter router, LegacyWorldState world, IServerLogger logger)
    {
        _router = router;
        _logger = logger;
        _interpreter = new LegacyCommandInterpreter(world, logger);
    }

    public void Start()
    {
        if (_loop is not null)
        {
            return;
        }

        _cts = new CancellationTokenSource();
        _loop = Task.Run(() => LoopAsync(_cts.Token), _cts.Token);
    }

    public async Task StopAsync()
    {
        if (_cts is not null)
        {
            _cts.Cancel();
            _cts = null;
        }

        if (_loop is not null)
        {
            try
            {
                await _loop.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // ignored
            }

            _loop = null;
        }
    }

    private async Task LoopAsync(CancellationToken token)
    {
        await foreach (var command in _router.ReadAllAsync(token).ConfigureAwait(false))
        {
            try
            {
                _interpreter.Append(command);
            }
            catch (Exception ex)
            {
                _logger.Error($"Error interpretando comando legacy de la sesión {command.SessionCode}", ex);
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
    }
}
