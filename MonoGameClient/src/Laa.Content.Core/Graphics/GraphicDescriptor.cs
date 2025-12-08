namespace Laa.Content.Core.Graphics;

public sealed record GraphicDescriptor(
    short PosX,
    short PosY,
    IReadOnlyList<byte> OccupiedMask,
    IReadOnlyList<byte> HiddenMask,
    byte AlignY,
    byte Unused0,
    byte Type,
    byte SubLayer,
    ushort Reserved,
    byte ResourceEffect,
    byte Flags,
    short ReflectedPosX
);
