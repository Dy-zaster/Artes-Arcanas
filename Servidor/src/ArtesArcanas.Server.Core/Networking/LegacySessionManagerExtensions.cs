using System.Collections.Generic;
using ArtesArcanas.Server.Core.World;

namespace ArtesArcanas.Server.Core.Networking;

internal static class LegacySessionManagerExtensions
{
    public static async Task BroadcastToMapAsync(
        this LegacySessionManager sessions,
        LegacyWorldState world,
        byte mapId,
        ReadOnlyMemory<byte> payload,
        ushort? excludedCode = null)
    {
        var players = world.GetPlayersOnMap(mapId);
        if (players.Count == 0)
        {
            return;
        }

        List<Task>? tasks = null;
        foreach (var other in players)
        {
            if (excludedCode.HasValue && other.Code == excludedCode.Value)
            {
                continue;
            }

            tasks ??= new List<Task>();
            tasks.Add(sessions.SendToSessionAsync(other.Code, payload));
        }

        if (tasks is null || tasks.Count == 0)
        {
            return;
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }
}
