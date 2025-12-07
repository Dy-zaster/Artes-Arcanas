using System;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Laa.Monogame.Client.Networking;

public sealed class LegacyLoginConfig
{
    public string AccountName { get; set; } = "Testin";
    public string Password { get; set; } = "password";
    public string AvatarName { get; set; } = "Testin";
    public byte ClassIndex { get; set; } = 0;
    public byte RaceIndex { get; set; } = 0;
    public byte Gender { get; set; } = 0;
    public byte Strength { get; set; } = 25;
    public byte Constitution { get; set; } = 25;
    public byte Dexterity { get; set; } = 25;
    public byte Wisdom { get; set; } = 25;
    public byte Intelligence { get; set; } = 25;
    public ushort Skills { get; set; } = 0;

    public string GetSanitizedAccountName() => SanitizeLogin(AccountName);
    public string GetSanitizedAvatarIdentifier() => SanitizeLogin(AvatarName);

    public static LegacyLoginConfig Load(string baseDirectory)
    {
        var path = Path.Combine(baseDirectory, "LegacyLoginConfig.json");
        if (!File.Exists(path))
        {
            var sample = new LegacyLoginConfig();
            File.WriteAllText(path, JsonSerializer.Serialize(sample, new JsonSerializerOptions { WriteIndented = true }));
            Console.WriteLine($"Legacy login config template created at {path}. Fill it with valid credentials.");
            return sample;
        }

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<LegacyLoginConfig>(json) ?? new LegacyLoginConfig();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to parse {path}: {ex.Message}");
            return new LegacyLoginConfig();
        }
    }

    private static string SanitizeLogin(string? value)
    {
        var input = value ?? string.Empty;
        var builder = new StringBuilder(capacity: 16);
        var skipSpaces = true;

        foreach (var ch in input)
        {
            if (skipSpaces && ch == ' ')
            {
                continue;
            }

            skipSpaces = false;
            if (!IsAllowedLoginChar(ch))
            {
                continue;
            }

            var mapped = MapLoginChar(ch);
            builder.Append(mapped);
            if (builder.Length == 16)
            {
                break;
            }
        }

        while (builder.Length < 5)
        {
            builder.Append('_');
        }

        return builder.ToString();
    }

    private static bool IsAllowedLoginChar(char ch)
    {
        return char.IsLetterOrDigit(ch)
               || ch == ' '
               || ch == '-'
               || ch == '_'
               || ch == '\''
               || ch >= 0x00A0;
    }

    private static char MapLoginChar(char ch)
    {
        if (ch >= 'a' && ch <= 'z')
        {
            return char.ToUpperInvariant(ch);
        }

        if ((ch >= 'A' && ch <= 'Z') || char.IsDigit(ch))
        {
            return ch;
        }

        return ch switch
        {
            ' ' => '_',
            '-' => '_',
            '_' => '_',
            '\'' => '_',
            'Á' or 'À' or 'Ä' or 'Â' or 'á' or 'à' or 'ä' or 'â' or 'Å' or 'å' or 'Ã' or 'ã' => 'A',
            'É' or 'È' or 'Ë' or 'Ê' or 'é' or 'è' or 'ë' or 'ê' => 'E',
            'Í' or 'Ì' or 'Ï' or 'Î' or 'í' or 'ì' or 'ï' or 'î' => 'I',
            'Ó' or 'Ò' or 'Ö' or 'Ô' or 'ó' or 'ò' or 'ö' or 'ô' or 'Õ' or 'õ' => 'O',
            'Ú' or 'Ù' or 'Ü' or 'Û' or 'ú' or 'ù' or 'ü' or 'û' => 'U',
            'Ý' or 'ý' or 'ÿ' or 'Ÿ' => 'Y',
            'ñ' or 'Ñ' => 'N',
            'ç' or 'Ç' => 'C',
            _ => '_'
        };
    }
}
