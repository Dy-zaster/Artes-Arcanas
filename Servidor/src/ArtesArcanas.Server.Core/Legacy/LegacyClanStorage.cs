using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
namespace ArtesArcanas.Server.Core.Legacy;

public sealed record LegacyClanBanner(uint ColorPrimary, uint ColorSecondary);

public sealed class LegacyClanInfo
{
    public byte Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Leader { get; init; } = string.Empty;
    public byte Color { get; init; } = 255;
    public ushort ActiveMembers { get; init; }
    public int ClanIdentifier { get; init; }
    public LegacyClanBanner Banner { get; init; } = new(LegacyConstants.DefaultClanBanner, LegacyConstants.DefaultClanBanner);
}

public sealed class LegacyClanStorage
{
    private readonly object _sync = new();
    private readonly string _backingFile;
    private List<LegacyClanInfo> _clans = new(LegacyConstants.MaxClans + 1);

    public IReadOnlyList<LegacyClanInfo> Clans => _clans;

    public static LegacyClanStorage Load(string path)
    {
        var storage = new LegacyClanStorage(path);

        if (!File.Exists(path))
        {
            return storage;
        }

        storage.ReadFile();
        return storage;
    }

    private LegacyClanStorage(string backingFile)
    {
        _backingFile = backingFile;
    }

    public bool TryCreateClan(string leaderName, string? proposedName, out LegacyClanInfo? clan, out string? error)
    {
        clan = null;
        error = null;

        var trimmedLeader = (leaderName ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(trimmedLeader))
        {
            error = "Nombre del líder inválido.";
            return false;
        }

        var desiredName = string.IsNullOrWhiteSpace(proposedName)
            ? $"Clan de {trimmedLeader}"
            : proposedName.Trim();

        var normalizedName = NormalizeClanName(desiredName);
        if (string.IsNullOrEmpty(normalizedName))
        {
            error = "Nombre del clan inválido.";
            return false;
        }

        lock (_sync)
        {
            var nextId = FindAvailableId(_clans);
            if (nextId is null)
            {
                error = "No hay espacio para crear nuevos clanes.";
                return false;
            }

            if (ContainsSimilarNameLocked(normalizedName, nextId.Value))
            {
                error = "Ya existe un clan con un nombre similar.";
                return false;
            }

            var identifier = GenerateClanIdentifier(trimmedLeader);
            var newClan = new LegacyClanInfo
            {
                Id = nextId.Value,
                Leader = trimmedLeader,
                Name = desiredName,
                Color = 0,
                ActiveMembers = 1,
                ClanIdentifier = identifier,
                Banner = new LegacyClanBanner(LegacyConstants.DefaultClanBanner, LegacyConstants.DefaultClanBanner)
            };

            var updated = new List<LegacyClanInfo>(_clans.Count + 1);
            updated.AddRange(_clans.Where(static c => !string.IsNullOrWhiteSpace(c.Leader)));
            updated.Add(newClan);
            updated.Sort(static (a, b) => a.Id.CompareTo(b.Id));

            if (!PersistClans(updated))
            {
                error = "No se pudo escribir clanes.dat.";
                return false;
            }

            _clans = updated;
            clan = newClan;
            return true;
        }
    }

    public bool TrySetBanner(byte clanId, uint colorPrimary, uint colorSecondary, out LegacyClanInfo? clan, out string? error) =>
        TryUpdateClan(clanId,
            current => (CloneClan(current, banner: new LegacyClanBanner(colorPrimary, colorSecondary)), null),
            out clan,
            out error);

    public bool TrySetColor(byte clanId, byte color, out LegacyClanInfo? clan, out string? error) =>
        TryUpdateClan(clanId,
            current => (CloneClan(current, color: color), null),
            out clan,
            out error);

    public bool TryRename(byte clanId, string newName, out LegacyClanInfo? clan, out string? error)
    {
        var trimmed = (newName ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            clan = null;
            error = "El nombre del clan no puede estar vacío.";
            return false;
        }

        var normalized = NormalizeClanName(trimmed);
        if (string.IsNullOrEmpty(normalized))
        {
            clan = null;
            error = "El nombre del clan no contiene caracteres válidos.";
            return false;
        }

        return TryUpdateClan(
            clanId,
            current =>
            {
                if (string.Equals(current.Name, trimmed, StringComparison.Ordinal))
                {
                    return (current, null);
                }

                if (ContainsSimilarNameLocked(normalized, clanId))
                {
                    return (current, "Ya existe un clan con un nombre similar.");
                }

                return (CloneClan(current, name: trimmed), null);
            },
            out clan,
            out error);
    }

