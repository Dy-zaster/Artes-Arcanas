namespace LegacyDataExtractor.Models;

public sealed record MapExtendedData(
    byte RespawnX,
    byte RespawnY,
    byte RespawnMap,
    int DungeonFlags,
    int AutoResetFlags,
    IReadOnlyList<byte> FlagBehavior,
    IReadOnlyList<byte> FlagData1,
    IReadOnlyList<byte> FlagData2,
    IReadOnlyList<byte> DetectorBehavior,
    IReadOnlyList<byte> DetectorData1,
    IReadOnlyList<byte> DetectorData2,
    IReadOnlyList<int> FlagsToDetect,
    IReadOnlyList<int> ExpectedFlagStates
)
{
    public static MapExtendedData Empty { get; } = new(
        0,
        0,
        0,
        0,
        0,
        Array.Empty<byte>(),
        Array.Empty<byte>(),
        Array.Empty<byte>(),
        Array.Empty<byte>(),
        Array.Empty<byte>(),
        Array.Empty<byte>(),
        Array.Empty<int>(),
        Array.Empty<int>()
    );
}
