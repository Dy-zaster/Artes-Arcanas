using ArtesArcanas.Server.Core.Configuration;
using ArtesArcanas.Server.Core.Legacy;
using ArtesArcanas.Server.Core.Logging;
using ArtesArcanas.Server.Core.World.Maps;

namespace ArtesArcanas.Server.Core.Runtime;

public sealed class ServerBuilder
{
    private string? _rootPath;
    private string? _legacyPath;
    private string? _optionsFile;

    private ServerBuilder()
    {
    }

    public static ServerBuilder CreateDefault() => new();

    public ServerBuilder UseRootPath(string path)
    {
        _rootPath = path;
        return this;
    }

    public ServerBuilder UseLegacyDataPath(string path)
    {
        _legacyPath = path;
        return this;
    }

    public ServerBuilder UseOptionsFile(string path)
    {
        _optionsFile = path;
        return this;
    }

    public ServerHost Build()
    {
        var paths = ServerPaths.Discover(_rootPath, _legacyPath, _optionsFile);
        var options = ServerOptionsLoader.Load(paths.OptionsFile);
        var logger = new ServerLogger(options, paths);
        var admins = LegacyAdministratorRegistry.Load(paths.AdministratorFile);
        var clans = LegacyClanStorage.Load(paths.ClanFile);
        var castles = LegacyCastleStorage.Load(paths.CastlesFile);
        var prices = LegacyPriceStorage.Load(paths.PricesFile);
        var itemCatalog = LegacyItemCatalog.Load(paths.ItemCatalogFile);
        var monsters = LegacyMonsterCatalog.Load(paths.MonsterFile);
        var animationMap = LegacyAnimationMap.Load(paths.AnimationMapFile);
        var data = new LegacyGameData(admins, clans, castles, prices, itemCatalog, monsters, animationMap);
        var accountStorage = new LegacyAccountStorage(paths.AvatarDirectory);
        var mapLoader = new LegacyMapLoader(paths.MapDirectory, logger);
        var mapManager = new LegacyMapManager(mapLoader.LoadMaps(options.HighestMapId));

        return new ServerHost(options, paths, data, accountStorage, logger, mapManager);
    }
}
