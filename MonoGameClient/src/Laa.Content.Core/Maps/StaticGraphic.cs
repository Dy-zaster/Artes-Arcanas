namespace Laa.Content.Core.Maps;

public sealed record StaticGraphic(
    ushort CodeFlags,
    byte X,
    byte Y,
    byte Flags,
    byte SubLayer
);
