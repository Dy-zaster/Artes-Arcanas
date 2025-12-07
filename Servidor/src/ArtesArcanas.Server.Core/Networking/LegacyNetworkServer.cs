using System.Net;
using System.Net.Sockets;
using ArtesArcanas.Server.Core.Configuration;
using ArtesArcanas.Server.Core.Legacy;
using ArtesArcanas.Server.Core.Logging;
using ArtesArcanas.Server.Core.World;

namespace ArtesArcanas.Server.Core.Networking;

public sealed class LegacyNetworkServer : IAsyncDisposable
{
    private readonly ServerOptions _options;
    private readonly IServerLogger _logger;
    private readonly LegacyWorldState _world;
    private readonly LegacyGameData _gameData;
    private readonly LegacyAccountStorage _accounts;
    private readonly ILegacyCommandSink _commandSink;
    private readonly LegacySessionManager _sessionManager;

    private TcpListener? _listener;
    private CancellationTokenSource? _cts;
    private Task? _acceptLoop;
    private readonly List<Task> _connections = new();

    public LegacyNetworkServer(ServerOptions options, IServerLogger logger, LegacyWorldState world, LegacyGameData data, LegacyAccountStorage accounts, ILegacyCommandSink commandSink, LegacySessionManager sessionManager)
    {
        _options = options;
        _logger = logger;
        _world = world;
        _gameData = data;
        _accounts = accounts;
        _commandSink = commandSink;
        _sessionManager = sessionManager;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_listener is not null)
        {
            return;
        }

        var ipAddress = ResolveAddress(_options.IpAddress);
        _listener = new TcpListener(ipAddress, _options.Port);
        _listener.Start();
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _acceptLoop = Task.Run(() => AcceptLoopAsync(_cts.Token), cancellationToken);
        _logger.Info($"Servidor TCP escuchando en {ipAddress}:{_options.Port}");
        await Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_listener is null)
        {
            return;
        }

        _cts?.Cancel();
        _listener.Stop();
        if (_acceptLoop is not null)
        {
            await _acceptLoop.ConfigureAwait(false);
        }

        await Task.WhenAll(_connections.ToArray()).ConfigureAwait(false);
        _connections.Clear();
        _listener = null;
    }

    private async Task AcceptLoopAsync(CancellationToken token)
    {
        if (_listener is null)
        {
            return;
        }

        try
        {
            while (!token.IsCancellationRequested)
            {
                TcpClient client;
                try
                {
                    client = await _listener.AcceptTcpClientAsync(token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                var task = Task.Run(() => HandleClientAsync(client, token), token);
                lock (_connections)
                {
                    _connections.Add(task);
                }

                _ = task.ContinueWith(t =>
                {
                    lock (_connections)
                    {
                        _connections.Remove(t);
                    }
                }, TaskScheduler.Default);
            }
        }
        finally
        {
            _logger.Info("Bucle de aceptación detenido.");
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken token)
    {
        var remote = client.Client.RemoteEndPoint as IPEndPoint ?? new IPEndPoint(IPAddress.None, 0);
        if (!_world.TryReserveSlot(remote, out var code))
        {
            _logger.Warning($"Conexión rechazada ({remote}) – sin slots disponibles.");
            await SendServerFullAsync(client).ConfigureAwait(false);
            client.Dispose();
            return;
        }

        _logger.Info($"[{code}] Conexión entrante desde {remote}.");

        await using var context = new LegacyConnectionContext(
            client,
            client.GetStream(),
            _world,
            _gameData,
            _accounts,
            _options,
            _commandSink,
            _sessionManager,
            _logger,
            token,
            code);

        try
        {
            await context.RunAsync(token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (!token.IsCancellationRequested)
            {
                _logger.Error($"[{code}] Error en la conexión.", ex);
            }
        }
    }

    private static async Task SendServerFullAsync(TcpClient client)
    {
        try
        {
            var payload = new byte[] { (byte)'E', (byte)'S' };
            await client.GetStream().WriteAsync(payload).ConfigureAwait(false);
        }
        catch
        {
            // ignore
        }
    }

    private static IPAddress ResolveAddress(string preferred)
    {
        if (!string.IsNullOrWhiteSpace(preferred) && Legacy.LegacySecurity.SeemsIp(preferred))
        {
            if (IPAddress.TryParse(preferred, out var parsed))
            {
                return parsed;
            }
        }

        return IPAddress.Any;
    }

    public async ValueTask DisposeAsync()
    {
        if (_listener is not null)
        {
            await StopAsync(CancellationToken.None).ConfigureAwait(false);
        }
    }
}
