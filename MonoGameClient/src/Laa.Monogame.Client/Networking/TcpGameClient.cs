using Laa.Protocol;
using System.Net.Sockets;
using System.Text;

namespace Laa.Monogame.Client.Networking;

public sealed class TcpGameClient : IDisposable
{
    private readonly PacketReader _reader = new();
    private TcpClient? _client;
    private NetworkStream? _stream;
    private readonly byte[] _buffer = new byte[4096];

    public async Task<bool> ConnectAsync(string host, int port, CancellationToken cancellationToken)
    {
        try
        {
            _client = new TcpClient();
            await _client.ConnectAsync(host, port, cancellationToken);
            _stream = _client.GetStream();
            return true;
        }
        catch
        {
            Dispose();
            return false;
        }
    }

    public async Task SendAsync(MessageId id, ReadOnlyMemory<byte> payload, byte flags = 0, CancellationToken cancellationToken = default)
    {
        if (_stream is null) throw new InvalidOperationException("Not connected.");
        var packet = PacketWriter.Write(id, payload.Span, flags);
        await _stream.WriteAsync(packet, cancellationToken);
    }

    public async Task<IEnumerable<Packet>> ReceiveAsync(CancellationToken cancellationToken)
    {
        var packets = new List<Packet>();
        if (_stream is null)
        {
            return packets;
        }

        if (!_stream.DataAvailable)
        {
            return packets;
        }

        var read = await _stream.ReadAsync(_buffer.AsMemory(0, _buffer.Length), cancellationToken);
        if (read <= 0)
        {
            Dispose();
            return packets;
        }

        _reader.Append(_buffer.AsSpan(0, read));
        while (_reader.TryRead(out var packet))
        {
            packets.Add(packet);
        }

        return packets;
    }

    public static byte[] BuildLoginPayload(string username, string passwordHash)
    {
        using var ms = new MemoryStream();
        WriteString(ms, username);
        WriteString(ms, passwordHash);
        return ms.ToArray();
    }

    public static byte[] BuildCharacterCreatePayload(string username, CharacterCreationPayload payload)
    {
        using var ms = new MemoryStream();
        WriteString(ms, username);
        WriteString(ms, payload.Name);
        ms.WriteByte(payload.Race);
        ms.WriteByte(payload.Class);
        Span<byte> perkBuf = stackalloc byte[4];
        BitConverter.TryWriteBytes(perkBuf, payload.PerkMask);
        ms.Write(perkBuf);
        ms.WriteByte(payload.Strength);
        ms.WriteByte(payload.Constitution);
        ms.WriteByte(payload.Intelligence);
        ms.WriteByte(payload.Wisdom);
        ms.WriteByte(payload.Dexterity);
        return ms.ToArray();
    }

    public static byte[] BuildCharacterListPayload(string username)
    {
        using var ms = new MemoryStream();
        WriteString(ms, username);
        return ms.ToArray();
    }

    public void Dispose()
    {
        _stream?.Dispose();
        _client?.Dispose();
        _stream = null;
        _client = null;
    }

    private static void WriteString(Stream stream, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value ?? string.Empty);
        var len = (byte)Math.Min(byte.MaxValue, bytes.Length);
        stream.WriteByte(len);
        stream.Write(bytes, 0, len);
    }
}

public readonly record struct CharacterCreationPayload(
    string Name,
    byte Race,
    byte Class,
    uint PerkMask,
    byte Strength,
    byte Constitution,
    byte Intelligence,
    byte Wisdom,
    byte Dexterity);
