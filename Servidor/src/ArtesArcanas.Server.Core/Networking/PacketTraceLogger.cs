using System.Text;

namespace ArtesArcanas.Server.Core.Networking;

internal static class PacketTraceLogger
{
    private const string MarkerFileName = "trace_packets.txt";
    private const string LogFilePrefix = "packet-trace-";

    private static readonly object Sync = new();
    private static readonly string BaseDirectory = AppContext.BaseDirectory;

    public static bool IsEnabled { get; } = File.Exists(Path.Combine(BaseDirectory, MarkerFileName));
    public const int MaxPacketsPerSession = 128;

    public static void Log(ushort code, int packetIndex, ReadOnlySpan<byte> payload)
    {
        if (!IsEnabled)
        {
            return;
        }

        var path = Path.Combine(BaseDirectory, $"{LogFilePrefix}{code}.log");
        var builder = new StringBuilder();
        builder.Append(DateTime.UtcNow.ToString("O"));
        builder.Append(" packet ");
        builder.Append(packetIndex);
        builder.Append(" len=");
        builder.Append(payload.Length);
        if (!payload.IsEmpty)
        {
            builder.Append(" first='");
            builder.Append(ToPrintable(payload[0]));
            builder.Append("' (0x");
            builder.Append(payload[0].ToString("X2"));
            builder.Append(')');
        }
        builder.AppendLine();
        builder.AppendLine(RenderHexDump(payload));

        lock (Sync)
        {
            File.AppendAllText(path, builder.ToString());
        }
    }

    private static string RenderHexDump(ReadOnlySpan<byte> payload)
    {
        if (payload.IsEmpty)
        {
            return string.Empty;
        }

        var sb = new StringBuilder(payload.Length * 4);
        for (var offset = 0; offset < payload.Length; offset += 16)
        {
            sb.Append(offset.ToString("X4"));
            sb.Append(':');

            var chunk = Math.Min(16, payload.Length - offset);
            for (var i = 0; i < 16; i++)
            {
                sb.Append(' ');
                if (i < chunk)
                {
                    sb.Append(payload[offset + i].ToString("X2"));
                }
                else
                {
                    sb.Append("  ");
                }
            }

            sb.Append("  ");
            for (var i = 0; i < chunk; i++)
            {
                sb.Append(ToPrintable(payload[offset + i]));
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static char ToPrintable(byte value) =>
        value is >= 32 and <= 126 ? (char)value : '.';
}
