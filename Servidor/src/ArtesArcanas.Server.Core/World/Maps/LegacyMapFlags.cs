namespace ArtesArcanas.Server.Core.World.Maps;

internal static class LegacyMapFlags
{
    public const ushort SafeZone = 0x0001;
    public const ushort CombatZone = 0x0002;
    public const ushort Indoor = 0x0010;
    public const ushort NoRain = 0x0020;
    public const ushort NoFog = 0x0040;
    public const ushort AlwaysNight = 0x0080;
    public const ushort Abyss = 0x0100;
    public const ushort SoundMask = 0xF000;
}
