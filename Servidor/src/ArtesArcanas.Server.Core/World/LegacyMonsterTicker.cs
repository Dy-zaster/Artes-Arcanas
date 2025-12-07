using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ArtesArcanas.Server.Core.Networking;
using ArtesArcanas.Server.Core.World.Maps;

namespace ArtesArcanas.Server.Core.World;

internal sealed class LegacyMonsterTicker : ILegacyWorldTicker
{
    private static readonly sbyte[] DeltaX = { 0, 0, -1, 1, 1, -1, -1, 1 };
    private static readonly sbyte[] DeltaY = { -1, 1, 0, 0, -1, 1, -1, 1 };

    private readonly LegacyWorldState _world;
    private readonly LegacySessionManager _sessions;

    public LegacyMonsterTicker(LegacyWorldState world, LegacySessionManager sessions)
    {
        _world = world;
        _sessions = sessions;
    }

    public async Task TickAsync(LegacyWorldState world, CancellationToken cancellationToken)
    {
        var monsters = world.GetAllMonsters();
        List<Task>? broadcasts = null;

        foreach (var monster in monsters)
        {
            if (monster.IsMerchant)
            {
                continue;
            }

            if (!monster.ShouldAttemptMove(1))
            {
                continue;
            }

            var direction = (byte)LegacyRandom.Next(0, 8);
            var targetX = ClampCoordinate(monster.X, DeltaX[direction], LegacyMapGrid.ExpandedWidth);
            var targetY = ClampCoordinate(monster.Y, DeltaY[direction], LegacyMapGrid.ExpandedHeight);

            if (world.TryMoveMonster(monster, targetX, targetY, direction))
            {
                monster.ResetMoveCooldown();
                var payload = LegacyWorldPacketFactory.BuildMonsterMovementPacket(monster);
                broadcasts ??= new List<Task>();
                broadcasts.Add(_sessions.BroadcastToMapAsync(world, monster.MapId, payload));
            }
            else
            {
                monster.ResetMoveCooldown();
            }
        }

        if (broadcasts is not null && broadcasts.Count > 0)
        {
            await Task.WhenAll(broadcasts).ConfigureAwait(false);
        }
    }

    private static byte ClampCoordinate(byte origin, sbyte delta, int limit)
    {
        var value = origin + delta;
        if (value < 0)
        {
            return 0;
        }

        if (value >= limit)
        {
            return (byte)(limit - 1);
        }

        return (byte)value;
    }
}
