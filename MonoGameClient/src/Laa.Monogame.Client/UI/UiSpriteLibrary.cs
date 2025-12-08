using Laa.Monogame.Client.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Laa.Monogame.Client.UI;

public sealed class UiSpriteLibrary : IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly Dictionary<string, AtlasSpriteEntry> _entries;
    private readonly Dictionary<string, Texture2D> _textureCache = new(StringComparer.OrdinalIgnoreCase);

    public UiSpriteLibrary(GraphicsDevice graphicsDevice, IEnumerable<string> searchRoots)
    {
        _graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
        _entries = AtlasContentLoader.Load(searchRoots) ?? new Dictionary<string, AtlasSpriteEntry>(StringComparer.OrdinalIgnoreCase);
    }

    public bool TryGetSprite(string key, out UiSprite sprite)
    {
        sprite = default;
        if (string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        if (!_entries.TryGetValue(key, out var entry))
        {
            return false;
        }

        var texture = GetTexture(entry);
        sprite = new UiSprite(texture, new Rectangle(entry.X, entry.Y, entry.Width, entry.Height));
        return true;
    }

    private Texture2D GetTexture(AtlasSpriteEntry entry)
    {
        if (_textureCache.TryGetValue(entry.Atlas, out var cached))
        {
            return cached;
        }

        var atlasPath = Path.Combine(entry.Root, entry.Atlas);
        if (!File.Exists(atlasPath))
        {
            throw new FileNotFoundException($"Atlas image '{atlasPath}' not found.");
        }

        using var stream = File.OpenRead(atlasPath);
        var texture = Texture2D.FromStream(_graphicsDevice, stream);
        _textureCache[entry.Atlas] = texture;
        return texture;
    }

    public void Dispose()
    {
        foreach (var texture in _textureCache.Values)
        {
            texture.Dispose();
        }

        _textureCache.Clear();
    }
}

public readonly record struct UiSprite(Texture2D Texture, Rectangle Source);
