using System.Text;

namespace ArtesArcanas.Server.Core.Legacy;

internal static class LegacyBinaryEncoding
{
    public static void AppendB2(StringBuilder builder, ushort value)
    {
        builder.Append((char)(value & 0xFF));
        builder.Append((char)((value >> 8) & 0xFF));
    }

    public static void AppendB3(StringBuilder builder, int value)
    {
        builder.Append((char)(value & 0xFF));
        builder.Append((char)((value >> 8) & 0xFF));
        builder.Append((char)((value >> 16) & 0xFF));
    }

    public static void AppendB4(StringBuilder builder, uint value)
    {
        builder.Append((char)(value & 0xFF));
        builder.Append((char)((value >> 8) & 0xFF));
        builder.Append((char)((value >> 16) & 0xFF));
        builder.Append((char)((value >> 24) & 0xFF));
    }

    public static void AppendInventory(StringBuilder builder, ReadOnlySpan<byte> data)
    {
        for (var i = 0; i < data.Length; i++)
        {
            builder.Append((char)data[i]);
        }
    }
}
