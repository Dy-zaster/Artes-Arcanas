using System;
using System.IO;

namespace ArtesArcanas.Server.Core.Legacy;

public sealed class LegacyAnimationMap
{
    private readonly byte[] _entries;

    private LegacyAnimationMap(byte[] entries)
    {
        _entries = entries;
    }

    public static LegacyAnimationMap Empty { get; } = new(Array.Empty<byte>());
    public bool IsEmpty => _entries.Length == 0;

    public static LegacyAnimationMap Load(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return Empty;
            }

            var bytes = File.ReadAllBytes(filePath);
            return bytes.Length == 0 ? Empty : new LegacyAnimationMap(bytes);
        }
        catch
        {
            return Empty;
        }
    }

    public byte Resolve(byte armorIndex, byte category, byte race, bool isFemale)
    {
        if (_entries.Length == 0)
        {
            return 0;
        }

        var genderBits = isFemale ? (1 << 11) : 0;
        var index = (armorIndex + (category << 5) + (race << 8) + genderBits) & 0x0FFF;
        if ((uint)index >= (uint)_entries.Length)
        {
            index %= _entries.Length;
        }

        return _entries[index];
    }
}
