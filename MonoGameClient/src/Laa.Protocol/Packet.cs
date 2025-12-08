using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace Laa.Protocol;

public readonly record struct Packet(MessageId MessageId, byte Flags, ReadOnlyMemory<byte> Payload);

public readonly struct PacketHeader
{
    public const ushort Magic = 0xAA55;
    public const byte CurrentVersion = 1;
    public const int Size = 9; // magic(2) + version(1) + msg(1) + flags(1) + length(4)
    public const int MaxPayloadLength = 1_048_576; // 1 MiB

    public PacketHeader(byte version, MessageId messageId, byte flags, int payloadLength)
    {
        Version = version;
        MessageId = messageId;
        Flags = flags;
        PayloadLength = payloadLength;
    }

    public byte Version { get; }
    public MessageId MessageId { get; }
    public byte Flags { get; }
    public int PayloadLength { get; }

    public static bool TryRead(ReadOnlySpan<byte> buffer, out PacketHeader header)
    {
        header = default;
        if (buffer.Length < Size)
        {
            return false;
        }

        var magic = BinaryPrimitives.ReadUInt16LittleEndian(buffer);
        if (magic != Magic)
        {
            return false;
        }

        var version = buffer[2];
        var messageId = (MessageId)buffer[3];
        var flags = buffer[4];
        var length = BinaryPrimitives.ReadInt32LittleEndian(buffer.Slice(5, 4));

        if (length < 0 || length > MaxPayloadLength)
        {
            return false;
        }

        header = new PacketHeader(version, messageId, flags, length);
        return true;
    }

    public static void Write(Span<byte> buffer, byte version, MessageId messageId, byte flags, int payloadLength)
    {
        if (buffer.Length < Size) throw new ArgumentException("Buffer too small.", nameof(buffer));
        BinaryPrimitives.WriteUInt16LittleEndian(buffer, Magic);
        buffer[2] = version;
        buffer[3] = (byte)messageId;
        buffer[4] = flags;
        BinaryPrimitives.WriteInt32LittleEndian(buffer.Slice(5, 4), payloadLength);
    }
}

/// <summary>
/// Accumula bytes de socket y emite paquetes completos con framing AA55.
/// </summary>
public sealed class PacketReader
{
    private readonly List<byte> _buffer = new();

    public void Append(ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty)
        {
            return;
        }

        _buffer.AddRange(data.ToArray());
    }

    public bool TryRead(out Packet packet)
    {
        packet = default;
        while (true)
        {
            if (_buffer.Count < PacketHeader.Size)
            {
                return false;
            }

            var span = CollectionsMarshal.AsSpan(_buffer);
            if (!PacketHeader.TryRead(span, out var header))
            {
                // Desalineado: descartar un byte y reintentar.
                _buffer.RemoveAt(0);
                continue;
            }

            var totalLength = PacketHeader.Size + header.PayloadLength;
            if (_buffer.Count < totalLength)
            {
                return false;
            }

            var payload = new byte[header.PayloadLength];
            if (header.PayloadLength > 0)
            {
                span.Slice(PacketHeader.Size, header.PayloadLength).CopyTo(payload);
            }

            _buffer.RemoveRange(0, totalLength);
            packet = new Packet(header.MessageId, header.Flags, payload);
            return true;
        }
    }
}

/// <summary>
/// Serializa un paquete con header + payload en un solo buffer.
/// </summary>
public static class PacketWriter
{
    public static byte[] Write(MessageId messageId, ReadOnlySpan<byte> payload, byte flags = 0, byte version = PacketHeader.CurrentVersion)
    {
        var length = payload.Length;
        if (length > PacketHeader.MaxPayloadLength)
        {
            throw new ArgumentOutOfRangeException(nameof(payload), $"Payload too large ({length}>{PacketHeader.MaxPayloadLength}).");
        }

        var buffer = new byte[PacketHeader.Size + length];
        PacketHeader.Write(buffer, version, messageId, flags, length);
        if (length > 0)
        {
            payload.CopyTo(buffer.AsSpan(PacketHeader.Size));
        }

        return buffer;
    }
}
