namespace ArtesArcanas.Server.Core.Legacy;

public sealed class LegacyUserAccount
{
    public LegacyUserAccount(LegacyUserData data, byte[] snapshot)
    {
        Data = data;
        CharacterSnapshot = snapshot;
    }

    public LegacyUserData Data { get; }
    public byte[] CharacterSnapshot { get; }
}
