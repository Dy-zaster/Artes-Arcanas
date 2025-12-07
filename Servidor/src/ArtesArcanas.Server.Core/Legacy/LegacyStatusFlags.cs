namespace ArtesArcanas.Server.Core.Legacy;

public static class LegacyStatusFlags
{
    public const int Berserker = 0x0001;
    public const int Invisible = 0x0002;
    public const int Armor = 0x0004;
    public const int GiantStrength = 0x0008;
    public const int Haste = 0x0010;
    public const int Protection = 0x0020;
    public const int Stun = 0x0040;
    public const int Paralysis = 0x0080;

    public const int Vision = 0x0400;
    public const int Poison = 0x0800;

    public const int Zoomorphism = 0x10000;
    public const int Hidden = 0x20000;
    public const int Bandaged = 0x40000;
    public const int DefensiveMode = 0x80000;

    public const int ManaShield = 0x20000000;
    public const int ExtendedDuration = 0x02000000;
}
