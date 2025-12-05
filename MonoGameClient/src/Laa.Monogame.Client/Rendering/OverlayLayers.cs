using System;

namespace Laa.Monogame.Client.Rendering;

[Flags]
public enum OverlayLayers
{
    None = 0,
    Sensors = 1 << 0,
    Nests = 1 << 1,
    Merchants = 1 << 2,
    All = Sensors | Nests | Merchants
}
