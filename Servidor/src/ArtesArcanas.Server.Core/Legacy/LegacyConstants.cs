using System.Text;

namespace ArtesArcanas.Server.Core.Legacy;

internal static class LegacyConstants
{
    public const int MaxPlayers = 255;
    public const int MaxMonsters = 8191;
    public const int MaxClans = 249;
    public const byte NoClanId = 0xFF;
    public const int MaxAdministrators = 15;
    public const int MaxMaps = 254;
    public const int MaxMerchants = 15;
    public const int MaxTimers = 16;
    public const int EquipmentSlots = 8;
    public const int InventoryArtifactSlots = 30; // MAX_ARTEFACTOS+1
    public const int ChestSlots = InventoryArtifactSlots;
    public const int PartySlots = 4;

    public const int PlayerInstanceSize = 296;
    public const int PlayerSnapshotSize = PlayerInstanceSize - 4;

    public const string AvatarDirectoryName = "Avatares";
    public const int ClanRecordSize = 64;
    public const int CastleRecordSize = 12;
    public const int MerchantInflationSize = InventoryArtifactSlots;
    public const uint DefaultClanBanner = 0x8000_0000;
    public const string AvatarFileExtension = ".avt";
    public const byte MaxNewbieLevel = 6;
    public const byte MaxLevelWithBonus = 24;
    public const sbyte HeroBehaviorThreshold = 100;
    public const uint SkillElocution = 0x0200;

    public const byte LegacyClientVersion = 5;

    public static readonly Encoding LegacyEncoding = Encoding.Latin1;
}
