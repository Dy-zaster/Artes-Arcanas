namespace ArtesArcanas.Server.Core.Legacy;

internal static class LegacyAccountSerializer
{
    public static LegacyUserAccount Read(Stream stream)
    {
        var reader = new BinaryReader(stream, LegacyConstants.LegacyEncoding, leaveOpen: true);
        var data = new LegacyUserData
        {
            Version = reader.ReadByte(),
            State = (LegacyUserState)reader.ReadByte(),
            CreationDay = reader.ReadUInt16(),
            Permissions = (LegacyUserPermissions)reader.ReadUInt32(),
            LastIp = reader.ReadInt32(),
            Login = PascalString.Read(reader, 16),
            ChatPenalty = reader.ReadByte(),
            LastLoginDay = reader.ReadUInt16()
        };

        var password = reader.ReadBytes(32);
        if (password.Length != 32)
        {
            throw new InvalidDataException("Archivo de cuenta incompleto (password).");
        }

        password.CopyTo(data.PasswordHash, 0);

        data.ClanIdentifier = reader.ReadInt32();
        data.ServerIdentifier = reader.ReadInt32();
        data.ProcessBufferedCommands = reader.ReadByte() != 0;
        data.IdleKickTimer = reader.ReadByte();
        data.Reserved = reader.ReadUInt16();

        var snapshot = reader.ReadBytes(LegacyConstants.PlayerSnapshotSize);
        if (snapshot.Length != LegacyConstants.PlayerSnapshotSize)
        {
            throw new InvalidDataException("Archivo de cuenta incompleto (personaje).");
        }

        return new LegacyUserAccount(data, snapshot);
    }

    public static void Write(Stream stream, LegacyUserAccount account)
    {
        if (account.CharacterSnapshot.Length != LegacyConstants.PlayerSnapshotSize)
        {
            throw new InvalidOperationException("Tamaño inválido del snapshot del personaje.");
        }

        var writer = new BinaryWriter(stream, LegacyConstants.LegacyEncoding, leaveOpen: true);
        var data = account.Data;

        writer.Write(data.Version);
        writer.Write((byte)data.State);
        writer.Write(data.CreationDay);
        writer.Write((uint)data.Permissions);
        writer.Write(data.LastIp);
        PascalString.Write(writer, data.Login, 16);
        writer.Write(data.ChatPenalty);
        writer.Write(data.LastLoginDay);
        writer.Write(data.PasswordHash);
        writer.Write(data.ClanIdentifier);
        writer.Write(data.ServerIdentifier);
        writer.Write(data.ProcessBufferedCommands ? (byte)1 : (byte)0);
        writer.Write(data.IdleKickTimer);
        writer.Write(data.Reserved);
        writer.Write(account.CharacterSnapshot);
        writer.Flush();
    }
}
