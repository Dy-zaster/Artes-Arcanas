using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Laa.Monogame.Client.Networking;

internal sealed class PlayerCommandQueue
{
    private readonly Queue<ReadOnlyMemory<byte>> _commands = new();
    private readonly object _sync = new();

    public void Enqueue(ReadOnlyMemory<byte> command)
    {
        if (command.IsEmpty)
        {
            return;
        }

        var copy = new byte[command.Length];
        command.Span.CopyTo(copy);
        lock (_sync)
        {
            _commands.Enqueue(copy);
        }
    }

    public async Task FlushAsync(INetworkClient? client)
    {
        if (client is null)
        {
            return;
        }

        while (true)
        {
            ReadOnlyMemory<byte>? next = null;
            lock (_sync)
            {
                if (_commands.Count > 0)
                {
                    next = _commands.Dequeue();
                }
            }

            if (next is null)
            {
                break;
            }

            await client.SendAsync(next.Value).ConfigureAwait(false);
        }
    }
}
