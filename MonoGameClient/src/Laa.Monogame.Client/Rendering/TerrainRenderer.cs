using System;
using System.Collections.Generic;
using System.IO;
using Laa.Content.Core.Maps;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Laa.Monogame.Client.Rendering;

/// <summary>
/// Simple SpriteBatch-based renderer that draws the terrain grid using placeholder tiles.
/// </summary>
public sealed class TerrainRenderer : IDisposable
{
    private const int SheetTileWidth = 24;
    private const int SheetTileHeight = 16;
    private const int VariationColumns = 6;
    private const int VariationRows = 3;
    private const int VariationCells = VariationColumns * VariationRows;
    private const int VariationStrideX = SheetTileWidth * VariationColumns;
    private const int VariationStrideY = SheetTileHeight * VariationRows;
    private const int LiquidTerrainStart = 28;
    private const float EdgeThicknessRatio = 0.35f;
    private const float LiquidAnimationSpeed = 4f;

    private static readonly string[] TerrainSheetCandidates =
    {
        "terrain.png",
        "terrain.jpg",
        "terreno.png",
        "terreno.jpg"
    };

    private static readonly string[] TerrainSheetKeys =
    {
        "terrain",
        "terreno"
    };

    private readonly GraphicsDevice _graphicsDevice;
    private readonly int _tileSize;
    private readonly IReadOnlyList<string> _searchRoots;
    private Texture2D? _tileSheetTexture;
    private Rectangle _tileSheetRegion;
    private Texture2D? _tileTexture;
    private bool _hasTileSheet;
    private Texture2D? _verticalGradientTexture;
    private Texture2D? _horizontalGradientTexture;
    private int _liquidAnimationFrame;

    public TerrainRenderer(GraphicsDevice graphicsDevice, int tileSize, IEnumerable<string> searchRoots)
    {
        _graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
        _tileSize = tileSize > 0 ? tileSize : throw new ArgumentOutOfRangeException(nameof(tileSize));
        _searchRoots = searchRoots is not null
            ? new List<string>(searchRoots)
            : Array.Empty<string>();
    }

    public int TileSize => _tileSize;

    public void LoadContent()
    {
        _tileTexture = new Texture2D(_graphicsDevice, 1, 1);
        _tileTexture.SetData(new[] { Color.White });
        var sheet = LoadTerrainSheet();
        if (sheet is null)
        {
            Console.Error.WriteLine("Warning: terrain sheet not found. Falling back to debug colors.");
            _tileSheetTexture = null;
            _tileSheetRegion = Rectangle.Empty;
            _hasTileSheet = false;
        }
        else
        {
            _tileSheetTexture = sheet.Value.Texture;
            _tileSheetRegion = sheet.Value.Region;
            _hasTileSheet = true;
        }
        _verticalGradientTexture = CreateGradientTexture(1, _tileSize, vertical: true);
        _horizontalGradientTexture = CreateGradientTexture(_tileSize, 1, vertical: false);
    }

    public void Draw(SpriteBatch spriteBatch, MapDocument? map, Camera2D camera, TilePalette palette, GameTime gameTime)
    {
        if (spriteBatch is null) throw new ArgumentNullException(nameof(spriteBatch));
        if (camera is null) throw new ArgumentNullException(nameof(camera));
        if (palette is null) throw new ArgumentNullException(nameof(palette));
        if (gameTime is null) throw new ArgumentNullException(nameof(gameTime));
        if (_tileTexture is null || map is null)
        {
            return;
        }

        var totalSeconds = (float)gameTime.TotalGameTime.TotalSeconds;
        _liquidAnimationFrame = (int)(totalSeconds * LiquidAnimationSpeed);

        spriteBatch.Begin(
            samplerState: SamplerState.PointClamp,
            transformMatrix: camera.GetViewMatrix());

        for (var y = 0; y < map.Terrain.Count; y++)
        {
            var row = map.Terrain[y];
            for (var x = 0; x < row.Count; x++)
            {
                var code = row[x];
                var destination = new Rectangle(
                    x * _tileSize,
                    y * _tileSize,
                    _tileSize,
                    _tileSize);
                if (TryGetSourceRectangle(code, x, y, out var source))
                {
                    spriteBatch.Draw(
                        _tileSheetTexture!,
                        destination,
                        source,
                        Color.White);
                    DrawEdgeOverlays(spriteBatch, map, palette, code, x, y, destination);
                }
                else
                {
                    var fallbackColor = palette[code];
                    if (fallbackColor.A == 0)
                    {
                        continue;
                    }

                    spriteBatch.Draw(_tileTexture, destination, fallbackColor);
                    DrawEdgeOverlays(spriteBatch, map, palette, code, x, y, destination);
                }
            }
        }

        spriteBatch.End();
    }

