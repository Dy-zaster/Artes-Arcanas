using Laa.Content.Core.Maps;
using Laa.Monogame.Client.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Laa.Monogame.Client.UI;

public sealed class UiMinimapWidget : IUiWidget, IDisposable
{
    private const int TextureSize = 128;

    private readonly GraphicsDevice _graphicsDevice;
    private readonly Texture2D _pixelTexture;
    private readonly Color[] _palette;
    private Texture2D? _minimapTexture;
    private Vector2 _highlight = new(0.5f, 0.5f);
    private bool _highlightVisible;

    public UiMinimapWidget(GraphicsDevice graphicsDevice, IEnumerable<string>? paletteRoots)
    {
        _graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
        _pixelTexture = new Texture2D(graphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });
        _palette = LoadPalette(paletteRoots) ?? BuildFallbackPalette();
    }

    public Vector2 Offset { get; set; } = Vector2.Zero;

    public void Update(GameTime gameTime, MouseState currentMouse, MouseState previousMouse, Rectangle contentBounds)
    {
        // No interactivity yet.
    }

    public void Draw(SpriteBatch spriteBatch, DebugTextRenderer textRenderer, Rectangle contentBounds)
    {
        if (spriteBatch is null) throw new ArgumentNullException(nameof(spriteBatch));
        if (_minimapTexture is null)
        {
            return;
        }

        var destination = new Rectangle(
            (int)Math.Floor(contentBounds.Left + Offset.X),
            (int)Math.Floor(contentBounds.Top + Offset.Y),
            TextureSize,
            TextureSize);

        spriteBatch.Draw(_minimapTexture, destination, Color.White);

        if (_highlightVisible)
        {
            var markerSize = 12;
            var hx = destination.Left + (int)Math.Round(_highlight.X * (TextureSize - markerSize));
            var hy = destination.Top + (int)Math.Round(_highlight.Y * (TextureSize - markerSize));
            var outer = new Rectangle(hx, hy, markerSize, markerSize);
            DrawRect(spriteBatch, outer, Color.Black * 0.85f);
            var inner = new Rectangle(
                outer.X + 2,
                outer.Y + 2,
                Math.Max(1, outer.Width - 4),
                Math.Max(1, outer.Height - 4));
            DrawRect(spriteBatch, inner, Color.LimeGreen * 0.9f);
        }
    }

    public void SetMap(MapDocument? map)
    {
        _minimapTexture?.Dispose();
        _minimapTexture = null;

        if (map is null)
        {
            return;
        }

        var sourceHeight = map.Terrain.Count;
        var sourceWidth = map.Terrain.FirstOrDefault()?.Count ?? 0;
        if (sourceWidth <= 0 || sourceHeight <= 0)
        {
            return;
        }

        var colors = new Color[TextureSize * TextureSize];
        var stepX = sourceWidth / (float)TextureSize;
        var stepY = sourceHeight / (float)TextureSize;

        for (var y = 0; y < TextureSize; y++)
        {
            var srcY = Math.Clamp((int)Math.Floor(y * stepY), 0, sourceHeight - 1);
            var srcY2 = Math.Min(sourceHeight - 1, srcY + 1);
            var row = map.Terrain[srcY];
            var row2 = map.Terrain[srcY2];

            for (var x = 0; x < TextureSize; x++)
            {
                var srcX = Math.Clamp((int)Math.Floor(x * stepX), 0, sourceWidth - 1);
                var srcX2 = Math.Min(sourceWidth - 1, srcX + 1);
                var c1 = LookupColor(row, srcX);
                var c2 = LookupColor(row, srcX2);
                var c3 = LookupColor(row2, srcX);
                var c4 = LookupColor(row2, srcX2);
                colors[y * TextureSize + x] = AverageColors(c1, c2, c3, c4);
            }
        }

        _minimapTexture = new Texture2D(_graphicsDevice, TextureSize, TextureSize);
        _minimapTexture.SetData(colors);
    }

    public void SetHighlight(Vector2? normalized)
    {
        if (normalized.HasValue)
        {
            var value = normalized.Value;
            _highlight = new Vector2(
                MathHelper.Clamp(value.X, 0f, 1f),
                MathHelper.Clamp(value.Y, 0f, 1f));
            _highlightVisible = true;
        }
        else
        {
            _highlightVisible = false;
        }
    }

    public void Dispose()
    {
        _minimapTexture?.Dispose();
        _pixelTexture.Dispose();
    }

    private Color LookupColor(IReadOnlyList<byte> row, int x)
    {
        if (row.Count == 0)
        {
            return Color.Black;
        }

        var index = row[Math.Clamp(x, 0, row.Count - 1)] & 0x1F;
        if (_palette.Length == 0)
        {
            return Color.DarkGray;
        }

        if (index >= _palette.Length)
        {
            index %= _palette.Length;
        }

        return _palette[index];
    }

    private static Color AverageColors(params Color[] colors)
    {
        if (colors.Length == 0)
        {
            return Color.Transparent;
        }

        var r = 0;
        var g = 0;
        var b = 0;
        foreach (var color in colors)
        {
            r += color.R;
            g += color.G;
            b += color.B;
        }

        var count = colors.Length;
        return new Color(
            (byte)(r / count),
            (byte)(g / count),
            (byte)(b / count));
    }

    private static Color[]? LoadPalette(IEnumerable<string>? searchRoots)
    {
        if (searchRoots is null)
        {
            return null;
        }

        foreach (var root in searchRoots)
        {
            if (string.IsNullOrWhiteSpace(root))
            {
                continue;
            }

            var candidate = Path.Combine(root, "Mapa.pal");
            if (File.Exists(candidate))
            {
                return ReadPalette(candidate);
            }
        }

        return null;
    }

    private static Color[] ReadPalette(string path)
    {
        var bytes = File.ReadAllBytes(path);
        var palette = new Color[bytes.Length / 2];
        for (var i = 0; i < palette.Length; i++)
        {
            var value = BitConverter.ToUInt16(bytes, i * 2);
            palette[i] = FromRgb565(value);
        }

        return palette;
    }

    private static Color[] BuildFallbackPalette()
    {
        var palette = new Color[32];
        for (var i = 0; i < palette.Length; i++)
        {
            var t = i / (float)(palette.Length - 1);
            var r = (byte)(70 + 100 * (1f - t));
            var g = (byte)(90 + 130 * t);
            var b = (byte)(60 + 90 * (1f - Math.Abs(0.5f - t) * 2f));
            palette[i] = new Color(r, g, b);
        }

        return palette;
    }

    private static Color FromRgb565(ushort value)
    {
        var r = (byte)(((value >> 11) & 0x1F) << 3);
        var g = (byte)(((value >> 5) & 0x3F) << 2);
        var b = (byte)((value & 0x1F) << 3);
        return new Color(r, g, b);
    }

    private void DrawRect(SpriteBatch spriteBatch, Rectangle rect, Color color)
    {
        spriteBatch.Draw(_pixelTexture, rect, color);
    }
}
