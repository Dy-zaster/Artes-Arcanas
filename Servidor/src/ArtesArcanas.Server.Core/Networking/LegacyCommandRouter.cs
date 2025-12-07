using System.Threading.Channels;
using ArtesArcanas.Server.Core.Logging;

namespace ArtesArcanas.Server.Core.Networking;

public readonly record struct LegacyCommandBuffer(
    ushort SessionCode,
    byte[] Data,
    DateTime Timestamp);

public sealed class LegacyCommandRouter : ILegacyCommandSink, IAsyncDisposable
{
    private readonly Channel<LegacyCommandBuffer> _channel;

    public LegacyCommandRouter(IServerLogger logger, int capacity = 2048)
    {
        _ = logger;
        _channel = Channel.CreateBounded<LegacyCommandBuffer>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = false,
            SingleWriter = false
        });
    }

    public async Task EnqueueAsync(ushort sessionCode, ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
    {
        if (data.IsEmpty)
        {
            return;
        }

        var copy = data.ToArray();
        var command = new LegacyCommandBuffer(sessionCode, copy, DateTime.UtcNow);
        while (await _channel.Writer.WaitToWriteAsync(cancellationToken).ConfigureAwait(false))
        {
            if (_channel.Writer.TryWrite(command))
            {
                break;
            }
        }
    }

    public IAsyncEnumerable<LegacyCommandBuffer> ReadAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);

    public void Complete() => _channel.Writer.TryComplete();

    public ValueTask DisposeAsync()
    {
        Complete();
        return ValueTask.CompletedTask;
    }
}
