using System.Collections.Generic;
using ArtesArcanas.Server.Core.Legacy;

namespace ArtesArcanas.Server.Core.World;

internal sealed class LegacyAreaSnapshotBuilder
{
    private const int MaxRefrescamientoX = 29;
    private const int MaxRefrescamientoY = 24;
    private const byte PacketLimit = 64;

    private readonly LegacyWorldState _world;

    public LegacyAreaSnapshotBuilder(LegacyWorldState world)
    {
        _world = world;
    }

    public List<byte[]> BuildRefreshPackets(LegacyPlayerContext origin, LegacyPlayerSnapshot snapshot)
    {
        var entries = GatherNearbyPlayers(snapshot);
        if (entries.Count == 0)
        {
            entries.Add(new AreaEntry(origin.Code, snapshot));
        }

        if (entries.Count == 1)
        {
            return new List<byte[]>
            {
                BuildSingleEntryPacket(entries[0])
            };
        }

        var packets = new List<byte[]>();
        var index = 0;
        while (index < entries.Count)
        {
            var packet = new List<byte>(capacity: 2 + PacketLimit * 5) { (byte)'r', 0 };
            var count = 0;

            while (index < entries.Count && count < PacketLimit)
            {
                var entry = entries[index++];
                AppendEntity(packet, entry);
                count++;
            }

            packet[1] = (byte)count;
            packets.Add(packet.ToArray());
        }

        return packets;
    }

    private List<AreaEntry> GatherNearbyPlayers(LegacyPlayerSnapshot originSnapshot)
    {
        var entries = new List<AreaEntry>();
        var originX = originSnapshot.CoordenadaX;
        var originY = originSnapshot.CoordenadaY;

        var players = _world.GetPlayersInArea(
            originSnapshot.CodigoMapa,
            originX,
            originY,
            MaxRefrescamientoX,
            MaxRefrescamientoY);

        foreach (var other in players)
        {
            var snapshot = other.Snapshot;
            if (snapshot.CodigoMapa != originSnapshot.CodigoMapa)
            {
                continue;
            }

            entries.Add(new AreaEntry(other.Code, snapshot));
        }

        return entries;
    }

    private static void AppendEntity(List<byte> buffer, AreaEntry entry)
    {
        buffer.Add((byte)(entry.Code & 0xFF));
        buffer.Add((byte)((entry.Code >> 8) & 0xFF));
        buffer.Add(entry.Snapshot.CoordenadaX);
        buffer.Add(entry.Snapshot.CoordenadaY);
        buffer.Add(ComposeDirection(entry.Snapshot));
    }

    private static byte[] BuildSingleEntryPacket(AreaEntry entry)
    {
        return new[]
        {
            (byte)'P',
            (byte)(entry.Code & 0xFF),
            (byte)((entry.Code >> 8) & 0xFF),
            entry.Snapshot.CoordenadaX,
            entry.Snapshot.CoordenadaY,
            ComposeDirection(entry.Snapshot)
        };
    }

    private static byte ComposeDirection(LegacyPlayerSnapshot snapshot) =>
        (byte)(snapshot.Direccion | ((snapshot.Accion & 0x0F) << 4));

    private readonly record struct AreaEntry(ushort Code, LegacyPlayerSnapshot Snapshot);
}
