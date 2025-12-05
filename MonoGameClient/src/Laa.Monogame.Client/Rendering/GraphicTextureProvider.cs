using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Laa.Content.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Laa.Monogame.Client.Rendering;

public sealed class GraphicTextureProvider : IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly GraphicDocument _document;
    private readonly List<string> _searchRoots;
    private readonly Dictionary<int, GraphicTextureEntry> _cache = new();
    private readonly Dictionary<string, AtlasSpriteEntry> _atlasEntries;
    private readonly Dictionary<string, Texture2D> _atlasTextures = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<Texture2D> _ownedTextures = new();

    public GraphicTextureProvider(GraphicsDevice graphicsDevice, GraphicDocument document, IEnumerable<string> searchRoots)
    {
        _graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
        _document = document ?? throw new ArgumentNullException(nameof(document));
        _searchRoots = searchRoots?.Select(Path.GetFullPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(Directory.Exists)
            .ToList() ?? new List<string>();
        _atlasEntries = AtlasContentLoader.Load(_searchRoots);
    }

    public bool TryGetTexture(int descriptorIndex, out GraphicTextureEntry entry)
    {
        entry = default;
        if (descriptorIndex < 0 || descriptorIndex >= _document.Descriptors.Count)
        {
            return false;
        }

        if (_cache.TryGetValue(descriptorIndex, out var cached))
        {
            entry = cached;
            return true;
        }

        var descriptor = _document.Descriptors[descriptorIndex];
        var key = ResolveTextureKey(descriptorIndex);
        if (key is not null && _atlasEntries.TryGetValue(key, out var atlasSprite))
        {
            var atlasTexture = GetAtlasTexture(atlasSprite);
            var atlasEntry = new GraphicTextureEntry(
                atlasTexture,
                new Rectangle(atlasSprite.X, atlasSprite.Y, atlasSprite.Width, atlasSprite.Height),
                descriptor.PosX,
                descriptor.PosY,
                descriptor.ReflectedPosX,
                descriptor.AlignY);
            _cache[descriptorIndex] = atlasEntry;
            entry = atlasEntry;
            return true;
        }

        var fileName = ResolveFileName(descriptorIndex);
        if (fileName is null)
        {
            return false;
        }

        var filePath = FindExistingFile(fileName);
        if (filePath is null)
        {
            return false;
        }

        using var stream = File.OpenRead(filePath);
        var texture = Texture2D.FromStream(_graphicsDevice, stream);
        var extension = Path.GetExtension(filePath);
        if (extension.Equals(".bmp", StringComparison.OrdinalIgnoreCase))
        {
            ApplyLegacyChromaKey(texture);
        }
        _ownedTextures.Add(texture);

            var created = new GraphicTextureEntry(
                texture,
                null,
                descriptor.PosX,
                descriptor.PosY,
                descriptor.ReflectedPosX,
                descriptor.AlignY);

        _cache[descriptorIndex] = created;
        entry = created;
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
        ApplyLegacyChromaKey(loaded);
        _atlasTextures[sprite.Atlas] = loaded;
        return loaded;
    }

    private string? FindExistingFile(string fileName)
    {
        foreach (var root in _searchRoots)
        {
            var bmpPath = Path.Combine(root, fileName);
            if (File.Exists(bmpPath))
            {
                return bmpPath;
            }

            var pngCandidate = Path.ChangeExtension(bmpPath, ".png");
            if (File.Exists(pngCandidate))
            {
                return pngCandidate;
            }
        }

        return null;
    }

    private static string? ResolveFileName(int descriptorIndex)
    {
        if (descriptorIndex < 0)
        {
            return null;
        }

        if (descriptorIndex < 256)
        {
            return $"{descriptorIndex}.bmp";
        }

        if (descriptorIndex < 512)
        {
            return $"c{descriptorIndex - 256}.bmp";
        }

        return $"x{descriptorIndex - 512}.bmp";
    }

    private static string? ResolveTextureKey(int descriptorIndex)
    {
        if (descriptorIndex < 0)
        {
            return null;
        }

        if (descriptorIndex < 256)
        {
            return descriptorIndex.ToString();
        }

        if (descriptorIndex < 512)
        {
            return $"c{descriptorIndex - 256}";
        }

        return $"x{descriptorIndex - 512}";
    }

    private static void ApplyLegacyChromaKey(Texture2D texture)
    {
        if (texture is null)
        {
            return;
        }

        var totalPixels = texture.Width * texture.Height;
        if (totalPixels == 0)
        {
            return;
        }

        var data = new Color[totalPixels];
        texture.GetData(data);
        var modified = false;
        for (var i = 0; i < data.Length; i++)
        {
            var color = data[i];
            if (color.A == 0)
            {
                continue;
            }

            if (color.R == 0 && color.G == 0 && color.B == 0)
            {
                data[i] = new Color(0, 0, 0, 0);
                modified = true;
            }
        }

        if (modified)
        {
            texture.SetData(data);
        }
    }

    public void Dispose()
    {
        foreach (var texture in _ownedTextures)
        {
            texture.Dispose();
        }

        foreach (var texture in _atlasTextures.Values)
        {
            texture.Dispose();
        }

        _ownedTextures.Clear();
        _atlasTextures.Clear();
        _cache.Clear();
    }
}

public sealed record GraphicTextureEntry(Texture2D Texture, Rectangle? SourceRectangle, int OffsetX, int OffsetY, int ReflectedOffsetX, byte AlignY);
