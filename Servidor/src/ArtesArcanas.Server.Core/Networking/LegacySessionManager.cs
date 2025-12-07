using System.Collections.Concurrent;
using System.Linq;

namespace ArtesArcanas.Server.Core.Networking;

public sealed class LegacySessionManager
{
    private readonly ConcurrentDictionary<ushort, LegacyConnectionContext> _sessions = new();

    public IReadOnlyCollection<ushort> SessionCodes => _sessions.Keys.ToArray();

    internal void Register(LegacyConnectionContext context)
    {
        _sessions[context.Code] = context;
    }

    internal bool TryGetContext(ushort code, out LegacyConnectionContext? context) =>
        _sessions.TryGetValue(code, out context);

    public void Unregister(ushort code)
    {
        _sessions.TryRemove(code, out _);
    }

    public async Task BroadcastAsync(ReadOnlyMemory<byte> payload, ushort? excludedCode = null)
    {
        var tasks = new List<Task>(_sessions.Count);
        foreach (var kvp in _sessions)
        {
            if (excludedCode.HasValue && kvp.Key == excludedCode.Value)
            {
                continue;
            }

            tasks.Add(SafeSendAsync(kvp.Value, payload));
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    public async Task SendToSessionAsync(ushort code, ReadOnlyMemory<byte> payload)
    {
        if (_sessions.TryGetValue(code, out var context))
        {
            await SafeSendAsync(context, payload).ConfigureAwait(false);
        }
    }

    private static async Task SafeSendAsync(LegacyConnectionContext context, ReadOnlyMemory<byte> payload)
    {
        try
        {
            await context.SendPacketAsync(payload).ConfigureAwait(false);
        }
        catch
        {
            // el contexto probablemente se cerró; ignorar
        }
    }
}
