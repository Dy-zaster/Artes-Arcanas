using Laa.Content.Core.Animations;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Laa.Monogame.Client.Rendering;

public sealed class AnimationTextureProvider : IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly Dictionary<string, AnimationEntry> _entries;
    private readonly Dictionary<string, AnimationTexture> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, AtlasSpriteEntry> _atlasEntries;
    private readonly Dictionary<string, Texture2D> _atlasTextures = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<Texture2D> _ownedTextures = new();

    public AnimationTextureProvider(GraphicsDevice graphicsDevice, AnimationDocument document, IEnumerable<string> searchRoots)
    {
        _graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
        if (document is null) throw new ArgumentNullException(nameof(document));
        _entries = document.Animations
            .Where(a => !string.IsNullOrWhiteSpace(a.Key))
            .ToDictionary(a => a.Key, StringComparer.OrdinalIgnoreCase);
        _atlasEntries = AtlasContentLoader.Load(searchRoots);
    }

    public bool TryGetAnimation(string key, out AnimationTexture animation)
    {
        animation = default!;
        if (string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        if (_cache.TryGetValue(key, out var cached))
        {
            animation = cached;
            return true;
        }

        if (!_entries.TryGetValue(key, out var entry))
        {
            return false;
        }

        if (!TryGetSheetRegion(entry.Key, out var texture, out var region))
        {
            return false;
        }

        var directions = BuildDirections(entry, region);
        var created = new AnimationTexture(entry.Kind, entry.StyleFlags, texture, directions);
        _cache[entry.Key] = created;
        animation = created;
        return true;
    }

    private IReadOnlyList<AnimationDirectionSlice> BuildDirections(AnimationEntry entry, Rectangle sheetRegion)
    {
        var slices = new List<AnimationDirectionSlice>(entry.Directions.Count);
        var offsetX = 0;
        foreach (var direction in entry.Directions)
        {
            if (direction is null)
            {
                slices.Add(new AnimationDirectionSlice(0, 0, 0, Array.Empty<AnimationFrameSlice>()));
                continue;
            }

            var baseX = sheetRegion.X + offsetX;
            var baseY = sheetRegion.Y;
            var frames = new List<AnimationFrameSlice>(direction.Frames.Count);
            var prevAccum = 0;
            foreach (var frame in direction.Frames)
            {
                var accum = frame.AccumulatedY;
                var height = accum - prevAccum;
                if (height <= 0 || frame.Width <= 0)
                {
                    frames.Add(new AnimationFrameSlice(Rectangle.Empty, frame.CenterX, frame.CenterY));
                    prevAccum = accum;
                    continue;
                }

                var source = new Rectangle(baseX, baseY + prevAccum, frame.Width, height);
                frames.Add(new AnimationFrameSlice(source, frame.CenterX, frame.CenterY));
                prevAccum = accum;
            }

            slices.Add(new AnimationDirectionSlice(direction.MaxWidth, direction.OffsetX, direction.OffsetY, frames));
            offsetX += direction.MaxWidth;
        }

        return slices;
    }

    private bool TryGetSheetRegion(string key, out Texture2D texture, out Rectangle region)
    {
        texture = default!;
        region = Rectangle.Empty;
        if (!_atlasEntries.TryGetValue(key, out var sprite))
        {
            return false;
        }

        texture = GetAtlasTexture(sprite);
        region = new Rectangle(sprite.X, sprite.Y, sprite.Width, sprite.Height);
        return true;
    }

    private Texture2D GetAtlasTexture(AtlasSpriteEntry sprite)
    {
        if (_atlasTextures.TryGetValue(sprite.Atlas, out var texture))
        {
            return texture;
        }

        var fullPath = Path.Combine(sprite.Root, sprite.Atlas);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Atlas image '{fullPath}' not found.");
        }

        using var stream = File.OpenRead(fullPath);
        var loaded = Texture2D.FromStream(_graphicsDevice, stream);
        _atlasTextures[sprite.Atlas] = loaded;
        _ownedTextures.Add(loaded);
        return loaded;
    }

    public void Dispose()
    {
        foreach (var texture in _ownedTextures)
        {
            texture.Dispose();
        }
        _atlasTextures.Clear();
        _ownedTextures.Clear();
        _cache.Clear();
    }
}

public sealed record AnimationTexture(
    AnimationKind Kind,
    int StyleFlags,
    Texture2D Texture,
    IReadOnlyList<AnimationDirectionSlice> Directions);

public sealed record AnimationDirectionSlice(
    short MaxWidth,
    byte OffsetX,
    byte OffsetY,
    IReadOnlyList<AnimationFrameSlice> Frames);

public sealed record AnimationFrameSlice(
    Rectangle Source,
    byte CenterX,
    byte CenterY);
