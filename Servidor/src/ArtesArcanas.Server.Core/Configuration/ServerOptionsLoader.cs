using System.Text.Json;

namespace ArtesArcanas.Server.Core.Configuration;

public static class ServerOptionsLoader
{
    public static ServerOptions Load(string optionsFile)
    {
        using var stream = File.OpenRead(optionsFile);
        var json = JsonSerializer.Deserialize<JsonServerOptions>(stream, JsonHelpers.Options);
        if (json is null)
        {
            throw new InvalidOperationException($"No se pudo deserializar la configuración: {optionsFile}");
        }

        var basePositions = json.BasePositions?.Select(
                bp => new BasePosition(bp.RaceId, bp.MapId, bp.X, bp.Y))
            .ToArray() ?? Array.Empty<BasePosition>();

        return new ServerOptions
        {
            WelcomeMessage = json.WelcomeMessage ?? string.Empty,
            AvatarServer = json.AvatarServer ?? string.Empty,
            IpAddress = json.IpAddress ?? string.Empty,
            Port = json.Port,
            HighestMapId = json.HighestMapId,
            SpawnCooldown = json.SpawnCooldown,
            AllowGlobalCommunication = json.AllowGlobalCommunication,
            AllowMultipleSessions = json.AllowMultipleSessions,
            KeepLogFile = json.KeepLogFile,
            TimestampLogEntries = json.TimestampLogEntries,
            BasePositions = basePositions
        };
    }

    private sealed record JsonServerOptions
    {
        public string? WelcomeMessage { get; init; }
        public string? AvatarServer { get; init; }
        public string? IpAddress { get; init; }
        public int Port { get; init; } = 15715;
        public int HighestMapId { get; init; } = 1;
        public byte SpawnCooldown { get; init; } = 128;
        public bool AllowGlobalCommunication { get; init; } = true;
        public bool AllowMultipleSessions { get; init; } = true;
        public bool KeepLogFile { get; init; }
        public bool TimestampLogEntries { get; init; }
        public JsonBasePosition[]? BasePositions { get; init; }
    }

    private sealed record JsonBasePosition(byte RaceId, byte MapId, byte X, byte Y);

    private static class JsonHelpers
    {
        public static readonly JsonSerializerOptions Options = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
    }
}
