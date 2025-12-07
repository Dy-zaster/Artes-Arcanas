using System;
using System.Collections.Generic;
using ArtesArcanas.Server.Core.World;

namespace ArtesArcanas.Server.Core.World.Maps;

public sealed class LegacyMapDefinition
{
    private readonly byte[] _baseTerrain;

    public LegacyMapDefinition(
        byte id,
        string name,
        ushort flags,
        byte northMap,
        byte southMap,
        byte eastMap,
        byte westMap,
        byte version,
        byte sensorCount,
        byte nestCount,
        byte merchantCount,
        ushort graphicCount,
        byte[] baseTerrain,
        LegacyMapExtendedData extendedData,
        LegacyMapGrid grid,
        IReadOnlyList<LegacyMapMerchantDefinition> merchants,
        IReadOnlyList<LegacyMapNestDefinition> nests,
        IReadOnlyList<LegacyMapSensorDefinition> sensors)
    {
        Id = id;
        Name = name;
        Flags = flags;
        NorthMap = northMap;
        SouthMap = southMap;
        EastMap = eastMap;
        WestMap = westMap;
        Version = version;
        SensorCount = sensorCount;
        NestCount = nestCount;
        MerchantCount = merchantCount;
        GraphicCount = graphicCount;
        _baseTerrain = baseTerrain;
        ExtendedData = extendedData;
        Grid = grid;
        Merchants = merchants;
        Nests = nests;
        Sensors = sensors;
        _sensorLookup = BuildSensorLookup(sensors);
    }

    public byte Id { get; }
    public string Name { get; }
    public ushort Flags { get; }
    public byte NorthMap { get; }
    public byte SouthMap { get; }
    public byte EastMap { get; }
    public byte WestMap { get; }
    public byte Version { get; }
    public byte SensorCount { get; }
    public byte NestCount { get; }
    public byte MerchantCount { get; }
    public ushort GraphicCount { get; }
    public ReadOnlyMemory<byte> BaseTerrain => _baseTerrain;
    public LegacyMapExtendedData ExtendedData { get; }
    public LegacyMapGrid Grid { get; }
    public IReadOnlyList<LegacyMapMerchantDefinition> Merchants { get; }
    public IReadOnlyList<LegacyMapNestDefinition> Nests { get; }
    public IReadOnlyList<LegacyMapSensorDefinition> Sensors { get; }
    public LegacyMapFlags MapFlags => (LegacyMapFlags)Flags;

    public byte RespawnMap => ExtendedData.RespawnMap;
    public byte RespawnX => ExtendedData.RespawnX;
    public byte RespawnY => ExtendedData.RespawnY;

    private readonly Dictionary<int, LegacyMapSensorDefinition> _sensorLookup;

    public bool TryGetSensor(byte x, byte y, out LegacyMapSensorDefinition? sensor)
    {
        var key = (y << 8) | x;
        if (_sensorLookup.TryGetValue(key, out var existing))
        {
            sensor = existing;
            return true;
        }

        sensor = null;
        return false;
    }

    private static Dictionary<int, LegacyMapSensorDefinition> BuildSensorLookup(IReadOnlyList<LegacyMapSensorDefinition> sensors)
    {
        if (sensors.Count == 0)
        {
            return new Dictionary<int, LegacyMapSensorDefinition>();
        }

        var lookup = new Dictionary<int, LegacyMapSensorDefinition>(sensors.Count);
        foreach (var sensor in sensors)
        {
            var key = (sensor.Y << 8) | sensor.X;
            lookup[key] = sensor;
        }

        return lookup;
    }
}

public sealed class LegacyMapExtendedData
{
    public LegacyMapExtendedData(
        byte respawnX,
        byte respawnY,
        byte respawnMap,
        int initialFlags,
        int autoResetFlags,
        byte[] flagBehaviors,
        byte[] flagDataSet1,
        byte[] flagDataSet2)
    {
        RespawnX = respawnX;
        RespawnY = respawnY;
        RespawnMap = respawnMap;
        InitialFlags = initialFlags;
        AutoResetFlags = autoResetFlags;
        FlagBehaviors = flagBehaviors;
        FlagDataSet1 = flagDataSet1;
        FlagDataSet2 = flagDataSet2;
    }

    public byte RespawnX { get; }
    public byte RespawnY { get; }
    public byte RespawnMap { get; }
    public int InitialFlags { get; }
    public int AutoResetFlags { get; }
    public IReadOnlyList<byte> FlagBehaviors { get; }
    public IReadOnlyList<byte> FlagDataSet1 { get; }
    public IReadOnlyList<byte> FlagDataSet2 { get; }
}

public sealed record LegacyMapMerchantDefinition(
    byte Type,
    byte X,
    byte Y,
    byte MonsterId,
    IReadOnlyList<LegacyInventorySlot> Items,
    IReadOnlyList<byte> Inflation);

public sealed record LegacyMapNestDefinition(
    byte Type,
    byte X,
    byte Y,
    byte Quantity);

public sealed record LegacyMapSensorDefinition(
    LegacySensorType Type,
    byte X,
    byte Y,
    byte Key1,
    byte Key2,
    byte Data1,
    byte Data2,
    byte Data3,
    byte Data4,
    LegacySensorFlags Flags,
    string Text);

[Flags]
public enum LegacyMapFlags : ushort
{
    None = 0,
    Indoor = 0x0010,
    NoRain = 0x0020,
    NoFog = 0x0040,
    AlwaysNight = 0x0080
}

public enum LegacySensorType : byte
{
    PhysicalRegeneration = 0,
    ManaRegeneration = 1,
    Resurrection = 2,
    Portal = 3,
    SwapItem = 4,
    SetFlag = 5,
    ClearFlag = 6,
    ClanBanner = 7,
    Reserved = 8,
    FoundClan = 9
}

[Flags]
public enum LegacySensorFlags : byte
{
    None = 0,
    ConsumeKey = 0x01,
    RepelAvatar = 0x02,
    SoloClan = 0x10,
    SoloApprentice = 0x20,
    SoloGhost = 0x40,
    PartOfCastle = 0x80
}

[Flags]
public enum LegacyTerrainFlags : ushort
{
    None = 0,
    Covered = 0x0400,
    Soft = 0x0800,
    Solid = 0x1000,
    Wilderness = 0x2000,
    Fire = 0x4000,
    Water = 0x8000
}
