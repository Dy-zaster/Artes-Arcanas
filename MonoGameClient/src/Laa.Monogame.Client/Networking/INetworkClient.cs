using System;
using System.Threading.Tasks;

namespace Laa.Monogame.Client.Networking;

public interface INetworkClient : IDisposable
{
    event EventHandler<NetworkEventArgs>? MessageReceived;

    Task ConnectAsync(string host, int port);
    Task DisconnectAsync();
    Task SendAsync(ReadOnlyMemory<byte> payload);
}

public sealed class NetworkEventArgs : EventArgs
{
    public NetworkEventArgs(NetworkMessage message)
    {
        Message = message ?? throw new ArgumentNullException(nameof(message));
    }

    public NetworkMessage Message { get; }
}