    public bool TryAdjustActiveMembers(byte clanId, int delta, out LegacyClanInfo? clan, out string? error)
    {
        return TryUpdateClan(
            clanId,
            current =>
            {
                var updatedValue = Math.Max(0, Math.Min(ushort.MaxValue, current.ActiveMembers + delta));
                return (CloneClan(current, activeMembers: (ushort)updatedValue), null);
            },
            out clan,
            out error);
    }

    public bool TryRemoveClan(byte clanId, out string? error) =>
        TryUpdateClan(clanId, static _ => (null, null), out _, out error);

    private void ReadFile()
    {
        using var stream = File.OpenRead(_backingFile);
        using var reader = new BinaryReader(stream, LegacyConstants.LegacyEncoding, leaveOpen: false);

        while (stream.Position < stream.Length)
        {
            if (!TryReadClan(reader, _clans))
            {
                break;
            }
        }
    }

    private bool TryUpdateClan(
        byte clanId,
        Func<LegacyClanInfo, (LegacyClanInfo? Updated, string? Error)> updater,
        out LegacyClanInfo? updated,
        out string? error)
    {
        lock (_sync)
        {
            var index = _clans.FindIndex(c => c.Id == clanId);
            if (index < 0)
            {
                updated = null;
                error = $"Clan #{clanId} no encontrado.";
                return false;
            }

            var current = _clans[index];
            var (replacement, failure) = updater(current);
            if (failure is not null)
            {
                updated = null;
                error = failure;
                return false;
            }

            var clone = new List<LegacyClanInfo>(_clans);
            if (replacement is null)
            {
                clone.RemoveAt(index);
            }
            else
            {
                clone[index] = replacement;
            }

            if (!PersistClans(clone))
            {
                updated = null;
                error = "No se pudo escribir clanes.dat.";
                return false;
            }

            _clans = clone;
            updated = replacement;
            error = null;
            return true;
        }
    }

    private bool ContainsSimilarNameLocked(string normalizedName, byte excludeClanId)
    {
        if (string.IsNullOrEmpty(normalizedName))
        {
            return false;
        }

        foreach (var existing in _clans)
        {
            if (existing.Id == excludeClanId)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(existing.Leader))
            {
                continue;
            }

            if (NormalizeClanName(existing.Name) == normalizedName)
            {
                return true;
            }
        }

