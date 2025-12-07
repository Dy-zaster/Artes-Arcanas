namespace ArtesArcanas.Server.Core.Legacy;

public sealed record LegacyAdministrator(string Login, LegacyUserState State);

public sealed class LegacyAdministratorRegistry
{
    private readonly List<LegacyAdministrator> _administrators = new(LegacyConstants.MaxAdministrators + 1);

    public IReadOnlyList<LegacyAdministrator> Administrators => _administrators;
    public int ServerIdentifier { get; private set; }

    public static LegacyAdministratorRegistry Load(string path)
    {
        var registry = new LegacyAdministratorRegistry();

        if (!File.Exists(path))
        {
            return registry;
        }

        using var stream = File.OpenRead(path);
        var reader = new BinaryReader(stream, LegacyConstants.LegacyEncoding, leaveOpen: false);

        var count = reader.ReadByte();
        reader.ReadByte(); // unused
        reader.ReadUInt16(); // unused
        registry.ServerIdentifier = reader.ReadInt32();
        reader.ReadInt32(); // unused
        reader.ReadInt32(); // unused

        count = Math.Min(count, (byte)(LegacyConstants.MaxAdministrators + 1));
        for (var i = 0; i < count; i++)
        {
            var login = PascalString.Read(reader, 16);
            var state = (LegacyUserState)reader.ReadByte();
            reader.ReadUInt16(); // unused
            reader.ReadInt32(); // unused

            if (!string.IsNullOrWhiteSpace(login))
            {
                registry._administrators.Add(new LegacyAdministrator(login, state));
            }
        }

        return registry;
    }

    public bool TryGetAdministrator(string login, out LegacyAdministrator? administrator)
    {
        administrator = _administrators.FirstOrDefault(adm => adm.Login.Equals(login, StringComparison.OrdinalIgnoreCase));
        return administrator is not null;
    }
}
