using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;

const int DefaultAtlasSize = 2048;

var (sourceDir, outputDir, atlasSize, chromaKey) = ParseArguments(args);
var builder = new AtlasBuilder(sourceDir, outputDir, atlasSize, chromaKey);
await builder.BuildAsync();

static (string Source, string Output, int AtlasSize, Color? ChromaKey) ParseArguments(string[] args)
{
    string source = Path.Combine("..", "..", "Original Pascal", "Laa", "grf");
    string output = Path.Combine("..", "..", "MonoGameClient", "content", "graphics", "atlases");
    var size = DefaultAtlasSize;
    Color? chromaKey = Color.Black;

    for (var i = 0; i < args.Length; i++)
    {
        switch (args[i])
        {
            case "--source" when i + 1 < args.Length:
                source = args[++i];
                break;
            case "--output" when i + 1 < args.Length:
                output = args[++i];
                break;
            case "--atlas-size" when i + 1 < args.Length && int.TryParse(args[i + 1], out var parsed):
                size = parsed;
                i++;
                break;
            case "--chroma-key" when i + 1 < args.Length:
                if (TryParseColor(args[++i], out var parsedColor))
                {
                    chromaKey = parsedColor;
                }
                else if (string.Equals(args[i], "none", StringComparison.OrdinalIgnoreCase))
                {
                    chromaKey = null;
                }
                break;
            case "--help":
            case "-h":
                PrintUsage();
                Environment.Exit(0);
                break;
        }
    }

    return (source, output, size, chromaKey);
}

static void PrintUsage()
{
    Console.WriteLine("Usage: dotnet run -- [--source <path>] [--output <path>] [--atlas-size <pixels>] [--chroma-key <hex|none>]");
}

static bool TryParseColor(string text, out Color? color)
{
    color = null;
    if (string.IsNullOrWhiteSpace(text))
    {
        return false;
    }

    text = text.Trim();
    if (text.StartsWith("#", StringComparison.Ordinal))
    {
        text = text[1..];
    }

    if (text.Length is not 6 and not 8)
    {
        return false;
    }

    if (!int.TryParse(text, System.Globalization.NumberStyles.HexNumber, null, out var value))
    {
        return false;
    }

    byte a = 255;
    int offset = 0;
    if (text.Length == 8)
    {
        a = (byte)((value >> 24) & 0xFF);
        offset = 2;
    }

    var r = (byte)((value >> (16 - offset * 8)) & 0xFF);
    var g = (byte)((value >> (8 - offset * 8)) & 0xFF);
    var b = (byte)(value & 0xFF);
    color = Color.FromArgb(a, r, g, b);
    return true;
}

internal sealed class AtlasBuilder
{
    private readonly string _sourceRoot;
    private readonly string _outputRoot;
    private readonly int _atlasSize;
    private readonly Color? _chromaKey;

    public AtlasBuilder(string sourceRoot, string outputRoot, int atlasSize, Color? chromaKey)
    {
        if (atlasSize <= 0) throw new ArgumentOutOfRangeException(nameof(atlasSize));
        _atlasSize = atlasSize;
        _sourceRoot = Path.GetFullPath(sourceRoot);
        _outputRoot = Path.GetFullPath(outputRoot);
        _chromaKey = chromaKey;
    }

