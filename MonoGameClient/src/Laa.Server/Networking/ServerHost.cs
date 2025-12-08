using System.Net;
using System.Net.Sockets;
using System.Text;
using Laa.Protocol;
using Laa.Server.Accounts;

namespace Laa.Server.Networking;

public sealed class ServerHost
{
    private static readonly IPAddress BindAddress = IPAddress.Loopback;
    private const int Port = 7667;
    private const int ReceiveBufferSize = 4096;
    private readonly AccountStore _accounts = new(Path.Combine(AppContext.BaseDirectory, "accounts"));

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var listener = new TcpListener(BindAddress, Port);
        listener.Start();
        Console.WriteLine($"[SERVER] Listening on {BindAddress}:{Port}");

        var clients = new List<Task>();
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                TcpClient client;
                try
                {
                    client = await listener.AcceptTcpClientAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                clients.Add(HandleClientAsync(client, cancellationToken));
            }
        }
        finally
        {
            listener.Stop();
            await Task.WhenAll(clients);
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        var endpoint = client.Client.RemoteEndPoint?.ToString() ?? "unknown";
        Console.WriteLine($"[SERVER] Client connected: {endpoint}");

        using var _ = client;
        using var stream = client.GetStream();
        var buffer = new byte[ReceiveBufferSize];
        var reader = new PacketReader();

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
                if (read == 0)
                {
                    break;
                }

                reader.Append(buffer.AsSpan(0, read));
                while (reader.TryRead(out var packet))
                {
                    await ProcessPacketAsync(stream, packet, cancellationToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Graceful shutdown.
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[SERVER] Error with {endpoint}: {ex.Message}");
        }
        finally
        {
            Console.WriteLine($"[SERVER] Client disconnected: {endpoint}");
        }
    }

    private async Task ProcessPacketAsync(NetworkStream stream, Packet packet, CancellationToken cancellationToken)
    {
        switch (packet.MessageId)
        {
            case MessageId.LoginRequest:
                await HandleLoginAsync(stream, packet.Payload, cancellationToken);
                break;
            case MessageId.CharacterListRequest:
                await HandleCharacterListAsync(stream, packet.Payload, cancellationToken);
                break;
            case MessageId.CharacterCreateRequest:
                await HandleCharacterCreateAsync(stream, packet.Payload, cancellationToken);
                break;
            case MessageId.EnterWorldRequest:
                await HandleEnterWorldAsync(stream, packet.Payload, cancellationToken);
                break;
            case MessageId.Ping:
                await HandlePingAsync(stream, packet.Payload, cancellationToken);
                break;
            default:
                Console.WriteLine($"[SERVER] Unhandled message: {packet.MessageId} ({packet.Payload.Length} bytes)");
                break;
        }
    }

    private async Task HandleLoginAsync(NetworkStream stream, ReadOnlyMemory<byte> payload, CancellationToken cancellationToken)
    {
        if (!TryParseLogin(payload.Span, out var username, out var passwordHash))
        {
            await SendErrorAsync(stream, 1, "Malformed login payload.", cancellationToken);
            return;
        }

        Console.WriteLine($"[SERVER] Login attempt user='{username}'");
        var account = await _accounts.LoadAsync(username, cancellationToken);
        if (account is null)
        {
            await SendErrorAsync(stream, 2, "Cuenta inexistente.", cancellationToken);
            return;
        }

        if (!string.Equals(account.PasswordHash, passwordHash, StringComparison.Ordinal))
        {
            await SendErrorAsync(stream, 3, "Clave incorrecta.", cancellationToken);
            return;
        }

        var token = "dev-token";
        var tokenBytes = Encoding.UTF8.GetBytes(token);
        var response = new byte[2 + tokenBytes.Length];
        response[0] = 0; // result OK
        response[1] = (byte)tokenBytes.Length;
        Array.Copy(tokenBytes, 0, response, 2, tokenBytes.Length);

        var packet = PacketWriter.Write(MessageId.LoginResponse, response);
        await stream.WriteAsync(packet, cancellationToken);
    }

    private async Task HandleCharacterListAsync(NetworkStream stream, ReadOnlyMemory<byte> payload, CancellationToken cancellationToken)
    {
        if (!TryParseUsername(payload.Span, out var username))
        {
            await SendErrorAsync(stream, 1, "Malformed list payload.", cancellationToken);
            return;
        }

        var account = await _accounts.LoadAsync(username, cancellationToken);
        if (account is null)
        {
            await SendErrorAsync(stream, 2, "Cuenta inexistente.", cancellationToken);
            return;
        }

        var buffer = BuildCharacterListPayload(account.Characters);
        var packet = PacketWriter.Write(MessageId.CharacterListResponse, buffer);
        await stream.WriteAsync(packet, cancellationToken);
    }

    private async Task HandleCharacterCreateAsync(NetworkStream stream, ReadOnlyMemory<byte> payload, CancellationToken cancellationToken)
    {
        if (!TryParseCharacterCreate(payload.Span, out var username, out var record))
        {
            await SendErrorAsync(stream, 1, "Payload de creación inválido.", cancellationToken);
            return;
        }

        var account = await _accounts.LoadAsync(username, cancellationToken);
        if (account is null)
        {
            await SendErrorAsync(stream, 2, "Cuenta inexistente.", cancellationToken);
            return;
        }

        if (account.Characters.Count >= 5)
        {
            await SendErrorAsync(stream, 3, "Límite de personajes alcanzado.", cancellationToken);
            return;
        }

        if (account.Characters.Any(c => string.Equals(c.Name, record.Name, StringComparison.OrdinalIgnoreCase)))
        {
            await SendErrorAsync(stream, 4, "Nombre ya usado en esta cuenta.", cancellationToken);
            return;
        }

        var enriched = ApplyCharacterDefaults(record);
        account.Characters.Add(enriched);
        await _accounts.SaveAsync(account, cancellationToken);
        var packet = PacketWriter.Write(MessageId.CharacterCreateResponse, new byte[] { 0 });
        await stream.WriteAsync(packet, cancellationToken);
    }

    private async Task HandleEnterWorldAsync(NetworkStream stream, ReadOnlyMemory<byte> payload, CancellationToken cancellationToken)
    {
        var name = Encoding.UTF8.GetString(payload.Span);
        Console.WriteLine($"[SERVER] EnterWorld requested for '{name}'.");
        var packet = PacketWriter.Write(MessageId.EnterWorldResponse, Array.Empty<byte>());
        await stream.WriteAsync(packet, cancellationToken);
    }

    private static async Task HandlePingAsync(NetworkStream stream, ReadOnlyMemory<byte> payload, CancellationToken cancellationToken)
    {
        var packet = PacketWriter.Write(MessageId.Pong, payload.Span);
        await stream.WriteAsync(packet, cancellationToken);
    }

    private static async Task SendErrorAsync(NetworkStream stream, byte code, string message, CancellationToken cancellationToken)
    {
        var messageBytes = Encoding.UTF8.GetBytes(message ?? string.Empty);
        var length = 1 + Math.Min(ushort.MaxValue, messageBytes.Length);
        var payload = new byte[length];
        payload[0] = code;
        var copyLen = Math.Min(messageBytes.Length, length - 1);
        Array.Copy(messageBytes, 0, payload, 1, copyLen);
        var packet = PacketWriter.Write(MessageId.ErrorResponse, payload);
        await stream.WriteAsync(packet, cancellationToken);
    }

    private static bool TryParseLogin(ReadOnlySpan<byte> payload, out string username, out string passwordHash)
    {
        username = string.Empty;
        passwordHash = string.Empty;
        if (payload.Length < 2)
        {
            return false;
        }

        var userLen = payload[0];
        if (payload.Length < 1 + userLen + 1)
        {
            return false;
        }

        var passLen = payload[1 + userLen];
        if (payload.Length < 1 + userLen + 1 + passLen)
        {
            return false;
        }

        username = Encoding.UTF8.GetString(payload.Slice(1, userLen));
        passwordHash = Encoding.UTF8.GetString(payload.Slice(1 + userLen + 1, passLen));
        return true;
    }

    private static bool TryParseCharacterCreate(ReadOnlySpan<byte> payload, out string username, out CharacterRecord record)
    {
        record = default;
        username = string.Empty;
        var reader = new SpanReader(payload);
        if (!reader.TryReadString(out username))
        {
            return false;
        }

        if (!reader.TryReadString(out var name))
        {
            return false;
        }

        if (!reader.TryReadByte(out var race) || !reader.TryReadByte(out var @class))
        {
            return false;
        }

        if (!reader.TryReadUInt(out var perkMask))
        {
            return false;
        }

        if (!reader.TryReadByte(out var str) || !reader.TryReadByte(out var con) || !reader.TryReadByte(out var intel) ||
            !reader.TryReadByte(out var wis) || !reader.TryReadByte(out var dex))
        {
            return false;
        }

        record = new CharacterRecord(
            name,
            race,
            @class,
            perkMask,
            str,
            con,
            intel,
            wis,
            dex,
            Evasion: 0,
            Level: 1,
            Experience: 0,
            MapId: 0,
            PosX: 0,
            PosY: 0,
            MaxMana: 30,
            Mana: 30,
            MaxHealth: 50,
            Health: 50,
            Gold: 0,
            Silver: 0,
            FoodPercent: 100,
            Honor: 1,
            Armor: 0,
            MagicResist: 0,
            Inventory: new List<CharacterItem>(),
            Spells: new List<CharacterSpell>());
        return true;
    }

    private static byte[] BuildCharacterListPayload(List<CharacterRecord> characters)
    {
        using var ms = new MemoryStream();
        ms.WriteByte((byte)Math.Clamp(characters.Count, 0, 5));
        foreach (var c in characters.Take(5))
        {
            WriteString(ms, c.Name);
            ms.WriteByte(c.Race);
            ms.WriteByte(c.Class);
            Span<byte> perkBuf = stackalloc byte[4];
            BitConverter.TryWriteBytes(perkBuf, c.PerkMask);
            ms.Write(perkBuf);
            ms.WriteByte(c.StatStrength);
            ms.WriteByte(c.StatConstitution);
            ms.WriteByte(c.StatIntelligence);
            ms.WriteByte(c.StatWisdom);
            ms.WriteByte(c.StatDexterity);
            ms.WriteByte(c.Evasion);
            Span<byte> tmp = stackalloc byte[4];
            BitConverter.TryWriteBytes(tmp, c.Level);
            ms.Write(tmp[..2]); // level ushort
            BitConverter.TryWriteBytes(tmp, c.Experience);
            ms.Write(tmp);
            ms.WriteByte(c.MapId);
            ms.WriteByte(c.PosX);
            ms.WriteByte(c.PosY);
            BitConverter.TryWriteBytes(tmp, c.MaxMana);
            ms.Write(tmp[..2]);
            BitConverter.TryWriteBytes(tmp, c.Mana);
            ms.Write(tmp[..2]);
            BitConverter.TryWriteBytes(tmp, c.MaxHealth);
            ms.Write(tmp[..2]);
            BitConverter.TryWriteBytes(tmp, c.Health);
            ms.Write(tmp[..2]);
            BitConverter.TryWriteBytes(tmp, c.Gold);
            ms.Write(tmp);
            BitConverter.TryWriteBytes(tmp, c.Silver);
            ms.Write(tmp);
            ms.WriteByte(c.FoodPercent);
            ms.WriteByte(c.Honor);
            BitConverter.TryWriteBytes(tmp, c.Armor);
            ms.Write(tmp[..2]);
            BitConverter.TryWriteBytes(tmp, c.MagicResist);
            ms.Write(tmp[..2]);
            var invCount = (byte)Math.Min(255, c.Inventory.Count);
            ms.WriteByte(invCount);
            for (var i = 0; i < invCount; i++)
            {
                var item = c.Inventory[i];
                BitConverter.TryWriteBytes(tmp, item.ItemId);
                ms.Write(tmp[..4]);
                BitConverter.TryWriteBytes(tmp, item.Amount);
                ms.Write(tmp[..2]);
                ms.WriteByte((byte)item.EquippedSlot);
            }
            var spellCount = (byte)Math.Min(255, c.Spells.Count);
            ms.WriteByte(spellCount);
            for (var s = 0; s < spellCount; s++)
            {
                var spell = c.Spells[s];
                BitConverter.TryWriteBytes(tmp, spell.SpellId);
                ms.Write(tmp[..2]);
            }
        }
        return ms.ToArray();
    }

    private static bool TryParseUsername(ReadOnlySpan<byte> payload, out string username)
    {
        username = string.Empty;
        var reader = new SpanReader(payload);
        if (!reader.TryReadString(out username))
        {
            return false;
        }

        return !string.IsNullOrWhiteSpace(username);
    }

    private static CharacterRecord ApplyCharacterDefaults(CharacterRecord record)
    {
        var spawn = ResolveSpawn(record.Race);
        return record with
        {
            Level = record.Level == 0 ? (ushort)1 : record.Level,
            Experience = record.Experience,
            MapId = spawn.MapId,
            PosX = spawn.X,
            PosY = spawn.Y,
            MaxMana = record.MaxMana == 0 ? (ushort)30 : record.MaxMana,
            Mana = record.Mana == 0 ? (ushort)30 : record.Mana,
            MaxHealth = record.MaxHealth == 0 ? (ushort)50 : record.MaxHealth,
            Health = record.Health == 0 ? (ushort)50 : record.Health,
            FoodPercent = record.FoodPercent == 0 ? (byte)100 : record.FoodPercent,
            Honor = record.Honor == 0 ? (byte)1 : record.Honor,
            Armor = record.Armor,
            MagicResist = record.MagicResist,
            Inventory = record.Inventory ?? new List<CharacterItem>()
        };
    }

    private static (byte MapId, byte X, byte Y) ResolveSpawn(byte race)
    {
        return race switch
        {
            0 => (1, 220, 62),   // Humano
            1 => (4, 138, 232),  // Elfo
            2 => (6, 46, 53),    // Enano
            3 => (1, 206, 51),   // Gnomo
            4 => (4, 138, 232),  // Semielfo
            5 => (8, 33, 107),   // Orco
            6 => (12, 138, 104), // Drow
            _ => (0, 128, 128)
        };
    }

    private static void WriteString(Stream stream, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value ?? string.Empty);
        var len = (byte)Math.Min(byte.MaxValue, bytes.Length);
        stream.WriteByte(len);
        stream.Write(bytes, 0, len);
    }
}
