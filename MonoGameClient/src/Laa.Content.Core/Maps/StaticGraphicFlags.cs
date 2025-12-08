namespace Laa.Content.Core.Maps;

[Flags]
public enum StaticGraphicFlags : byte
{
    None = 0,
    Mirror = 0x01,
    NaturalTransparency = 0x02,
    ForcedTransparency = 0x04,
    Illusion = 0x08,
    SensitiveToFlags = 0x20,
    Antialiased = 0x40,
    Levitation = 0x80
}
