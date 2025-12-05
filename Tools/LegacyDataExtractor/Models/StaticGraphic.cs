namespace LegacyDataExtractor.Models;

public sealed record StaticGraphic(
    ushort CodeFlags,
    byte X,
    byte Y,
    byte Flags,
    byte SubLayer
);
