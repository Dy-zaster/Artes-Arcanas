using System.Threading;
using ArtesArcanas.Server.Core.Legacy;

namespace ArtesArcanas.Server.Core.World;

internal sealed class LegacyMonsterInstance
{
    private int _moveCooldown;

    public LegacyMonsterInstance(
        ushort code,
        byte mapId,
        byte x,
        byte y,
        byte monsterType,
        bool isMerchant,
        LegacyMonsterDescriptor descriptor)
    {
        Code = code;
        MapId = mapId;
        X = x;
        Y = y;
        MonsterType = monsterType;
        IsMerchant = isMerchant;
        Descriptor = descriptor;
        Animation = monsterType;
        Direction = 0;
        Action = 0;
        Flags = 0;
        ResetMoveCooldown();
    }

    public ushort Code { get; }
    public byte MapId { get; }
    public byte X { get; private set; }
    public byte Y { get; private set; }
    public byte Direction { get; private set; }
    public byte Action { get; private set; }
    public byte Animation { get; }
    public ushort Flags { get; private set; }
    public byte MonsterType { get; }
    public bool IsMerchant { get; }
    public LegacyMonsterDescriptor Descriptor { get; }

    public void SetPosition(byte x, byte y, byte direction)
    {
        X = x;
        Y = y;
        Direction = direction;
    }

    public void SetFlags(ushort flags)
    {
        Flags = flags;
    }

    public bool ShouldAttemptMove(int deltaTicks)
    {
        var current = Interlocked.Add(ref _moveCooldown, -deltaTicks);
        return current <= 0;
    }

    public void ResetMoveCooldown(int minimum = 6, int maximum = 18)
    {
        var value = LegacyRandom.Next(minimum, maximum);
        Interlocked.Exchange(ref _moveCooldown, value);
    }
}
