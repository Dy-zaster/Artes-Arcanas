using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

const int DefaultAtlasSize = 2048;

var (sourceDir, outputDir, atlasSize) = ParseArguments(args);
var builder = new AtlasBuilder(sourceDir, outputDir, atlasSize);
await builder.BuildAsync();

static (string Source, string Output, int AtlasSize) ParseArguments(string[] args)
{
    string source = Path.Combine("..", "..", "Original Pascal", "Laa", "grf");
    string output = Path.Combine("..", "..", "MonoGameClient", "content", "graphics", "atlases");
    var size = DefaultAtlasSize;

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
            case "--help":
            case "-h":
                PrintUsage();
                Environment.Exit(0);
                break;
        }
    }

    return (source, output, size);
}

static void PrintUsage()
{
    Console.WriteLine("Usage: dotnet run -- [--source <path>] [--output <path>] [--atlas-size <pixels>]");
}

internal sealed class AtlasBuilder
{
    private readonly string _sourceRoot;
    private readonly string _outputRoot;
    private readonly int _atlasSize;

    public AtlasBuilder(string sourceRoot, string outputRoot, int atlasSize)
    {
        if (atlasSize <= 0) throw new ArgumentOutOfRangeException(nameof(atlasSize));
        _atlasSize = atlasSize;
        _sourceRoot = Path.GetFullPath(sourceRoot);
        _outputRoot = Path.GetFullPath(outputRoot);
    }

    public async Task BuildAsync()
    {
        if (!Directory.Exists(_sourceRoot))
        {
            throw new DirectoryNotFoundException($"Source directory '{_sourceRoot}' does not exist.");
        }

        Directory.CreateDirectory(_outputRoot);

        var files = Directory.EnumerateFiles(_sourceRoot, "*.*", SearchOption.TopDirectoryOnly)
            .Where(f => f.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
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
            using var image = (Bitmap)Image.FromFile(file);
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
