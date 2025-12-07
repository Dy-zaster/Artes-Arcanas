namespace ArtesArcanas.Server.Core.Configuration;

public sealed record BasePosition(byte RaceId, byte MapId, byte X, byte Y);

public sealed class ServerOptions
{
    public string WelcomeMessage { get; init; } = string.Empty;
    public string AvatarServer { get; init; } = string.Empty;
    public string IpAddress { get; init; } = string.Empty;
    public int Port { get; init; } = 15715;
    public int HighestMapId { get; init; } = 1;
    public byte SpawnCooldown { get; init; } = 128;
    public bool AllowGlobalCommunication { get; init; } = true;
    public bool AllowMultipleSessions { get; init; } = true;
    public bool KeepLogFile { get; init; } = false;
    public bool TimestampLogEntries { get; init; } = false;
    public bool VerificationMode { get; init; } = false;
    public IReadOnlyList<BasePosition> BasePositions { get; init; } = Array.Empty<BasePosition>();
}
