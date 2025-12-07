using System.Text;
using ArtesArcanas.Server.Core.Logging;

namespace ArtesArcanas.Server.Core.Networking;

public sealed class LegacyCommandLogSink : ILegacyCommandSink
{
    private readonly IServerLogger _logger;
    private readonly int _maxCommands;
    private readonly Queue<string> _recent = new();

    public LegacyCommandLogSink(IServerLogger logger, int maxCommands = 100)
    {
        _logger = logger;
        _maxCommands = Math.Max(1, maxCommands);
    }

    public Task EnqueueAsync(ushort sessionCode, ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
    {
        var hex = ToHex(data.Span);
        var entry = $"[{sessionCode}] {hex}";

        _recent.Enqueue(entry);
        while (_recent.Count > _maxCommands)
        {
            _recent.Dequeue();
        }

        _logger.Info($"[{sessionCode}] Comandos pendientes: {hex}");
        return Task.CompletedTask;
    }

    private static string ToHex(ReadOnlySpan<byte> span)
    {
        var builder = new StringBuilder(span.Length * 2);
        for (var i = 0; i < span.Length; i++)
        {
            builder.Append(span[i].ToString("X2"));
        }
        return builder.ToString();
    }
}