    public void Dispose()
    {
        _tileTexture?.Dispose();
        _tileSheetTexture?.Dispose();
        _verticalGradientTexture?.Dispose();
        _horizontalGradientTexture?.Dispose();
    }

    private TileSheetResource? LoadTerrainSheet()
    {
        var direct = LoadDirectTerrainSheet();
        if (direct is not null)
        {
            return direct;
        }

        return LoadAtlasTerrainSheet();
    }

    private TileSheetResource? LoadDirectTerrainSheet()
    {
        foreach (var root in _searchRoots)
        {
            foreach (var candidate in TerrainSheetCandidates)
            {
                var path = Path.Combine(root, candidate);
                if (!File.Exists(path))
                {
                    path = Path.Combine(root, "atlases", candidate);
                }

                if (!File.Exists(path))
                {
                    continue;
                }

                var texture = LoadTexture(path);
                return new TileSheetResource(texture, new Rectangle(0, 0, texture.Width, texture.Height));
            }
        }

        return null;
    }

    private TileSheetResource? LoadAtlasTerrainSheet()
    {
        var atlasEntries = AtlasContentLoader.Load(_searchRoots);
        foreach (var key in TerrainSheetKeys)
        {
            if (!atlasEntries.TryGetValue(key, out var entry))
            {
                continue;
            }

            var atlasPath = Path.Combine(entry.Root, entry.Atlas);
            if (!File.Exists(atlasPath))
            {
                continue;
            }

            var texture = LoadTexture(atlasPath);
            var region = new Rectangle(entry.X, entry.Y, entry.Width, entry.Height);
            return new TileSheetResource(texture, region);
        }

        return null;
    }

    private Texture2D LoadTexture(string path)
    {
        using var stream = File.OpenRead(path);
        return Texture2D.FromStream(_graphicsDevice, stream);
    }

