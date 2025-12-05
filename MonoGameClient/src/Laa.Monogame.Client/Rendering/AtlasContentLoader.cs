using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Laa.Monogame.Client.Rendering;

internal static class AtlasContentLoader
{
    public static Dictionary<string, AtlasSpriteEntry> Load(IEnumerable<string> searchRoots)
    {
        var entries = new Dictionary<string, AtlasSpriteEntry>(StringComparer.OrdinalIgnoreCase);
        if (searchRoots is null)
        {
            return entries;
        }

        foreach (var root in searchRoots)
        {
            if (string.IsNullOrWhiteSpace(root))
            {
                continue;
            }

            var manifestPath = ResolveManifestPath(root);
            if (manifestPath is null)
            {
                continue;
            }

            try
            {
                using var stream = File.OpenRead(manifestPath);
                var manifest = JsonSerializer.Deserialize<AtlasManifest>(stream, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (manifest is null)
                {
                    continue;
                }

                var manifestRoot = Path.GetDirectoryName(manifestPath) ?? root;
                foreach (var sprite in manifest.Sprites)
                {
                    if (string.IsNullOrWhiteSpace(sprite.Key))
                    {
                        continue;
                    }

                    entries[sprite.Key] = new AtlasSpriteEntry(
                        sprite.Key,
                        sprite.Atlas,
                        manifestRoot,
                        sprite.X,
                        sprite.Y,
                        sprite.Width,
                        sprite.Height);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to read atlas manifest '{manifestPath}': {ex.Message}");
            }
        }

        return entries;
    }

    private static string? ResolveManifestPath(string root)
    {
        var fullRoot = Path.GetFullPath(root);
        var manifestPath = Path.Combine(fullRoot, "atlas_manifest.json");
        if (File.Exists(manifestPath))
        {
            return manifestPath;
        }

        manifestPath = Path.Combine(fullRoot, "atlases", "atlas_manifest.json");
        return File.Exists(manifestPath) ? manifestPath : null;
    }
}

internal sealed record AtlasManifest(int AtlasSize, IReadOnlyList<string> Atlases, IReadOnlyList<AtlasSprite> Sprites);

internal sealed record AtlasSprite(string Key, string Atlas, int X, int Y, int Width, int Height);

internal sealed record AtlasSpriteEntry(string Key, string Atlas, string Root, int X, int Y, int Width, int Height);
