using Microsoft.Xna.Framework;

namespace Laa.Monogame.Client.Rendering;

/// <summary>
/// Generates pseudo colors for legacy terrain ids so maps are readable before real textures exist.
/// </summary>
public sealed class TilePalette
{
    private readonly Color[] _lookup = new Color[256];

    public TilePalette()
    {
        for (var i = 0; i < _lookup.Length; i++)
        {
            _lookup[i] = CreateColor((byte)i);
        }

        _lookup[0] = Color.Transparent;
    }

    public Color this[byte code] => _lookup[code];

    private static Color CreateColor(byte code)
    {
        var seed = (code * 1103515245u + 12345u) & 0x00FFFFFF;

        var r = (byte)(40 + (seed & 0xFF) % 200);
        var g = (byte)(40 + ((seed >> 8) & 0xFF) % 200);
        var b = (byte)(40 + ((seed >> 16) & 0xFF) % 200);

        return new Color(r, g, b);
    }
}
