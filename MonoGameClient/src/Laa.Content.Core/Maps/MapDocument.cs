using System.Collections.Generic;

namespace Laa.Content.Core.Maps;

public sealed record MapDocument(
    MapHeader Header,
    IReadOnlyList<IReadOnlyList<byte>> Terrain,
    IReadOnlyList<StaticGraphic> Graphics,
    IReadOnlyList<SensorRecord> Sensors,
    IReadOnlyList<NestRecord> Nests,
    IReadOnlyList<MerchantRecord> Merchants,
    MapExtendedData ExtendedData
);
