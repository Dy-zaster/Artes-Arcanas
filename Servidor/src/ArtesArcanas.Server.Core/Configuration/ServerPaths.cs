using System.IO;

namespace ArtesArcanas.Server.Core.Configuration;

public sealed class ServerPaths
{
    private ServerPaths(
        string rootDirectory,
        string legacyDataDirectory,
        string optionsFile,
        string mapDirectory,
        string itemCatalogFile,
        string monsterFile)
    {
        RootDirectory = rootDirectory;
        LegacyDataDirectory = legacyDataDirectory;
        OptionsFile = optionsFile;
        MapDirectory = mapDirectory;
        ItemCatalogFile = itemCatalogFile;
        MonsterFile = monsterFile;
    }

    public string RootDirectory { get; }
    public string LegacyDataDirectory { get; }
    public string OptionsFile { get; }
    public string MapDirectory { get; }
    public string ItemCatalogFile { get; }
    public string MonsterFile { get; }
    public string AnimationMapFile => Path.Combine(MapDirectory, "mp_anim.b");

    public string AvatarDirectory => Path.Combine(LegacyDataDirectory, Legacy.LegacyConstants.AvatarDirectoryName);
    public string AdministratorFile => Path.Combine(LegacyDataDirectory, "admin.dat");
    public string ClanFile => Path.Combine(LegacyDataDirectory, "clanes.dat");
    public string CastlesFile => Path.Combine(LegacyDataDirectory, "castillos.dat");
    public string PricesFile => Path.Combine(LegacyDataDirectory, "precios.dat");
    public string LogFile => Path.Combine(RootDirectory, "server.log");

    public static ServerPaths Discover(string? rootOverride = null, string? legacyOverride = null, string? optionsOverride = null)
    {
        var root = NormalizeDirectory(rootOverride ?? Directory.GetCurrentDirectory());

        var suggestedLegacy = legacyOverride ??
            Path.Combine(root, "..", "Original Pascal", "Servidor");
        var legacy = Directory.Exists(suggestedLegacy) ? NormalizeDirectory(suggestedLegacy) : root;

        var options = optionsOverride ?? Path.Combine(legacy, "opciones.json");
        if (!File.Exists(options))
        {
            throw new FileNotFoundException($"No se encontró el archivo de configuración: {options}");
        }

        var mapDirectory = DiscoverMapDirectory(legacy);
        var itemCatalog = DiscoverItemCatalog(root);
        var monsterFile = DiscoverMonsterFile(mapDirectory);

        return new ServerPaths(root, legacy, Path.GetFullPath(options), mapDirectory, itemCatalog, monsterFile);
    }

    private static string DiscoverMapDirectory(string legacyDataDirectory)
    {
        var candidates = new[]
        {
            Path.Combine(legacyDataDirectory, "..", "Laa", "bin"),
            Path.Combine(legacyDataDirectory, "bin"),
            legacyDataDirectory
        };

        foreach (var candidate in candidates)
        {
            var normalized = NormalizeDirectory(candidate);
            if (Directory.Exists(normalized))
            {
                return normalized;
            }
        }

        return legacyDataDirectory;
    }

    private static string DiscoverItemCatalog(string rootDirectory)
    {
        var candidates = new[]
        {
            Path.Combine(rootDirectory, "..", "MonoGameClient", "content", "data", "items.json"),
            Path.Combine(rootDirectory, "..", "Docs", "Formats", "Samples", "items.json"),
            Path.Combine(rootDirectory, "items.json")
        };

        foreach (var candidate in candidates)
        {
            var fullPath = Path.GetFullPath(candidate);
            if (File.Exists(fullPath))
            {
                return fullPath;
            }
        }

        return Path.GetFullPath(candidates[^1]);
    }

    private static string DiscoverMonsterFile(string legacyMapDirectory)
    {
        var candidate = Path.GetFullPath(Path.Combine(legacyMapDirectory, "std.mon"));
        return candidate;
    }

    private static string NormalizeDirectory(string path)
    {
        var fullPath = Path.GetFullPath(path);
        return fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }
}
