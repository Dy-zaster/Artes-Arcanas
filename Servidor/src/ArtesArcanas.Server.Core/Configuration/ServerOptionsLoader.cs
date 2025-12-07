using System.Globalization;
using System.Text;

namespace ArtesArcanas.Server.Core.Configuration;

public static class ServerOptionsLoader
{
    public static ServerOptions Load(string optionsFile)
    {
        using var stream = File.OpenText(optionsFile);
        var builder = new OptionsBuilder();

        while (!stream.EndOfStream)
        {
            var rawLine = stream.ReadLine();
            if (string.IsNullOrWhiteSpace(rawLine))
            {
                continue;
            }

            var trimmed = rawLine.Trim();
            if (trimmed.StartsWith("//", StringComparison.Ordinal))
            {
                continue;
            }

            builder.ProcessLine(trimmed);
        }

        return builder.Build();
    }

    private sealed class OptionsBuilder
    {
        private readonly List<BasePosition> _positions = new();

        private string _welcome = string.Empty;
        private string _avatarServer = string.Empty;
        private string _ipAddress = string.Empty;
        private int _port = 15715;
        private int _highestMap = 1;
        private byte _spawnCooldown = 128;
        private bool _allowGlobalChat = true;
        private bool _allowMultipleSessions = true;
        private bool _keepLog = false;
        private bool _timestampLog = false;

        public void ProcessLine(string line)
        {
            var index = line.IndexOf('=');
            if (index <= 0)
            {
                return;
            }

            var key = NormalizeKey(line[..index]);
            var rawValue = line[(index + 1)..].Trim();

            switch (key)
            {
                case "posicion base":
                    ParseBasePosition(rawValue);
                    break;
                case "bienvenida":
                    _welcome = rawValue;
                    break;
                case "servidor de avatares":
                    _avatarServer = rawValue;
                    break;
                case "ip":
                    _ipAddress = rawValue;
                    break;
                case "puerto":
                    if (int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var newPort) &&
                        newPort is >= 21 and <= 32767)
                    {
                        _port = newPort;
                    }
                    break;
                case "numero de mapas":
                    if (int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var maps) &&
                        maps is >= 1 and <= 254)
                    {
                        _highestMap = maps;
                    }
                    break;
                case "turnos de reengendro":
                    if (int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var cooldown) &&
                        cooldown is >= 1 and <= 250)
                    {
                        _spawnCooldown = (byte)cooldown;
                    }
                    break;
                case "comunicacion total":
                    _allowGlobalChat = ParseBoolean(rawValue);
                    break;
                case "multiples sesiones":
                    _allowMultipleSessions = ParseBoolean(rawValue);
                    break;
                case "mantener registro":
                    _keepLog = ParseBoolean(rawValue);
                    break;
                case "fecha y hora en registro":
                    _timestampLog = ParseBoolean(rawValue);
                    break;
            }
        }

        private void ParseBasePosition(string value)
        {
            var segments = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (segments.Length < 4)
            {
                return;
            }

            if (!byte.TryParse(segments[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var race)) return;
            if (!byte.TryParse(segments[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var map)) return;
            if (!byte.TryParse(segments[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var x)) return;
            if (!byte.TryParse(segments[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out var y)) return;

            _positions.Add(new BasePosition((byte)(race % 9), map, x, y));
        }

        private static bool ParseBoolean(string value)
        {
            var normalized = NormalizeKey(value);
            return normalized is "si" or "sí" or "si.";
        }

        private static string NormalizeKey(string raw)
        {
            var formD = raw.Trim().Normalize(NormalizationForm.FormD);
            Span<char> buffer = stackalloc char[formD.Length];
            var length = 0;
            foreach (var ch in formD)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (category == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                buffer[length++] = char.ToLowerInvariant(ch);
            }

            return new string(buffer[..length]).Trim();
        }

        public ServerOptions Build() =>
            new()
            {
                WelcomeMessage = _welcome,
                AvatarServer = _avatarServer,
                IpAddress = _ipAddress,
                Port = _port,
                HighestMapId = _highestMap,
                SpawnCooldown = _spawnCooldown,
                AllowGlobalCommunication = _allowGlobalChat,
                AllowMultipleSessions = _allowMultipleSessions,
                KeepLogFile = _keepLog,
                TimestampLogEntries = _timestampLog,
                BasePositions = _positions.ToArray()
            };
    }
}
