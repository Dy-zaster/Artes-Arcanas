namespace Laa.Content.Core.Maps;

public sealed record MapHeader(
    string Name,
    ushort GraphicCount,
    ushort Flags,
    byte NorthMap,
    byte SouthMap,
    byte EastMap,
    byte WestMap,
    byte Version,
    byte NestCount,
    byte MerchantCount,
    byte SensorCount,
    byte ExtendedBytes
);
