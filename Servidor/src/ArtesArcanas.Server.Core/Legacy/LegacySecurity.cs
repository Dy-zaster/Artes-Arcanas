using System.Security.Cryptography;
using System.Text;

namespace ArtesArcanas.Server.Core.Legacy;

public static class LegacySecurity
{
    private const string PasswordPrefix = "Artes Arcanas:";

    public static byte[] HashPassword(string password, string identifier)
    {
        var payload = $"{PasswordPrefix}{identifier}{password}";
        return SHA256.HashData(Encoding.UTF8.GetBytes(payload));
    }

    public static uint ComputeHandshake(uint seed)
    {
        const uint Token = 0x542C3A9E;
        static uint XRandom(uint value)
        {
            value ^= value << 13;
            value ^= value >> 17;
            value ^= value << 5;
            return value;
        }

        return XRandom(seed + XRandom(Token ^ seed));
    }

    public static bool ValidateHandshake(uint expected, uint received) =>
        expected == received;

    public static bool SeemsIp(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var parts = value.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 4)
        {
            return false;
        }

        foreach (var part in parts)
        {
            if (!int.TryParse(part, out var octet) || octet < 0 || octet > 255)
            {
                return false;
            }
        }

        return true;
    }

    public static string SanitizeLogin(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return "_____";
        }

        var builder = new StringBuilder(capacity: 16);
        var skipSpaces = true;
        foreach (var ch in raw)
        {
            if (skipSpaces && ch == ' ')
            {
                continue;
            }

            skipSpaces = false;
            var normalized = NormalizeLoginChar(ch);
            builder.Append(normalized);
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

    private static char NormalizeLoginChar(char ch) =>
        ch switch
        {
            // Letras
            >= 'a' and <= 'z' => char.ToUpperInvariant(ch),
            >= 'A' and <= 'Z' => ch,

            // Números con reemplazos especiales
            '1' => 'I',
            '0' => 'O',
            '5' or '$' => 'S',
            '2' => 'Z',
            '6' or '9' => 'G',

            // Números no especiales
            >= '0' and <= '9' => ch,

            // Acentos y demás caracteres especiales
            'Á' or 'À' or 'Ä' or 'Â' or 'á' or 'à' or 'ä' or 'â' or 'Å' or 'å' or 'Ã' or 'ã' or 'Æ' or 'æ' => 'A',
            'É' or 'È' or 'Ë' or 'Ê' or 'é' or 'è' or 'ë' or 'ê' or '€' => 'E',
            'Í' or 'Ì' or 'Ï' or 'Î' or 'í' or 'ì' or 'ï' or 'î' => 'I',
            'Ó' or 'Ò' or 'Ö' or 'Ô' or 'ó' or 'ò' or 'ö' or 'ô' or 'Õ' or 'õ' or 'Ø' or 'ø' => 'O',
            'Ú' or 'Ù' or 'Ü' or 'Û' or 'ú' or 'ù' or 'ü' or 'û' => 'U',
            'ñ' or 'Ñ' => 'N',
            'Ç' or 'ç' => 'C',
            'Ý' or 'ý' or 'ÿ' or 'Ÿ' => 'Y',
            'Ð' or 'ð' => 'D',
            'Þ' or 'þ' => 'P',
            'ß' => 'B',

            _ => '_'
        };

}
