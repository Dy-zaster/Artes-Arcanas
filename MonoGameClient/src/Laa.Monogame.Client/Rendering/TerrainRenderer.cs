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
    private readonly int _tileWidth;
    private readonly int _tileHeight;
    private readonly IReadOnlyList<string> _searchRoots;
    private Texture2D? _tileSheetTexture;
    private Rectangle _tileSheetRegion;
    private Color[]? _tileSheetData;
    private Texture2D? _tileTexture;
    private bool _hasTileSheet;
    private Texture2D? _verticalGradientTexture;
    private Texture2D? _horizontalGradientTexture;
    private int _liquidAnimationFrame;
    private PseudoMosaicMaskSet? _pseudoMosaicMaskSet;
    private readonly Dictionary<OverlayCacheKey, Texture2D> _overlayTextureCache = new();

    public TerrainRenderer(GraphicsDevice graphicsDevice, int tileWidth, int tileHeight, IEnumerable<string> searchRoots)
    {
        _graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
        _tileWidth = tileWidth > 0 ? tileWidth : throw new ArgumentOutOfRangeException(nameof(tileWidth));
        _tileHeight = tileHeight > 0 ? tileHeight : throw new ArgumentOutOfRangeException(nameof(tileHeight));
        _searchRoots = searchRoots is not null
            ? new List<string>(searchRoots)
            : Array.Empty<string>();
    }

    public void LoadContent()
    {
        _tileTexture = new Texture2D(_graphicsDevice, 1, 1);
        _tileTexture.SetData(new[] { Color.White });
        foreach (var cached in _overlayTextureCache.Values)
        {
            cached.Dispose();
        }
        _overlayTextureCache.Clear();
        var sheet = LoadTerrainSheet();
        if (sheet is null)
        {
            Console.Error.WriteLine("Warning: terrain sheet not found. Falling back to debug colors.");
            _tileSheetTexture = null;
            _tileSheetRegion = Rectangle.Empty;
            _hasTileSheet = false;
            _tileSheetData = null;
        }
        else
        {
            _tileSheetTexture = sheet.Value.Texture;
            _tileSheetRegion = sheet.Value.Region;
            _hasTileSheet = true;
            _tileSheetData = new Color[_tileSheetTexture.Width * _tileSheetTexture.Height];
            _tileSheetTexture.GetData(_tileSheetData);
        }
        _pseudoMosaicMaskSet = LoadPseudoMosaicMasks();
        _verticalGradientTexture = CreateGradientTexture(1, _tileHeight, vertical: true);
        _horizontalGradientTexture = CreateGradientTexture(_tileWidth, 1, vertical: false);
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

        var supportsPseudo = SupportsPseudoMosaics;
        spriteBatch.Begin(
            blendState: supportsPseudo ? BlendState.NonPremultiplied : BlendState.AlphaBlend,
            samplerState: SamplerState.PointClamp,
            transformMatrix: camera.GetViewMatrix());

        for (var y = 0; y < map.Terrain.Count; y++)
        {
            var row = map.Terrain[y];
            for (var x = 0; x < row.Count; x++)
            {
                var code = row[x];
                var destination = new Rectangle(
                    x * _tileWidth,
                    y * _tileHeight,
                    _tileWidth,
                    _tileHeight);
                if (TryGetSourceRectangle(code, x, y, out var source))
                {
                    spriteBatch.Draw(
                        _tileSheetTexture!,
                        destination,
                        source,
                        Color.White);
                    if (supportsPseudo)
                    {
                        DrawPseudoMosaics(spriteBatch, map, code, x, y, destination);
                    }
                    else
                    {
                        DrawEdgeOverlays(spriteBatch, map, palette, code, x, y, destination);
                    }
                }
                else
                {
                    var fallbackColor = palette[code];
                    if (fallbackColor.A == 0)
                    {
                        continue;
                    }

                    spriteBatch.Draw(_tileTexture, destination, fallbackColor);
                    if (!supportsPseudo)
                    {
                        DrawEdgeOverlays(spriteBatch, map, palette, code, x, y, destination);
                    }
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
        foreach (var cached in _overlayTextureCache.Values)
        {
            cached.Dispose();
        }
        _overlayTextureCache.Clear();
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

    private bool TryGetSourceRectangle(byte code, int tileX, int tileY, out Rectangle rectangle) =>
        TryGetSourceRectangle(code, tileX, tileY, out rectangle, out _, out _);

    private bool TryGetSourceRectangle(byte code, int tileX, int tileY, out Rectangle rectangle, out int variantX, out int variantY)
    {
        rectangle = default;
        variantX = 0;
        variantY = 0;
        if (!_hasTileSheet || _tileSheetTexture is null || code == 0)
        {
            return false;
        }

        var blockX = code & 0x3;
        var blockY = code >> 2;
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

        var verticalThickness = Math.Max(1, (int)(_tileHeight * EdgeThicknessRatio));
        var horizontalThickness = Math.Max(1, (int)(_tileWidth * EdgeThicknessRatio));

        if (TryGetTerrainCode(map, tileX, tileY - 1, out var topCode) && topCode != centerCode)
        {
            DrawVerticalEdge(spriteBatch, tileRect, palette[topCode], verticalThickness, SpriteEffects.None);
        }

        if (TryGetTerrainCode(map, tileX, tileY + 1, out var bottomCode) && bottomCode != centerCode)
        {
            DrawVerticalEdge(spriteBatch, tileRect, palette[bottomCode], verticalThickness, SpriteEffects.FlipVertically);
        }

        if (TryGetTerrainCode(map, tileX - 1, tileY, out var leftCode) && leftCode != centerCode)
        {
            DrawHorizontalEdge(spriteBatch, tileRect, palette[leftCode], horizontalThickness, SpriteEffects.None);
        }

        if (TryGetTerrainCode(map, tileX + 1, tileY, out var rightCode) && rightCode != centerCode)
        {
            DrawHorizontalEdge(spriteBatch, tileRect, palette[rightCode], horizontalThickness, SpriteEffects.FlipHorizontally);
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

    private void DrawPseudoMosaics(
        SpriteBatch spriteBatch,
        MapDocument map,
        byte centerCode,
        int tileX,
        int tileY,
        Rectangle tileRect)
    {
        if (_pseudoMosaicMaskSet is null || _tileSheetTexture is null || _tileSheetData is null)
        {
            return;
        }

        var terrainRows = map.Terrain;
        var height = terrainRows.Count;
        if (height == 0)
        {
            return;
        }

        var width = terrainRows[0].Count;
        if (width == 0)
        {
            return;
        }

        byte Sample(int x, int y)
        {
            var clampedX = Math.Clamp(x, 0, width - 1);
            var clampedY = Math.Clamp(y, 0, height - 1);
            return terrainRows[clampedY][clampedX];
        }

        void TryDrawAt(int sampleX, int sampleY, PseudoMosaicPattern pattern)
        {
            var neighbor = Sample(sampleX, sampleY);
            if (neighbor == centerCode)
            {
                return;
            }

            DrawPseudoTile(spriteBatch, neighbor, tileX, tileY, tileRect, pattern);
        }

        var px = tileX & ~3;
        var py = tileY & ~3;
        var pi = tileX & 0x3;
        var pj = tileY & 0x3;
        var terI = Sample(px + 2, py + 2);

        if (pj == 0 && pi == 0)
        {
            var northWest = Sample(px - 1, py - 1);
            var north = Sample(px, py - 1);
            var west = Sample(px - 1, py);

            if (northWest == north)
            {
                if (west != Sample(px, py))
                {
                    TryDrawAt(px - 1, py, PseudoMosaicPattern.Vertical);
                }

                TryDrawAt(px, py - 1, PseudoMosaicPattern.Horizontal);
            }
            else
            {
                if (northWest == west)
                {
                    if (north != Sample(px, py))
                    {
                        TryDrawAt(px, py - 1, PseudoMosaicPattern.Horizontal);
                    }

                    TryDrawAt(px - 1, py, PseudoMosaicPattern.Vertical);
                }
                else if (west == Sample(px, py))
                {
                    if (west != north)
                    {
                        TryDrawAt(px - 1, py - 1, PseudoMosaicPattern.Horizontal);
                    }

                    if (Sample(px, py) != west)
                    {
                        TryDrawAt(px - 1, py - 1, PseudoMosaicPattern.CornerBottomRight);
                    }
                }
                else
                {
                    TryDrawAt(px - 1, py, PseudoMosaicPattern.Vertical);
                    if (Sample(px, py) != west)
                    {
                        TryDrawAt(px - 1, py - 1, PseudoMosaicPattern.CornerBottomRight);
                    }
                }

                if (north != Sample(px, py))
                {
                    TryDrawAt(px, py - 1, PseudoMosaicPattern.CornerBottomLeft);
                }
            }
        }
        else if (pj == 0 && pi == 2)
        {
            TryDrawAt(px + 2, py - 1, PseudoMosaicPattern.Horizontal);
        }
        else if (pi == 0 && pj == 2)
        {
            TryDrawAt(px - 1, py + 2, PseudoMosaicPattern.Vertical);
        }
        else
        {
            PseudoMosaicPattern? pendingPattern = null;
            var sampleX = px;
            var sampleY = py;

            if (pi <= 1 && pj <= 1)
            {
                if (Sample(px, py) != terI)
                {
                    if (pj + pi == 1)
                    {
                        pendingPattern = PseudoMosaicPattern.CornerTopLeft;
                        sampleX = px + 2;
                        sampleY = py + 2;
                    }
                    else if (pj + pi == 2)
                    {
                        pendingPattern = PseudoMosaicPattern.CornerBottomRight;
                        sampleX = px;
                        sampleY = py;
                    }
                }
            }
            else if (pi >= 3 && pj <= 1)
            {
                if (Sample(px + 3, py) != terI)
                {
                    if (pi - pj == 3)
                    {
                        pendingPattern = PseudoMosaicPattern.CornerTopRight;
                        sampleX = px + 2;
                        sampleY = py + 2;
                    }
                    else if (pi - pj == 2)
                    {
                        pendingPattern = PseudoMosaicPattern.CornerBottomLeft;
                        sampleX = px + 3;
                        sampleY = py;
                    }
                }
            }
            else if (pi <= 1 && pj >= 3)
            {
                if (Sample(px, py + 3) != terI)
                {
                    if (pj - pi == 3)
                    {
                        pendingPattern = PseudoMosaicPattern.CornerBottomLeft;
                        sampleX = px + 2;
                        sampleY = py + 2;
                    }
                    else if (pj - pi == 2)
                    {
                        pendingPattern = PseudoMosaicPattern.CornerTopRight;
                        sampleX = px;
                        sampleY = py + 3;
                    }
                }
            }
            else if (pi == 3 && pj == 3)
            {
                if (Sample(px + 3, py + 4) == Sample(px + 4, py + 3))
                {
                    pendingPattern = PseudoMosaicPattern.CornerTopLeft;
                    sampleX = px + 3;
                    sampleY = py + 4;
                }
            }

            if (pendingPattern.HasValue)
            {
                TryDrawAt(sampleX, sampleY, pendingPattern.Value);
            }
        }

        PseudoMosaicPattern? alternatePattern = null;
        var altX = px;
        var altY = py;

        if (pj == 0)
        {
            if (pi == 1)
            {
                if (Sample(px, py - 1) != Sample(px + 2, py - 2))
                {
                    alternatePattern = PseudoMosaicPattern.CornerBottomLeft;
                    altX = px + 2;
                    altY = py - 2;
                }
                else
                {
                    alternatePattern = PseudoMosaicPattern.HorizontalAlt;
                    altX = px + 1;
                    altY = py - 1;
                }
            }
            else if (pi == 3)
            {
                if (Sample(px + 4, py - 1) != Sample(px + 2, py - 2) &&
                    Sample(px + 4, py - 1) == Sample(px + 3, py))
                {
                    alternatePattern = PseudoMosaicPattern.CornerBottomRight;
                    altX = px + 2;
                    altY = py - 2;
                }
                else
                {
                    alternatePattern = PseudoMosaicPattern.HorizontalAlt;
                    altX = px + 3;
                    altY = py - 1;
                }
            }
        }

        if (pi == 0)
        {
            if (pj == 1)
            {
                if (Sample(px - 1, py) != Sample(px - 2, py + 2))
                {
                    alternatePattern = PseudoMosaicPattern.CornerTopRight;
                    altX = px - 2;
                    altY = py + 2;
                }
                else
                {
                    alternatePattern = PseudoMosaicPattern.VerticalAlt;
                    altX = px - 1;
                    altY = py + 1;
                }
            }
            else if (pj == 3)
            {
                if (Sample(px - 1, py + 4) != Sample(px - 2, py + 2) &&
                    Sample(px - 1, py + 4) == Sample(px, py + 3))
                {
                    alternatePattern = PseudoMosaicPattern.CornerBottomRight;
                    altX = px - 2;
                    altY = py + 2;
                }
                else
                {
                    alternatePattern = PseudoMosaicPattern.VerticalAlt;
                    altX = px - 1;
                    altY = py + 3;
                }
            }
        }

        if (alternatePattern.HasValue)
        {
            TryDrawAt(altX, altY, alternatePattern.Value);
        }
    }

    private void DrawPseudoTile(
        SpriteBatch spriteBatch,
        byte terrainCode,
        int tileX,
        int tileY,
        Rectangle tileRect,
        PseudoMosaicPattern pattern)
    {
        if (_tileSheetTexture is null || _tileSheetData is null || _pseudoMosaicMaskSet is null)
        {
            return;
        }

        if (!TryGetSourceRectangle(terrainCode, tileX, tileY, out var source, out var variantX, out var variantY))
        {
            return;
        }

        var adjustedPattern = AdjustPattern(pattern, terrainCode, tileX);
        var texture = GetOrCreateOverlayTexture(terrainCode, variantX, variantY, adjustedPattern, source);
        if (texture is null)
        {
            return;
        }

        spriteBatch.Draw(texture, tileRect, Color.White);
    }

    private Texture2D? GetOrCreateOverlayTexture(
        byte terrainCode,
        int variantX,
        int variantY,
        PseudoMosaicPattern pattern,
        Rectangle sourceRect)
    {
        if (_pseudoMosaicMaskSet is null || _tileSheetTexture is null || _tileSheetData is null)
        {
            return null;
        }

        var key = new OverlayCacheKey(terrainCode, variantX, variantY, pattern);
        if (_overlayTextureCache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        if (!_pseudoMosaicMaskSet.TryGetMask(pattern, out var mask))
        {
            return null;
        }

        var buffer = new Color[SheetTileWidth * SheetTileHeight];
        var sheetWidth = _tileSheetTexture.Width;
        for (var y = 0; y < SheetTileHeight; y++)
        {
            var sourceY = sourceRect.Top + y;
            var sourceOffset = sourceY * sheetWidth;
            var maskOffset = y * SheetTileWidth;
            for (var x = 0; x < SheetTileWidth; x++)
            {
                var sourceX = sourceRect.Left + x;
                var color = _tileSheetData[sourceOffset + sourceX];
                var weight = mask[maskOffset + x];
                if (weight <= 0f || color.A == 0)
                {
                    buffer[maskOffset + x] = Color.Transparent;
                }
                else
                {
                    var alpha = (byte)MathHelper.Clamp(weight * 255f, 0f, 255f);
                    buffer[maskOffset + x] = new Color(color.R, color.G, color.B, alpha);
                }
            }
        }

        var texture = new Texture2D(_graphicsDevice, SheetTileWidth, SheetTileHeight);
        texture.SetData(buffer);
        _overlayTextureCache[key] = texture;
        return texture;
    }

    private static PseudoMosaicPattern AdjustPattern(PseudoMosaicPattern pattern, byte terrainCode, int tileX)
    {
        if (terrainCode >= LiquidTerrainStart)
        {
            return pattern;
        }

        if ((tileX & 0x4) == 0)
        {
            if (pattern == PseudoMosaicPattern.VerticalAlt)
            {
                return PseudoMosaicPattern.Vertical;
            }

            if (pattern == PseudoMosaicPattern.Vertical)
            {
                return PseudoMosaicPattern.VerticalAlt;
            }
        }

        return pattern;
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

    private PseudoMosaicMaskSet? LoadPseudoMosaicMasks()
    {
        // Prefer raw mask if present.
        foreach (var root in _searchRoots)
        {
            var fullRoot = Path.GetFullPath(root);
            var primary = Path.Combine(fullRoot, "ti.bmp");
            if (!File.Exists(primary))
            {
                primary = Path.Combine(fullRoot, "atlases", "ti.bmp");
            }

            if (File.Exists(primary))
            {
                var mask = PseudoMosaicMaskSet.Load(primary, SheetTileWidth, SheetTileHeight);
                if (mask is not null)
                {
                    return mask;
                }
            }
        }

        // Fallback: extract from atlas entry "ti".
        var atlasEntries = AtlasContentLoader.Load(_searchRoots);
        if (atlasEntries.TryGetValue("ti", out var entry))
        {
            var atlasPath = Path.Combine(entry.Root, entry.Atlas);
            if (File.Exists(atlasPath))
            {
                using var texture = LoadTexture(atlasPath);
                var region = new Rectangle(entry.X, entry.Y, entry.Width, entry.Height);
                var data = new Color[region.Width * region.Height];
                texture.GetData(0, region, data, 0, data.Length);
                var mask = PseudoMosaicMaskSet.FromRegion(data, region.Width, region.Height, SheetTileWidth, SheetTileHeight);
                if (mask is not null)
                {
                    return mask;
                }
            }
        }

        return null;
    }

    private bool SupportsPseudoMosaics =>
        _hasTileSheet &&
        _tileSheetData is not null &&
        _pseudoMosaicMaskSet is not null;

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

    private readonly record struct OverlayCacheKey(
        byte TerrainCode,
        int VariantX,
        int VariantY,
        PseudoMosaicPattern Pattern);

    private enum PseudoMosaicPattern
    {
        Horizontal = 0,
        Vertical = 1,
        HorizontalAlt = 2,
        VerticalAlt = 3,
        CornerTopLeft = 4,
        CornerTopRight = 5,
        CornerBottomLeft = 6,
        CornerBottomRight = 7
    }

    private sealed class PseudoMosaicMaskSet
    {
        private readonly Dictionary<PseudoMosaicPattern, float[]> _masks;

        private PseudoMosaicMaskSet(Dictionary<PseudoMosaicPattern, float[]> masks)
        {
            _masks = masks;
        }

        public bool TryGetMask(PseudoMosaicPattern pattern, out float[] mask)
        {
            return _masks.TryGetValue(pattern, out mask!);
        }

        public static PseudoMosaicMaskSet? Load(string path, int tileWidth, int tileHeight)
        {
            try
            {
                using var stream = File.OpenRead(path);
                using var reader = new BinaryReader(stream);
                if (reader.ReadUInt16() != 0x4D42) // 'BM'
                {
                    return null;
                }

                stream.Seek(4, SeekOrigin.Current); // file size
                stream.Seek(4, SeekOrigin.Current); // reserved
                var dataOffset = reader.ReadInt32();
                var headerSize = reader.ReadInt32();
                if (headerSize < 40)
                {
                    return null;
                }

                var width = reader.ReadInt32();
                var height = reader.ReadInt32();
                var planes = reader.ReadInt16();
                var bitCount = reader.ReadInt16();
                if (planes != 1 || bitCount != 8)
                {
                    return null;
                }

                var compression = reader.ReadInt32();
                if (compression != 0)
                {
                    return null;
                }

                stream.Seek(dataOffset, SeekOrigin.Begin);

                var rowStride = ((width * bitCount + 31) / 32) * 4;
                var rowBuffer = new byte[rowStride];
                var pixels = new byte[Math.Max(1, width * height)];
                for (var row = 0; row < height; row++)
                {
                    var read = stream.Read(rowBuffer, 0, rowStride);
                    if (read < rowStride)
                    {
                        return null;
                    }

                    var destRow = height - 1 - row;
                    Buffer.BlockCopy(rowBuffer, 0, pixels, destRow * width, Math.Min(width, rowStride));
                }

                var blockCount = height / tileHeight;
                var patterns = Enum.GetValues<PseudoMosaicPattern>();
                var weights = new[]
                {
                    1f,
                    0.875f,
                    0.75f,
                    0.625f,
                    0.5f,
                    0.375f,
                    0.25f,
                    0.125f,
                    0f
                };

                var masks = new Dictionary<PseudoMosaicPattern, float[]>();
                var totalBlocks = Math.Min(blockCount, patterns.Length);
                for (var block = 0; block < totalBlocks; block++)
                {
                    var mask = new float[tileWidth * tileHeight];
                    for (var y = 0; y < tileHeight; y++)
                    {
                        for (var x = 0; x < tileWidth; x++)
                        {
                            var srcIndex = (block * tileHeight + y) * width + x;
                            byte raw = 8;
                            if (srcIndex >= 0 && srcIndex < pixels.Length)
                            {
                                raw = pixels[srcIndex];
                            }

                            var weightIndex = Math.Clamp(raw, 0, weights.Length - 1);
                            mask[y * tileWidth + x] = weights[weightIndex];
                        }
                    }

                    masks[patterns[block]] = mask;
                }

                return new PseudoMosaicMaskSet(masks);
            }
            catch
            {
                return null;
            }
        }

        public static PseudoMosaicMaskSet? FromRegion(Color[] pixels, int width, int height, int tileWidth, int tileHeight)
        {
            if (pixels is null || pixels.Length == 0 || width <= 0 || height <= 0)
            {
                return null;
            }

            var blockCount = height / tileHeight;
            var patterns = Enum.GetValues<PseudoMosaicPattern>();
            var weights = new[]
            {
                1f,
                0.875f,
                0.75f,
                0.625f,
                0.5f,
                0.375f,
                0.25f,
                0.125f,
                0f
            };

            float ToWeight(Color color)
            {
                // Use luminance to approximate the original 0-8 mask palette.
                var luminance = (int)(0.2126f * color.R + 0.7152f * color.G + 0.0722f * color.B);
                var index = Math.Clamp((int)MathF.Round(luminance / 32f), 0, weights.Length - 1);
                return weights[index];
            }

            var masks = new Dictionary<PseudoMosaicPattern, float[]>();
            var totalBlocks = Math.Min(blockCount, patterns.Length);
            for (var block = 0; block < totalBlocks; block++)
            {
                var mask = new float[tileWidth * tileHeight];
                for (var y = 0; y < tileHeight; y++)
                {
                    for (var x = 0; x < tileWidth; x++)
                    {
                        var srcIndex = (block * tileHeight + y) * width + x;
                        var weight = srcIndex >= 0 && srcIndex < pixels.Length
                            ? ToWeight(pixels[srcIndex])
                            : 0f;
                        mask[y * tileWidth + x] = weight;
                    }
                }

                masks[patterns[block]] = mask;
            }

            return masks.Count > 0 ? new PseudoMosaicMaskSet(masks) : null;
        }
    }

    private readonly struct TileSheetResource(Texture2D texture, Rectangle region)
    {
        public Texture2D Texture { get; } = texture;
        public Rectangle Region { get; } = region;
    }
}
