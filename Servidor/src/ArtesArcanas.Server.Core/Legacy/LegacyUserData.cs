namespace ArtesArcanas.Server.Core.Legacy;

[Flags]
public enum LegacyUserPermissions : uint
{
    None = 0,
    CleanMaps = 0x0001,
    Teleport = 0x0002,
    DeleteBags = 0x0004,
    CreateItems = 0x0008,
    ControlMonsters = 0x0010,
    SummonMonsters = 0x0020,
    DissolveMonsters = 0x0040,
    SearchPlayers = 0x0100,
    VisitPlayers = 0x0200,
    SummonPlayers = 0x0400,
    JailPlayers = 0x0800,
    RestorePlayers = 0x1000,
    KickPlayers = 0x2000,
    DisbandClan = 0x10000,
    ModerateChat = 0x20000,

    Moderator = ModerateChat | CleanMaps | RestorePlayers | SearchPlayers | VisitPlayers |
                ControlMonsters | Teleport | JailPlayers,
    Admin = Moderator | DeleteBags | SummonMonsters | DissolveMonsters | SummonPlayers |
            KickPlayers | DisbandClan,
    GameMaster = Admin | CreateItems,
    TestServer = SearchPlayers | VisitPlayers | Teleport | CreateItems
}

public enum LegacyUserState : byte
{
    NoConectado = 0,
    NoAutentificado = 1,
    Autentificado = 2,
    Baneado = 3,
    Normal = 4,
    Moderador = 5,
    Admin = 6,
    GameMaster = 7
}

public sealed class LegacyUserData
{
    public byte Version { get; set; }
    public LegacyUserState State { get; set; }
    public ushort CreationDay { get; set; }
    public LegacyUserPermissions Permissions { get; set; }
    public int LastIp { get; set; }
    public string Login { get; set; } = string.Empty;
    public byte ChatPenalty { get; set; }
    public ushort LastLoginDay { get; set; }
    public byte[] PasswordHash { get; } = new byte[32];
    public int ClanIdentifier { get; set; }
    public int ServerIdentifier { get; set; }
    public bool ProcessBufferedCommands { get; set; }
    public byte IdleKickTimer { get; set; }
    public ushort Reserved { get; set; }
}
