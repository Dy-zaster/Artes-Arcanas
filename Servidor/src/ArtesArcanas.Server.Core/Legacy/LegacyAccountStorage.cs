namespace ArtesArcanas.Server.Core.Legacy;

public sealed class LegacyAccountStorage
{
    private readonly string _avatarDirectory;

    public LegacyAccountStorage(string avatarDirectory)
    {
        _avatarDirectory = avatarDirectory;
        Directory.CreateDirectory(_avatarDirectory);
    }

    public string GetAccountPath(string login)
    {
        var fileName = $"{login}{LegacyConstants.AvatarFileExtension}";
        return Path.Combine(_avatarDirectory, fileName);
    }

    public async Task<LegacyUserAccount?> TryReadAsync(string login, CancellationToken cancellationToken)
    {
        var path = GetAccountPath(login);
        if (!File.Exists(path))
        {
            return null;
        }

        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
        return LegacyAccountSerializer.Read(stream);
    }

    public async Task WriteAsync(LegacyUserAccount account, CancellationToken cancellationToken)
    {
        var path = GetAccountPath(account.Data.Login);
        await using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
        LegacyAccountSerializer.Write(stream, account);
    }

    public bool Exists(string login) => File.Exists(GetAccountPath(login));
}
