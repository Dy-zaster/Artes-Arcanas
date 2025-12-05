namespace LegacyDataExtractor.Models;

public sealed record MapFile(
    MapHeader Header,
    IReadOnlyList<IReadOnlyList<byte>> Terrain,
    IReadOnlyList<StaticGraphic> Graphics,
    IReadOnlyList<SensorRecord> Sensors,
    IReadOnlyList<NestRecord> Nests,
    IReadOnlyList<MerchantRecord> Merchants,
    MapExtendedData ExtendedData
);
