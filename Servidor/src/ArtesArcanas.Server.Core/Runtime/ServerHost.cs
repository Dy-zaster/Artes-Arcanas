using System.Text;
using System.Linq;
using ArtesArcanas.Server.Core.Configuration;
using ArtesArcanas.Server.Core.Legacy;
using ArtesArcanas.Server.Core.Logging;
using ArtesArcanas.Server.Core.Networking;
using ArtesArcanas.Server.Core.World;
using ArtesArcanas.Server.Core.World.Maps;

namespace ArtesArcanas.Server.Core.Runtime;

public sealed class ServerHost : IAsyncDisposable
{
    private readonly ServerOptions _options;
    private readonly ServerPaths _paths;
    private readonly LegacyGameData _gameData;
    private readonly LegacyAccountStorage _accounts;
    private readonly IServerLogger _logger;
    private readonly LegacyWorldState _world;
    private readonly LegacySessionManager _sessionManager;
    private readonly LegacyCommandRouter _commandRouter;
    private readonly LegacyCommandDispatcher _commandDispatcher;
    private readonly ILegacyWorldActionHandler _worldActionHandler;
    private readonly LegacyWorldLoop _worldLoop;
    private readonly LegacyNetworkServer _network;
    private readonly LegacyMapManager _mapManager;
    private readonly LegacyMapSurface _mapSurface;

    public ServerHost(
        ServerOptions options,
        ServerPaths paths,
        LegacyGameData data,
        LegacyAccountStorage accounts,
        IServerLogger logger,
        LegacyMapManager mapManager)
    {
        _options = options;
        _paths = paths;
        _gameData = data;
        _accounts = accounts;
        _logger = logger;
        _mapManager = mapManager;
        _mapSurface = new LegacyMapSurface(mapManager);
        _world = new LegacyWorldState(_mapSurface, mapManager, data.Monsters, logger);
        _sessionManager = new LegacySessionManager();
        _commandRouter = new LegacyCommandRouter(logger);
        _commandDispatcher = new LegacyCommandDispatcher(_commandRouter, _world, logger);
        _worldActionHandler = new LegacyWorldActionSink(logger, _sessionManager, _world, _gameData, _mapSurface);
        var tickers = new ILegacyWorldTicker[]
        {
            new LegacyKeepAliveTicker(_sessionManager, logger, TimeSpan.FromMinutes(1)),
            new LegacyIdleTimeoutTicker(_sessionManager, logger, TimeSpan.FromMinutes(3), TimeSpan.FromMinutes(5)),
            new LegacyMonsterTicker(_world, _sessionManager),
            new LegacyWeatherTicker(_sessionManager, _world, logger)
        };
        _worldLoop = new LegacyWorldLoop(_world, _worldActionHandler, logger, tickers);
        _network = new LegacyNetworkServer(options, logger, _world, _gameData, accounts, _commandRouter, _sessionManager);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.Info("Iniciando servidor Artes Arcanas (.NET 8).");
        LogConfiguration();
        LogAdministrators();
        LogClans();
        LogCastles();
        LogPrices();
        LogMaps();
        LogItems();
        LogMonsters();

        _commandDispatcher.Start();
        _worldLoop.Start();
        await _network.StartAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.Info("Servidor detenido.");
        _commandRouter.Complete();
        await _worldLoop.StopAsync().ConfigureAwait(false);
        await _commandDispatcher.StopAsync().ConfigureAwait(false);
        await _network.StopAsync(cancellationToken).ConfigureAwait(false);
    }

