using System.Text.Json;
using System.Text.Json.Serialization;

namespace Laa.Server.Accounts;

public sealed class AccountStore
{
    private readonly string _root;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public AccountStore(string root)
    {
        _root = Directory.Exists(root) ? root : Directory.CreateDirectory(root).FullName;
    }

    public async Task<AccountRecord?> LoadAsync(string username, CancellationToken cancellationToken)
    {
        var path = GetPath(username);
        if (!File.Exists(path))
        {
            return null;
        }

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<AccountRecord>(stream, _jsonOptions, cancellationToken);
    }

    public async Task SaveAsync(AccountRecord account, CancellationToken cancellationToken)
    {
        var path = GetPath(account.Username);
        await using var stream = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.None);
        await JsonSerializer.SerializeAsync(stream, account, _jsonOptions, cancellationToken);
    }

    private string GetPath(string username)
    {
        var safe = username.Replace("..", string.Empty).Replace(Path.DirectorySeparatorChar, '_').Replace(Path.AltDirectorySeparatorChar, '_');
        return Path.Combine(_root, $"{safe}.json");
    }
}

public sealed record AccountRecord(
    string Username,
    string PasswordHash,
    List<CharacterRecord> Characters)
{
    [JsonIgnore]
    public bool HasSlot => Characters.Count < 5;
}

public sealed record CharacterRecord(
    string Name,
    byte Race,
    byte Class,
    uint PerkMask,
    byte StatStrength,
    byte StatConstitution,
    byte StatIntelligence,
    byte StatWisdom,
    byte StatDexterity,
    byte Evasion,
    ushort Level,
    uint Experience,
    byte MapId,
    byte PosX,
    byte PosY,
    ushort MaxMana,
    ushort Mana,
    ushort MaxHealth,
    ushort Health,
    uint Gold,
    uint Silver,
    byte FoodPercent,
    byte Honor,
    ushort Armor,
    ushort MagicResist,
    List<CharacterItem> Inventory,
    List<CharacterSpell> Spells);

public sealed record CharacterItem(
    int ItemId,
    ushort Amount,
    sbyte EquippedSlot);

public sealed record CharacterSpell(
    ushort SpellId);