    private static bool IsSupportedTexture(string path)
    {
        return path.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase);
    }

    public async Task BuildAsync()
    {
        if (!Directory.Exists(_sourceRoot))
        {
            throw new DirectoryNotFoundException($"Source directory '{_sourceRoot}' does not exist.");
        }

        Directory.CreateDirectory(_outputRoot);

var files = Directory.EnumerateFiles(_sourceRoot, "*.*", SearchOption.TopDirectoryOnly)
    .Where(IsSupportedTexture)
    .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
    .ToList();

        if (files.Count == 0)
        {
            Console.WriteLine("No BMP or PNG files were found under the source directory. Nothing to do.");
            return;
        }

        var atlasFiles = new List<string>();
        var sprites = new List<AtlasSprite>();

        var atlasIndex = 0;
        var atlas = CreateAtlas(atlasIndex);
        var currentX = 0;
        var currentY = 0;
        var rowHeight = 0;

        foreach (var file in files)
        {
            using var original = (Bitmap)Image.FromFile(file);
            using var image = PrepareBitmap(original);
            ApplyChromaKey(image);
            if (image.Width > _atlasSize || image.Height > _atlasSize)
            {
                Console.WriteLine($"Skipping '{Path.GetFileName(file)}' because it exceeds the atlas size {_atlasSize}x{_atlasSize}.");
                continue;
            }

            if (currentX + image.Width > _atlasSize)
            {
                currentX = 0;
                currentY += rowHeight;
                rowHeight = 0;
            }

            if (currentY + image.Height > _atlasSize)
            {
                SaveAtlas(atlas, atlasFiles);
                atlasIndex++;
                atlas = CreateAtlas(atlasIndex);
                currentX = 0;
                currentY = 0;
                rowHeight = 0;
            }

            atlas.Graphics.DrawImage(image, new Rectangle(currentX, currentY, image.Width, image.Height));
            rowHeight = Math.Max(rowHeight, image.Height);

            var key = Path.GetFileNameWithoutExtension(file);
            sprites.Add(new AtlasSprite(
                key,
                atlas.FileName,
                currentX,
                currentY,
                image.Width,
                image.Height));

            currentX += image.Width;
        }

        SaveAtlas(atlas, atlasFiles);

        var manifest = new AtlasManifest(
            _atlasSize,
            atlasFiles,
            sprites);

        var manifestPath = Path.Combine(_outputRoot, "atlas_manifest.json");
        await using var stream = File.Create(manifestPath);
        await JsonSerializer.SerializeAsync(stream, manifest, new JsonSerializerOptions { WriteIndented = true });

        Console.WriteLine($"Generated {atlasFiles.Count} atlas(es) with {sprites.Count} sprites.");
    }

    private AtlasCanvas CreateAtlas(int index)
    {
        var fileName = $"atlas_{index:D2}.png";
        var bitmap = new Bitmap(_atlasSize, _atlasSize, PixelFormat.Format32bppArgb);
        var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.Transparent);
        return new AtlasCanvas(bitmap, graphics, fileName);
    }

    private void SaveAtlas(AtlasCanvas atlas, List<string> atlasFiles)
    {
        var path = Path.Combine(_outputRoot, atlas.FileName);
        atlas.Graphics.Flush();
        atlas.Bitmap.Save(path, ImageFormat.Png);
        atlas.Dispose();
        atlasFiles.Add(atlas.FileName);
    }

    private static Bitmap PrepareBitmap(Bitmap source)
    {
        if (source.PixelFormat == PixelFormat.Format32bppArgb)
        {
            return (Bitmap)source.Clone();
        }

        var clone = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(clone);
        graphics.DrawImage(source, new Rectangle(0, 0, source.Width, source.Height));
        return clone;
    }

    private void ApplyChromaKey(Bitmap image)
    {
        if (_chromaKey is null)
        {
            return;
        }

        var key = _chromaKey.Value;
        var rect = new Rectangle(0, 0, image.Width, image.Height);
        var data = image.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
        var length = Math.Abs(data.Stride) * data.Height;
        var buffer = new byte[length];
        System.Runtime.InteropServices.Marshal.Copy(data.Scan0, buffer, 0, length);

        for (var y = 0; y < data.Height; y++)
        {
            var rowStart = y * data.Stride;
            for (var x = 0; x < data.Width; x++)
            {
                var index = rowStart + x * 4;
                var b = buffer[index];
                var g = buffer[index + 1];
                var r = buffer[index + 2];
                var a = buffer[index + 3];
                if (a == 0)
                {
                    continue;
                }

                if (r == key.R && g == key.G && b == key.B)
                {
                    buffer[index + 3] = 0;
                }
            }
        }

        System.Runtime.InteropServices.Marshal.Copy(buffer, 0, data.Scan0, length);
        image.UnlockBits(data);
    }
}

internal sealed record AtlasCanvas(Bitmap Bitmap, Graphics Graphics, string FileName) : IDisposable
{
    public void Dispose()
    {
        Graphics.Dispose();
        Bitmap.Dispose();
    }
}
internal sealed record AtlasManifest(int AtlasSize, IReadOnlyList<string> Atlases, IReadOnlyList<AtlasSprite> Sprites);
internal sealed record AtlasSprite(string Key, string Atlas, int X, int Y, int Width, int Height);
