using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LegacyDataExtractor.Models;

namespace LegacyDataExtractor.Parsing;

public sealed class AnimationFileParser
{
    private const int MonsterFrameCount = 8;
    private const int PlayerFrameCount = 20;
    private const int MonsterRecordSize = 44;
    private const int PlayerRecordSize = 104;

    public AnimationDocument ParseDirectory(string directory)
    {
        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new ArgumentException("Source directory cannot be empty.", nameof(directory));
        }

        var fullDirectory = Path.GetFullPath(directory);
        if (!Directory.Exists(fullDirectory))
        {
            throw new DirectoryNotFoundException($"Animations directory '{fullDirectory}' does not exist.");
        }

        var files = Directory.GetFiles(fullDirectory, "*.cr9", SearchOption.TopDirectoryOnly)
            .OrderBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var animations = new List<AnimationEntry>(files.Length);
        foreach (var file in files)
        {
            var entry = ParseFile(file);
            animations.Add(entry);
        }

        return new AnimationDocument(animations);
    }

    private static AnimationEntry ParseFile(string filePath)
    {
        var key = Path.GetFileNameWithoutExtension(filePath) ?? Path.GetFileName(filePath);
        var bytes = File.ReadAllBytes(filePath);
        if (!TryDetectKind(bytes.Length, out var kind, out var directionCount, out var hasStyle))
        {
            throw new InvalidDataException($"Unable to detect animation layout for '{filePath}' (size {bytes.Length} bytes).");
        }

        var directionSize = kind == AnimationKind.Player ? PlayerRecordSize : MonsterRecordSize;
        var frameCount = kind == AnimationKind.Player ? PlayerFrameCount : MonsterFrameCount;
        var bytesForDirections = hasStyle ? bytes.Length - sizeof(int) : bytes.Length;

        using var stream = new MemoryStream(bytes, writable: false);
        using var reader = new BinaryReader(stream);

        var directions = new List<AnimationDirection>(directionCount);
        for (var i = 0; i < directionCount; i++)
        {
            directions.Add(ReadDirection(reader, frameCount));
        }

        if (stream.Position != bytesForDirections)
        {
            stream.Position = bytesForDirections;
        }

        var style = hasStyle ? reader.ReadInt32() : 0;

        return new AnimationEntry(
            key,
            kind,
            directions,
            style);
    }

    private static AnimationDirection ReadDirection(BinaryReader reader, int frameCount)
    {
        var maxWidth = reader.ReadInt16();
        var offsetX = reader.ReadByte();
        var offsetY = reader.ReadByte();

        var accumY = new short[frameCount];
        for (var i = 0; i < frameCount; i++)
        {
            accumY[i] = reader.ReadInt16();
        }

        var widths = reader.ReadBytes(frameCount);
        var centerX = reader.ReadBytes(frameCount);
        var centerY = reader.ReadBytes(frameCount);

        var frames = new List<AnimationFrame>(frameCount);
        for (var i = 0; i < frameCount; i++)
        {
            frames.Add(new AnimationFrame(accumY[i], widths[i], centerX[i], centerY[i]));
        }

        return new AnimationDirection(maxWidth, offsetX, offsetY, frames);
    }

    private static bool TryDetectKind(int length, out AnimationKind kind, out int directionCount, out bool hasStyle)
    {
        if (length <= 0)
        {
            kind = AnimationKind.Unknown;
            directionCount = 0;
            hasStyle = false;
            return false;
        }

        if (TryMatch(length, PlayerRecordSize, out directionCount, out hasStyle))
        {
            kind = AnimationKind.Player;
            return true;
        }

        if (TryMatch(length, MonsterRecordSize, out directionCount, out hasStyle))
        {
            kind = AnimationKind.Monster;
            return true;
        }

        kind = AnimationKind.Unknown;
        directionCount = 0;
        hasStyle = false;
        return false;
    }

    private static bool TryMatch(int length, int recordSize, out int directionCount, out bool hasStyle)
    {
        if (length % recordSize == 0)
        {
            directionCount = length / recordSize;
            hasStyle = false;
            return true;
        }

        if ((length - sizeof(int)) > 0 && (length - sizeof(int)) % recordSize == 0)
        {
            directionCount = (length - sizeof(int)) / recordSize;
            hasStyle = true;
            return true;
        }

        directionCount = 0;
        hasStyle = false;
        return false;
    }
}