        return false;
    }

    private static LegacyClanInfo CloneClan(
        LegacyClanInfo source,
        string? name = null,
        string? leader = null,
        byte? color = null,
        ushort? activeMembers = null,
        int? identifier = null,
        LegacyClanBanner? banner = null)
    {
        return new LegacyClanInfo
        {
            Id = source.Id,
            Name = name ?? source.Name,
            Leader = leader ?? source.Leader,
            Color = color ?? source.Color,
            ActiveMembers = activeMembers ?? source.ActiveMembers,
            ClanIdentifier = identifier ?? source.ClanIdentifier,
            Banner = banner ?? source.Banner
        };
    }

    private static string NormalizeClanName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            var normalized = NormalizeClanCharacter(ch);
            if (normalized.HasValue)
            {
                builder.Append(normalized.Value);
            }
        }

        return builder.ToString();
    }

    private static char? NormalizeClanCharacter(char ch)
    {
        var upper = char.ToUpperInvariant(ch);
        return upper switch
        {
            '8' or 'ß' or 'ẞ' => 'B',
            '1' => 'I',
            '0' => 'O',
            '5' or '$' => 'S',
            '2' => 'Z',
            '6' or '9' => 'G',
            'Ç' => 'C',
            'Á' or 'À' or 'Ä' or 'Â' or 'Å' or 'Ã' or 'Æ' => 'A',
            'É' or 'È' or 'Ë' or 'Ê' or '€' => 'E',
            'Í' or 'Ì' or 'Ï' or 'Î' => 'I',
            'Ó' or 'Ò' or 'Ö' or 'Ô' or 'Õ' or 'Ø' => 'O',
            'Ú' or 'Ù' or 'Ü' or 'Û' => 'U',
            'Ñ' => 'N',
            'Þ' => 'P',
            'Ý' or 'Ÿ' => 'Y',
            'Ð' => 'D',
            >= 'A' and <= 'Z' => upper,
            '3' or '4' or '7' => upper,
            _ => (char?)null
        };
    }

    private bool PersistClans(List<LegacyClanInfo> clans)
    {
        try
        {
            var directory = Path.GetDirectoryName(_backingFile);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using var stream = new FileStream(_backingFile, FileMode.Create, FileAccess.Write, FileShare.None);
            using var writer = new BinaryWriter(stream, LegacyConstants.LegacyEncoding, leaveOpen: false);

            foreach (var clan in clans)
            {
                if (string.IsNullOrWhiteSpace(clan.Leader))
                {
                    continue;
                }

                writer.Write(clan.Id);
                var record = BuildClanRecord(clan);
                writer.Write(record);
            }

            writer.Flush();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryReadClan(BinaryReader reader, List<LegacyClanInfo> clans)
    {
        try
        {
            var clanId = reader.ReadInt32();
            if (clanId < 0 || clanId > LegacyConstants.MaxClans)
            {
                return false;
            }

            var payload = reader.ReadBytes(LegacyConstants.ClanRecordSize);
            if (payload.Length != LegacyConstants.ClanRecordSize)
            {
                return false;
            }

            var info = ParseClanRecord((byte)clanId, payload);
            if (!string.IsNullOrWhiteSpace(info.Leader))
            {
                clans.Add(info);
            }

            return true;
        }
        catch (EndOfStreamException)
        {
            return false;
        }
    }

    private static LegacyClanInfo ParseClanRecord(byte id, byte[] payload)
    {
        var span = payload.AsSpan();
        var leader = ReadString(span.Slice(0, 17));
        var name = ReadString(span.Slice(17, 17));
        var color = span[34];
        var members = BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(35, 2));
        var identifier = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(37, 4));
        var color0 = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(41, 4));
        var color1 = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(45, 4));

        return new LegacyClanInfo
        {
            Id = id,
            Leader = leader,
            Name = name,
            Color = color,
            ActiveMembers = members,
            ClanIdentifier = identifier,
            Banner = new LegacyClanBanner(color0, color1)
        };
    }

    private static byte[] BuildClanRecord(LegacyClanInfo clan)
    {
        var buffer = new byte[LegacyConstants.ClanRecordSize];
        var span = buffer.AsSpan();
        WriteString(span.Slice(0, 17), clan.Leader);
        WriteString(span.Slice(17, 17), clan.Name);
        span[34] = clan.Color;
        BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(35, 2), clan.ActiveMembers);
        BinaryPrimitives.WriteInt32LittleEndian(span.Slice(37, 4), clan.ClanIdentifier);
        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(41, 4), clan.Banner.ColorPrimary);
        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(45, 4), clan.Banner.ColorSecondary);
        return buffer;
    }

    private static string ReadString(ReadOnlySpan<byte> data)
    {
        var length = data[0];
        if (length == 0)
        {
            return string.Empty;
        }

        var actualLength = Math.Clamp(length, (byte)0, (byte)(data.Length - 1));
        return LegacyConstants.LegacyEncoding.GetString(data.Slice(1, actualLength));
    }

    private static void WriteString(Span<byte> destination, string? value)
    {
        destination.Clear();
        if (destination.Length == 0)
        {
            return;
        }

        var safe = value ?? string.Empty;
        var encoded = LegacyConstants.LegacyEncoding.GetBytes(safe);
        var length = (byte)Math.Min(destination.Length - 1, encoded.Length);
        destination[0] = length;
        if (length == 0)
        {
            return;
        }

        encoded.AsSpan(0, length).CopyTo(destination.Slice(1));
    }

    private static byte? FindAvailableId(IReadOnlyList<LegacyClanInfo> clans)
    {
        var limit = LegacyConstants.MaxClans + 1;
        var used = new bool[limit];
        foreach (var clan in clans)
        {
            if (clan.Id < used.Length)
            {
                used[clan.Id] = true;
            }
        }

        for (byte i = 0; i < used.Length; i++)
        {
            if (!used[i])
            {
                return i;
            }
        }

        return null;
    }

    private static int GenerateClanIdentifier(string leaderName)
    {
        var signature = 0;
        for (var i = 0; i < leaderName.Length && i < 4; i++)
        {
            signature ^= leaderName[i];
        }

        var random = RandomNumberGenerator.GetInt32(int.MinValue, int.MaxValue);
        return ((signature & 0xFF) << 24) ^ random;
    }
}