    private void LogConfiguration()
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Archivo de opciones: {_paths.OptionsFile}");
        builder.AppendLine($"Directorio Legacy: {_paths.LegacyDataDirectory}");
        builder.AppendLine($"Directorio de mapas: {_paths.MapDirectory}");
        builder.AppendLine($"Puerto TCP: {_options.Port}");
        builder.AppendLine($"IP preferida: {_options.IpAddress}");
        builder.AppendLine($"Mapas habilitados: 0..{_options.HighestMapId}");
        builder.AppendLine($"Turnos entre spawn de criaturas: {_options.SpawnCooldown}");
        builder.AppendLine($"Mensaje de bienvenida: {_options.WelcomeMessage}");
        builder.AppendLine($"Servidor de avatares: {_options.AvatarServer}");
        builder.AppendLine($"Chat global: {(_options.AllowGlobalCommunication ? "sí" : "no")}");
        builder.AppendLine($"Sesiones múltiples: {(_options.AllowMultipleSessions ? "sí" : "no")}");
        builder.AppendLine($"Registro persistente: {(_options.KeepLogFile ? "sí" : "no")}");
        builder.AppendLine($"Timestamp en registro: {(_options.TimestampLogEntries ? "sí" : "no")}");
        builder.AppendLine($"Posiciones base configuradas: {_options.BasePositions.Count}");

        _logger.Info(builder.ToString());
    }

    private void LogAdministrators()
    {
        if (_gameData.Administrators.Administrators.Count == 0)
        {
            _logger.Warning("No se encontraron administradores registrados (admin.dat).");
            return;
        }

        var entries = string.Join(", ",
            _gameData.Administrators.Administrators.Select(a => $"{a.Login} ({a.State})"));
        _logger.Info($"Administradores cargados: {entries}");
    }

    private void LogClans()
    {
        if (_gameData.Clans.Clans.Count == 0)
        {
            _logger.Info("No se encontraron clanes activos (clanes.dat).");
            return;
        }

        var sample = _gameData.Clans.Clans.First();
        _logger.Info($"Clanes activos: {_gameData.Clans.Clans.Count} (p.ej. {sample.Name} liderado por {sample.Leader}).");
    }

    private void LogCastles()
    {
        if (_gameData.Castles.Castles.Count == 0)
        {
            _logger.Info("No se encontraron registros de castillos (castillos.dat).");
            return;
        }

        var owned = _gameData.Castles.Castles.Count(c => c.ClanId <= LegacyConstants.MaxClans);
        _logger.Info($"Castillos cargados: {_gameData.Castles.Castles.Count}. Controlados por clanes: {owned}.");
    }

    private void LogPrices()
    {
        if (_gameData.Prices.Inflations.Count == 0)
        {
            _logger.Info("No se encontraron precios personalizados (precios.dat).");
            return;
        }

        var merchants = _gameData.Prices.Inflations.Sum(static entry => entry.Value.Count);
        _logger.Info($"Tablas de precios cargadas para {_gameData.Prices.Inflations.Count} mapas ({merchants} comerciantes).");
    }

    private void LogMaps()
    {
        if (_mapManager.Count == 0)
        {
            _logger.Warning("No se pudieron cargar mapas legacy (.mpv).");
            return;
        }

        _logger.Info($"Mapas cargados: {_mapManager.Count} archivos mpv.");
    }

    private void LogItems()
    {
        var catalog = _gameData.Items;
        _logger.Info($"Catálogo de objetos cargado: {catalog.Count} entradas (checksum {catalog.Checksum}).");
    }

    private void LogMonsters()
    {
        var monsters = _gameData.Monsters;
        if (monsters.Count == 0)
        {
            _logger.Warning("No se encontraron definiciones de monstruos (std.mon).");
            return;
        }

        _logger.Info($"Catálogo de monstruos cargado: {monsters.Count} entradas (std.mon).");
    }

    public async ValueTask DisposeAsync()
    {
        await _network.DisposeAsync().ConfigureAwait(false);
        await _commandDispatcher.DisposeAsync().ConfigureAwait(false);
        await _commandRouter.DisposeAsync().ConfigureAwait(false);
        await _worldLoop.DisposeAsync().ConfigureAwait(false);

        if (_logger is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
