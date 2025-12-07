using System.Text;

namespace ArtesArcanas.Server.Core.Legacy;

internal static class PascalString
{
    public static string Read(BinaryReader reader, int capacity)
    {
        var length = reader.ReadByte();
        var buffer = reader.ReadBytes(capacity);
        var actualLength = Math.Min(length, capacity);
        return LegacyConstants.LegacyEncoding.GetString(buffer, 0, actualLength);
    }

    public static void Write(BinaryWriter writer, string? value, int capacity)
    {
        var text = value ?? string.Empty;
        var encoded = LegacyConstants.LegacyEncoding.GetBytes(text);
        var actualLength = Math.Min(encoded.Length, capacity);

        writer.Write((byte)actualLength);

        if (actualLength > 0)
        {
            writer.Write(encoded, 0, actualLength);
        }

        var remaining = capacity - actualLength;
        if (remaining > 0)
        {
            Span<byte> padding = stackalloc byte[Math.Min(remaining, 64)];
            while (remaining > 0)
            {
                var chunk = Math.Min(padding.Length, remaining);
                writer.Write(padding[..chunk]);
                remaining -= chunk;
            }
        }
    }
}
