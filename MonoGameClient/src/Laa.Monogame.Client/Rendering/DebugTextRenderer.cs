using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Laa.Monogame.Client.Rendering;

public sealed class DebugTextRenderer : IDisposable
{
    private const int GlyphWidth = 5;
    private const int GlyphHeight = 7;
    private const int GlyphSpacing = 1;

    private readonly Texture2D _pixel;
    private readonly Dictionary<char, string[]> _glyphs;

    public DebugTextRenderer(GraphicsDevice graphicsDevice)
    {
        _pixel = new Texture2D(graphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        _glyphs = BuildGlyphs();
    }

    public Texture2D PixelTexture => _pixel;

    public Vector2 MeasureString(string text, float scale = 1f)
    {
        var width = 0f;
        var height = GlyphHeight;
        var lineWidth = 0f;

        foreach (var ch in text)
        {
            if (ch == '\r')
            {
                continue;
            }

            if (ch == '\n')
            {
                height += GlyphHeight + GlyphSpacing;
                if (lineWidth > width)
                {
                    width = lineWidth;
                }

                lineWidth = 0f;
                continue;
            }

            lineWidth += GlyphWidth + GlyphSpacing;
        }

        if (lineWidth > width)
        {
            width = lineWidth;
        }

        return new Vector2(width * scale, height * scale);
    }

    public void DrawString(SpriteBatch spriteBatch, string text, Vector2 position, Color color, float scale = 1f)
    {
        var cursorX = position.X;
        var cursorY = position.Y;

        foreach (var ch in text)
        {
            if (ch == '\r')
            {
                continue;
            }

            if (ch == '\n')
            {
                cursorX = position.X;
                cursorY += (GlyphHeight + GlyphSpacing) * scale;
                continue;
            }

            DrawGlyph(spriteBatch, ch, cursorX, cursorY, color, scale);
            cursorX += (GlyphWidth + GlyphSpacing) * scale;
        }
    }

    private void DrawGlyph(SpriteBatch spriteBatch, char character, float x, float y, Color color, float scale)
    {
        if (!_glyphs.TryGetValue(character, out var pattern))
        {
            pattern = _glyphs['?'];
        }

        for (var row = 0; row < pattern.Length; row++)
        {
            var line = pattern[row];
            for (var col = 0; col < line.Length; col++)
            {
                if (line[col] == '#')
                {
                    var destination = new Rectangle(
                        (int)Math.Floor(x + col * scale),
                        (int)Math.Floor(y + row * scale),
                        Math.Max(1, (int)Math.Ceiling(scale)),
                        Math.Max(1, (int)Math.Ceiling(scale)));
                    spriteBatch.Draw(_pixel, destination, color);
                }
            }
        }
    }

    private static Dictionary<char, string[]> BuildGlyphs()
    {
        var glyphs = new Dictionary<char, string[]>
        {
            [' '] = CreateGlyph(
                "     ",
                "     ",
                "     ",
                "     ",
                "     ",
                "     ",
                "     "),
            ['A'] = CreateGlyph(
                " ### ",
                "#   #",
                "#   #",
                "#####",
                "#   #",
                "#   #",
                "#   #"),
            ['B'] = CreateGlyph(
                "#### ",
                "#   #",
                "#   #",
                "#### ",
                "#   #",
                "#   #",
                "#### "),
            ['C'] = CreateGlyph(
                " ####",
                "#    ",
                "#    ",
                "#    ",
                "#    ",
                "#    ",
                " ####"),
            ['D'] = CreateGlyph(
                "#### ",
                "#   #",
                "#   #",
                "#   #",
                "#   #",
                "#   #",
                "#### "),
            ['E'] = CreateGlyph(
                "#####",
                "#    ",
                "#    ",
                "###  ",
                "#    ",
                "#    ",
                "#####"),
            ['F'] = CreateGlyph(
                "#####",
                "#    ",
                "#    ",
                "###  ",
                "#    ",
                "#    ",
                "#    "),
            ['G'] = CreateGlyph(
                " ####",
                "#    ",
                "#    ",
                "#  ##",
                "#   #",
                "#   #",
                " ####"),
            ['H'] = CreateGlyph(
                "#   #",
                "#   #",
                "#   #",
                "#####",
                "#   #",
                "#   #",
                "#   #"),
            ['I'] = CreateGlyph(
                "#####",
                "  #  ",
                "  #  ",
                "  #  ",
                "  #  ",
                "  #  ",
                "#####"),
            ['J'] = CreateGlyph(
                "#####",
                "    #",
                "    #",
                "    #",
                "#   #",
                "#   #",
                " ### "),
            ['K'] = CreateGlyph(
                "#   #",
                "#  # ",
                "# #  ",
                "##   ",
                "# #  ",
                "#  # ",
                "#   #"),
            ['L'] = CreateGlyph(
                "#    ",
                "#    ",
                "#    ",
                "#    ",
                "#    ",
                "#    ",
                "#####"),
            ['M'] = CreateGlyph(
                "#   #",
                "## ##",
                "# # #",
                "# # #",
                "#   #",
                "#   #",
                "#   #"),
            ['N'] = CreateGlyph(
                "#   #",
                "##  #",
                "##  #",
                "# # #",
                "#  ##",
                "#  ##",
                "#   #"),
            ['O'] = CreateGlyph(
                " ### ",
                "#   #",
                "#   #",
                "#   #",
                "#   #",
                "#   #",
                " ### "),
            ['P'] = CreateGlyph(
                "#### ",
                "#   #",
                "#   #",
                "#### ",
                "#    ",
                "#    ",
                "#    "),
            ['Q'] = CreateGlyph(
                " ### ",
                "#   #",
                "#   #",
                "#   #",
                "# # #",
                "#  ##",
                " ####"),
            ['R'] = CreateGlyph(
                "#### ",
                "#   #",
                "#   #",
                "#### ",
                "# #  ",
                "#  # ",
                "#   #"),
            ['S'] = CreateGlyph(
                " ####",
                "#    ",
                "#    ",
                " ### ",
                "    #",
                "    #",
                "#### "),
            ['T'] = CreateGlyph(
                "#####",
                "  #  ",
                "  #  ",
                "  #  ",
                "  #  ",
                "  #  ",
                "  #  "),
            ['U'] = CreateGlyph(
                "#   #",
                "#   #",
                "#   #",
                "#   #",
                "#   #",
                "#   #",
                " ### "),
            ['V'] = CreateGlyph(
                "#   #",
                "#   #",
                "#   #",
                "#   #",
                "#   #",
                " # # ",
                "  #  "),
            ['W'] = CreateGlyph(
                "#   #",
                "#   #",
                "#   #",
                "# # #",
                "# # #",
                "## ##",
                "#   #"),
            ['X'] = CreateGlyph(
                "#   #",
                "#   #",
                " # # ",
                "  #  ",
                " # # ",
                "#   #",
                "#   #"),
            ['Y'] = CreateGlyph(
                "#   #",
                "#   #",
                " # # ",
                "  #  ",
                "  #  ",
                "  #  ",
                "  #  "),
            ['Z'] = CreateGlyph(
                "#####",
                "    #",
                "   # ",
                "  #  ",
                " #   ",
                "#    ",
                "#####"),
            ['a'] = CreateGlyph(
                "     ",
                " ### ",
                "    #",
                " ####",
                "#   #",
                " ####",
                "     "),
            ['b'] = CreateGlyph(
                "#    ",
                "#    ",
                "#### ",
                "#   #",
                "#   #",
                "#### ",
                "     "),
            ['c'] = CreateGlyph(
                "     ",
                " ####",
                "#    ",
                "#    ",
                "#    ",
                " ####",
                "     "),
            ['d'] = CreateGlyph(
                "    #",
                "    #",
                " ####",
                "#   #",
                "#   #",
                " ####",
                "     "),
            ['e'] = CreateGlyph(
                "     ",
                " ### ",
                "#   #",
                "#####",
                "#    ",
                " ####",
                "     "),
            ['f'] = CreateGlyph(
                "  ## ",
                " #   ",
                " ### ",
                " #   ",
                " #   ",
                " #   ",
                "     "),
            ['g'] = CreateGlyph(
                "     ",
                " ####",
                "#   #",
                " ####",
                "    #",
                "#### ",
                "     "),
            ['h'] = CreateGlyph(
                "#    ",
                "#    ",
                "#### ",
                "#   #",
                "#   #",
                "#   #",
                "     "),
            ['i'] = CreateGlyph(
                "  #  ",
                "     ",
                " ##  ",
                "  #  ",
                "  #  ",
                " ### ",
                "     "),
            ['j'] = CreateGlyph(
                "   # ",
                "     ",
                "   ##",
                "   # ",
                "   # ",
                "#  # ",
                " ##  "),
            ['k'] = CreateGlyph(
                "#    ",
                "#  # ",
                "# #  ",
                "##   ",
                "# #  ",
                "#  # ",
                "     "),
            ['l'] = CreateGlyph(
                " ##  ",
                "  #  ",
                "  #  ",
                "  #  ",
                "  #  ",
                "  #  ",
                " ### "),
            ['m'] = CreateGlyph(
                "     ",
                "## ##",
                "# # #",
                "# # #",
                "#   #",
                "#   #",
                "     "),
            ['n'] = CreateGlyph(
                "     ",
                "###  ",
                "#  # ",
                "#  # ",
                "#  # ",
                "#  # ",
                "     "),
            ['o'] = CreateGlyph(
                "     ",
                " ### ",
                "#   #",
                "#   #",
                "#   #",
                " ### ",
                "     "),
            ['p'] = CreateGlyph(
                "     ",
                "#### ",
                "#   #",
                "#### ",
                "#    ",
                "#    ",
                "     "),
            ['q'] = CreateGlyph(
                "     ",
                " ####",
                "#   #",
                " ####",
                "    #",
                "    #",
                "     "),
            ['r'] = CreateGlyph(
                "     ",
                "#### ",
                "#   #",
                "#    ",
                "#    ",
                "#    ",
                "     "),
            ['s'] = CreateGlyph(
                "     ",
                " ####",
                "#    ",
                " ### ",
                "    #",
                "#### ",
                "     "),
            ['t'] = CreateGlyph(
                "  #  ",
                " ### ",
                "  #  ",
                "  #  ",
                "  #  ",
                "  ## ",
                "     "),
            ['u'] = CreateGlyph(
                "     ",
                "#   #",
                "#   #",
                "#   #",
                "#   #",
                " ####",
                "     "),
            ['v'] = CreateGlyph(
                "     ",
                "#   #",
                "#   #",
                "#   #",
                " # # ",
                "  #  ",
                "     "),
            ['w'] = CreateGlyph(
                "     ",
                "#   #",
                "#   #",
                "# # #",
                "## ##",
                "#   #",
                "     "),
            ['x'] = CreateGlyph(
                "     ",
                "#   #",
                " # # ",
                "  #  ",
                " # # ",
                "#   #",
                "     "),
            ['y'] = CreateGlyph(
                "     ",
                "#   #",
                "#   #",
                " ####",
                "    #",
                " ### ",
                "     "),
            ['z'] = CreateGlyph(
                "     ",
                "#####",
                "   # ",
                "  #  ",
                " #   ",
                "#####",
                "     "),
            ['0'] = CreateGlyph(
                " ### ",
                "#   #",
                "#  ##",
                "# # #",
                "##  #",
                "#   #",
                " ### "),
            ['1'] = CreateGlyph(
                "  #  ",
                " ##  ",
                "# #  ",
                "  #  ",
                "  #  ",
                "  #  ",
                "#####"),
            ['2'] = CreateGlyph(
                " ### ",
                "#   #",
                "    #",
                "   # ",
                "  #  ",
                " #   ",
                "#####"),
            ['3'] = CreateGlyph(
                " ### ",
                "#   #",
                "    #",
                " ### ",
                "    #",
                "#   #",
                " ### "),
            ['4'] = CreateGlyph(
                "#   #",
                "#   #",
                "#   #",
                "#####",
                "    #",
                "    #",
                "    #"),
            ['5'] = CreateGlyph(
                "#####",
                "#    ",
                "#    ",
                "#### ",
                "    #",
                "    #",
                "#### "),
            ['6'] = CreateGlyph(
                " ####",
                "#    ",
                "#    ",
                "#### ",
                "#   #",
                "#   #",
                " ### "),
            ['7'] = CreateGlyph(
                "#####",
                "    #",
                "   # ",
                "  #  ",
                " #   ",
                " #   ",
                " #   "),
            ['8'] = CreateGlyph(
                " ### ",
                "#   #",
                "#   #",
                " ### ",
                "#   #",
                "#   #",
                " ### "),
            ['9'] = CreateGlyph(
                " ### ",
                "#   #",
                "#   #",
                " ####",
                "    #",
                "    #",
                " ### "),
            ['#'] = CreateGlyph(
                " # # ",
                "#####",
                " # # ",
                " # # ",
                "#####",
                " # # ",
                " # # "),
            ['!'] = CreateGlyph(
                "  #  ",
                "  #  ",
                "  #  ",
                "  #  ",
                "  #  ",
                "     ",
                "  #  "),
            [':'] = CreateGlyph(
                "     ",
                "  #  ",
                "     ",
                "     ",
                "     ",
                "  #  ",
                "     "),
            [','] = CreateGlyph(
                "     ",
                "     ",
                "     ",
                "     ",
                "  ## ",
                "  #  ",
                " #   "),
            ['-'] = CreateGlyph(
                "     ",
                "     ",
                "     ",
                " ### ",
                "     ",
                "     ",
                "     "),
            ['+'] = CreateGlyph(
                "     ",
                "  #  ",
                "  #  ",
                "#####",
                "  #  ",
                "  #  ",
                "     "),
            ['%'] = CreateGlyph(
                "##  #",
                "## # ",
                "   # ",
                "  #  ",
                " #   ",
                "#  ##",
                "#  ##"),
            ['_'] = CreateGlyph(
                "     ",
                "     ",
                "     ",
                "     ",
                "     ",
                "     ",
                "#####"),
            ['.'] = CreateGlyph(
                "     ",
                "     ",
                "     ",
                "     ",
                "     ",
                "  #  ",
                "     "),
            ['?'] = CreateGlyph(
                " ### ",
                "#   #",
                "    #",
                "   # ",
                "  #  ",
                "     ",
                "  #  "),
            ['('] = CreateGlyph(
                "   # ",
                "  #  ",
                " #   ",
                " #   ",
                " #   ",
                "  #  ",
                "   # "),
            [')'] = CreateGlyph(
                " #   ",
                "  #  ",
                "   # ",
                "   # ",
                "   # ",
                "  #  ",
                " #   "),
            ['/'] = CreateGlyph(
                "    #",
                "    #",
                "   # ",
                "  #  ",
                " #   ",
                "#    ",
                "#    "),
            ['|'] = CreateGlyph(
                "  #  ",
                "  #  ",
                "  #  ",
                "  #  ",
                "  #  ",
                "  #  ",
                "  #  "),
            ['<'] = CreateGlyph(
                "    #",
                "   # ",
                "  #  ",
                " #   ",
                "  #  ",
                "   # ",
                "    #"),
            ['>'] = CreateGlyph(
                "#    ",
                " #   ",
                "  #  ",
                "   # ",
                "  #  ",
                " #   ",
                "#    "),
            ['['] = CreateGlyph(
                "#####",
                "#    ",
                "#    ",
                "#    ",
                "#    ",
                "#    ",
                "#####"),
            [']'] = CreateGlyph(
                "#####",
                "    #",
                "    #",
                "    #",
                "    #",
                "    #",
                "#####"),
            ['Á'] = CreateGlyph(
                "  #  ",
                " ### ",
                "#   #",
                "#   #",
                "#####",
                "#   #",
                "#   #"),
            ['É'] = CreateGlyph(
                "  #  ",
                "#####",
                "#    ",
                "#    ",
                "###  ",
                "#    ",
                "#####"),
            ['Ç'] = CreateGlyph(
                " ####",
                "#    ",
                "#    ",
                "#    ",
                "#    ",
                " ####",
                "  #  "),
            ['Í'] = CreateGlyph(
                "  #  ",
                "#####",
                "  #  ",
                "  #  ",
                "  #  ",
                "  #  ",
                "#####"),
            ['Ó'] = CreateGlyph(
                "  #  ",
                " ### ",
                "#   #",
                "#   #",
                "#   #",
                "#   #",
                " ### "),
            ['Ú'] = CreateGlyph(
                "  #  ",
                "#   #",
                "#   #",
                "#   #",
                "#   #",
                "#   #",
                " ### "),
            ['Ü'] = CreateGlyph(
                "#   #",
                "     ",
                "#   #",
                "#   #",
                "#   #",
                "#   #",
                " ### "),
            ['Ñ'] = CreateGlyph(
                " ## #",
                "#   #",
                "##  #",
                "##  #",
                "# # #",
                "#  ##",
                "#   #"),
            ['á'] = CreateGlyph(
                "  #  ",
                " ### ",
                "    #",
                " ####",
                "#   #",
                " ####",
                "     "),
            ['é'] = CreateGlyph(
                "  #  ",
                " ### ",
                "#   #",
                "#####",
                "#    ",
                " ####",
                "     "),
            ['í'] = CreateGlyph(
                "  #  ",
                "  #  ",
                "     ",
                " ##  ",
                "  #  ",
                "  #  ",
                " ### "),
            ['ó'] = CreateGlyph(
                "  #  ",
                " ### ",
                "#   #",
                "#   #",
                "#   #",
                " ### ",
                "     "),
            ['ç'] = CreateGlyph(
                "     ",
                " ####",
                "#    ",
                "#    ",
                "#    ",
                " ####",
                "  #  "),
            ['ú'] = CreateGlyph(
                "  #  ",
                "     ",
                "#   #",
                "#   #",
                "#   #",
                "#   #",
                " ####"),
            ['ü'] = CreateGlyph(
                "#   #",
                "     ",
                "#   #",
                "#   #",
                "#   #",
                "#   #",
                " ####"),
            ['ñ'] = CreateGlyph(
                " ## #",
                "     ",
                "###  ",
                "#  # ",
                "#  # ",
                "#  # ",
                "     "),
            ['¡'] = CreateGlyph(
                "  #  ",
                "     ",
                "  #  ",
                "  #  ",
                "  #  ",
                "  #  ",
                "  #  "),
            ['¿'] = CreateGlyph(
                "  ## ",
                " #   ",
                "     ",
                "  ###",
                "    #",
                "#   #",
                " ### ")
        };

        return glyphs;
    }

    private static string[] CreateGlyph(params string[] rows)
    {
        if (rows.Length != GlyphHeight)
        {
            throw new ArgumentException($"Glyph must contain {GlyphHeight} rows.", nameof(rows));
        }

        foreach (var row in rows)
        {
            if (row.Length != GlyphWidth)
            {
                throw new ArgumentException($"Glyph rows must be {GlyphWidth} characters wide.");
            }
        }

        return rows;
    }

    public void Dispose()
    {
        _pixel.Dispose();
    }
}
