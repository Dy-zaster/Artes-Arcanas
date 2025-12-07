using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Text;

namespace Laa.Monogame.Client.Networking;

internal static class OutboundCommandWriter
{
    public static ReadOnlyMemory<byte> BuildMoveCommand(byte direction)
    {
        return new byte[] { (byte)'m', (byte)(direction & 0x0F) };
    }

    public static ReadOnlyMemory<byte> BuildRunCommand(byte x, byte y)
    {
        return new byte[] { (byte)'M', x, y };
    }

    public static ReadOnlyMemory<byte> BuildAttackCommand(ushort tile)
    {
        Span<byte> buffer = stackalloc byte[3];
        buffer[0] = (byte)'A';
        BinaryPrimitives.WriteUInt16LittleEndian(buffer.Slice(1, 2), tile);
        return buffer.ToArray();
    }

    public static ReadOnlyMemory<byte> BuildAltAttackCommand(ushort tile)
    {
        Span<byte> buffer = stackalloc byte[3];
        buffer[0] = (byte)'B';
        BinaryPrimitives.WriteUInt16LittleEndian(buffer.Slice(1, 2), tile);
        return buffer.ToArray();
    }

    public static ReadOnlyMemory<byte> BuildSpellSelectionCommand(byte spellId)
    {
        return new byte[] { (byte)'j', spellId };
    }

    public static ReadOnlyMemory<byte> BuildSpellCastCommand(char opcode, ushort tile)
    {
        Span<byte> buffer = stackalloc byte[3];
        buffer[0] = (byte)opcode;
        BinaryPrimitives.WriteUInt16LittleEndian(buffer.Slice(1, 2), tile);
        return buffer.ToArray();
    }

    public static ReadOnlyMemory<byte> BuildChatCommand(char channel, string message)
    {
        var text = Encoding.UTF8.GetBytes(message ?? string.Empty);
        var length = Math.Min(text.Length, 255);
        var buffer = new byte[2 + length];
        buffer[0] = (byte)channel;
        buffer[1] = (byte)length;
        Array.Copy(text, 0, buffer, 2, length);
        return buffer;
    }

    public static IEnumerable<ReadOnlyMemory<byte>> BuildInventoryCommands(ushort[] tiles)
    {
        foreach (var tile in tiles)
        {
            var buffer = new byte[3];
            buffer[0] = (byte)'c';
            BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(1, 2), tile);
            yield return buffer;
        }
    }
}
