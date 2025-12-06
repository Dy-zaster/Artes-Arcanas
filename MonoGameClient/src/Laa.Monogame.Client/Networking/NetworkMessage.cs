using System;

namespace Laa.Monogame.Client.Networking;

public sealed record NetworkMessage(NetworkMessageType Type, ReadOnlyMemory<byte> Payload);

public enum NetworkMessageType
{
    Unknown = 0,
    RawServerStream = 1,
    MonsterSpawn = 2,
    MonsterMove = 3,
    MonsterAttack = 4,
    MonsterDeath = 5
}
