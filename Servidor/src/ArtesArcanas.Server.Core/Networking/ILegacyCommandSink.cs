namespace ArtesArcanas.Server.Core.Networking;

public interface ILegacyCommandSink
{
    Task EnqueueAsync(ushort sessionCode, ReadOnlyMemory<byte> data, CancellationToken cancellationToken);
}