    private Texture2D CreateGradientTexture(int width, int height, bool vertical)
    {
        var texture = new Texture2D(_graphicsDevice, width, height);
        var data = new Color[width * height];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                float t;
                if (vertical)
                {
                    t = 1f - y / Math.Max(1f, height - 1f);
                }
                else
                {
                    t = 1f - x / Math.Max(1f, width - 1f);
                }

                var alpha = (byte)(MathHelper.Clamp(t, 0f, 1f) * 255f);
                data[y * width + x] = new Color((byte)255, (byte)255, (byte)255, alpha);
            }
        }

        texture.SetData(data);
        return texture;
    }

    private static int Hash(int x, int y, byte code)
    {
        var value = (x * 73856093) ^ (y * 19349663) ^ (code * 83492791);
        return value & int.MaxValue;
    }

    private bool TryGetSourceRectangle(byte code, int tileX, int tileY, out Rectangle rectangle)
    {
        rectangle = default;
        if (!_hasTileSheet || _tileSheetTexture is null || code == 0)
        {
            return false;
        }

        var blockX = code & 0x3;
        var blockY = code >> 2;
        int variantX;
        int variantY;
        if (code >= LiquidTerrainStart)
        {
            var animationIndex = (_liquidAnimationFrame + tileX + tileY) % VariationCells;
            variantX = animationIndex % VariationColumns;
            variantY = (animationIndex / VariationColumns) % VariationRows;
        }
        else
        {
            var variantSeed = Hash(tileX, tileY, code);
            variantX = variantSeed % VariationColumns;
            variantY = (variantSeed / VariationColumns) % VariationRows;
        }

        var left = _tileSheetRegion.Left + blockX * VariationStrideX + variantX * SheetTileWidth;
        var top = _tileSheetRegion.Top + blockY * VariationStrideY + variantY * SheetTileHeight;
        var rightLimit = _tileSheetRegion.Left + _tileSheetRegion.Width;
        var bottomLimit = _tileSheetRegion.Top + _tileSheetRegion.Height;

        if (left + SheetTileWidth > rightLimit || top + SheetTileHeight > bottomLimit)
        {
            return false;
        }

        rectangle = new Rectangle(left, top, SheetTileWidth, SheetTileHeight);
        return true;
    }

    private void DrawEdgeOverlays(
        SpriteBatch spriteBatch,
        MapDocument map,
        TilePalette palette,
        byte centerCode,
        int tileX,
        int tileY,
        Rectangle tileRect)
    {
        if (_hasTileSheet)
        {
            // When the real terrain sheet is present, skip the temporary gradients.
            return;
        }

        if (_verticalGradientTexture is null || _horizontalGradientTexture is null)
        {
            return;
        }

        var thickness = Math.Max(1, (int)(_tileSize * EdgeThicknessRatio));

        if (TryGetTerrainCode(map, tileX, tileY - 1, out var topCode) && topCode != centerCode)
        {
            DrawVerticalEdge(spriteBatch, tileRect, palette[topCode], thickness, SpriteEffects.None);
        }

        if (TryGetTerrainCode(map, tileX, tileY + 1, out var bottomCode) && bottomCode != centerCode)
        {
            DrawVerticalEdge(spriteBatch, tileRect, palette[bottomCode], thickness, SpriteEffects.FlipVertically);
        }

        if (TryGetTerrainCode(map, tileX - 1, tileY, out var leftCode) && leftCode != centerCode)
        {
            DrawHorizontalEdge(spriteBatch, tileRect, palette[leftCode], thickness, SpriteEffects.None);
        }

        if (TryGetTerrainCode(map, tileX + 1, tileY, out var rightCode) && rightCode != centerCode)
        {
            DrawHorizontalEdge(spriteBatch, tileRect, palette[rightCode], thickness, SpriteEffects.FlipHorizontally);
        }
    }

    private void DrawVerticalEdge(SpriteBatch spriteBatch, Rectangle tileRect, Color color, int thickness, SpriteEffects effects)
    {
        if (color.A == 0)
        {
            return;
        }

        var dest = effects == SpriteEffects.None
            ? new Rectangle(tileRect.X, tileRect.Y, tileRect.Width, thickness)
            : new Rectangle(tileRect.X, tileRect.Bottom - thickness, tileRect.Width, thickness);

        spriteBatch.Draw(
            _verticalGradientTexture!,
            dest,
            null,
            color,
            0f,
            Vector2.Zero,
            effects,
            layerDepth: 0f);
    }

    private void DrawHorizontalEdge(SpriteBatch spriteBatch, Rectangle tileRect, Color color, int thickness, SpriteEffects effects)
    {
        if (color.A == 0)
        {
            return;
        }

        var dest = effects == SpriteEffects.None
            ? new Rectangle(tileRect.X, tileRect.Y, thickness, tileRect.Height)
            : new Rectangle(tileRect.Right - thickness, tileRect.Y, thickness, tileRect.Height);

        spriteBatch.Draw(
            _horizontalGradientTexture!,
            dest,
            null,
            color,
            0f,
            Vector2.Zero,
            effects,
            layerDepth: 0f);
    }

    private static bool TryGetTerrainCode(MapDocument map, int x, int y, out byte code)
    {
        code = 0;
        if (map.Terrain.Count == 0 || y < 0 || y >= map.Terrain.Count)
        {
            return false;
        }

        var row = map.Terrain[y];
        if (x < 0 || x >= row.Count)
        {
            return false;
        }

        code = row[x];
        return true;
    }

    private readonly struct TileSheetResource(Texture2D texture, Rectangle region)
    {
        public Texture2D Texture { get; } = texture;
        public Rectangle Region { get; } = region;
    }
}
