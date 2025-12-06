using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Laa.Monogame.Client.Networking;

public enum ServerCommandType
{
    SpritePosition,
    SpriteBatchPosition,
    SpriteAction,
    SpriteDirection,
    LocalPlayerPosition
}

public interface IServerCommand
{
    ServerCommandType Type { get; }
}

public sealed record SpritePositionCommand(ushort SpriteId, byte X, byte Y, byte Direction) : IServerCommand
{
    public ServerCommandType Type => ServerCommandType.SpritePosition;
}

public sealed record SpriteBatchPositionCommand(IReadOnlyList<SpriteBatchEntry> Entries) : IServerCommand
{
    public ServerCommandType Type => ServerCommandType.SpriteBatchPosition;
}

public readonly record struct SpriteBatchEntry(ushort SpriteId, byte X, byte Y, byte Direction);

public sealed record SpriteActionCommand(ushort SpriteId, byte Action) : IServerCommand
{
    public ServerCommandType Type => ServerCommandType.SpriteAction;
}

public sealed record SpriteDirectionCommand(ushort SpriteId, byte Direction) : IServerCommand
{
    public ServerCommandType Type => ServerCommandType.SpriteDirection;
}

public sealed record LocalPlayerPositionCommand(byte X, byte Y, byte Direction) : IServerCommand
{
    public ServerCommandType Type => ServerCommandType.LocalPlayerPosition;
}

public sealed class ServerCommandDecoder
{
    private readonly List<byte> _buffer = new();

    public void Enqueue(ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty)
        {
            return;
        }

        foreach (var b in data)
        {
            _buffer.Add(b);
        }
    }

    public IEnumerable<IServerCommand> Decode()
    {
        var commands = new List<IServerCommand>();
        var span = CollectionsMarshal.AsSpan(_buffer);
        var offset = 0;

        while (TryReadCommand(span.Slice(offset), out var command, out var consumed))
        {
            offset += consumed;
            commands.Add(command);
        }

        if (offset > 0 && offset <= _buffer.Count)
        {
            _buffer.RemoveRange(0, offset);
        }

        return commands;
    }

    private static bool TryReadCommand(ReadOnlySpan<byte> data, out IServerCommand command, out int consumed)
    {
        command = default!;
        consumed = 0;
        if (data.IsEmpty)
        {
            return false;
        }

        var opcode = data[0];
        switch (opcode)
        {
            case (byte)'P':
                return TryReadSpritePosition(data, out command, out consumed);
            case (byte)'p':
                return TryReadLocalPlayerPosition(data, out command, out consumed);
            case (byte)'r':
                return TryReadSpriteBatchPosition(data, out command, out consumed);
        }

        if (opcode >= 128 && opcode <= 135)
        {
            return TryReadSpriteDirection(data, opcode, out command, out consumed);
        }

        if (opcode >= 160 && opcode <= 175)
        {
            return TryReadSpriteAction(data, opcode, out command, out consumed);
        }

        // Unknown opcode; consume one byte to prevent stalling.
        consumed = 1;
        return false;
    }

    private static bool TryReadSpritePosition(ReadOnlySpan<byte> data, out IServerCommand command, out int consumed)
    {
        command = default!;
        consumed = 0;
        const int required = 6;
        if (data.Length < required)
        {
            return false;
        }

        var spriteId = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(1, 2));
        var x = data[3];
        var y = data[4];
        var direction = data[5];
        command = new SpritePositionCommand(spriteId, x, y, direction);
        consumed = required;
        return true;
    }

    private static bool TryReadLocalPlayerPosition(ReadOnlySpan<byte> data, out IServerCommand command, out int consumed)
    {
        command = default!;
        consumed = 0;
        const int required = 4;
        if (data.Length < required)
        {
            return false;
        }

        var x = data[1];
        var y = data[2];
        var direction = data[3];
        command = new LocalPlayerPositionCommand(x, y, direction);
        consumed = required;
        return true;
    }

    private static bool TryReadSpriteBatchPosition(ReadOnlySpan<byte> data, out IServerCommand command, out int consumed)
    {
        command = default!;
        consumed = 0;
        if (data.Length < 2)
        {
            return false;
        }

        var count = data[1];
        var required = 2 + count * 5;
        if (data.Length < required)
        {
            return false;
        }

        var entries = new SpriteBatchEntry[count];
        var offset = 2;
        for (var i = 0; i < count; i++)
        {
            var id = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(offset, 2));
            var x = data[offset + 2];
            var y = data[offset + 3];
            var direction = data[offset + 4];
            entries[i] = new SpriteBatchEntry(id, x, y, direction);
            offset += 5;
        }

        command = new SpriteBatchPositionCommand(entries);
        consumed = required;
        return true;
    }

    private static bool TryReadSpriteDirection(ReadOnlySpan<byte> data, byte opcode, out IServerCommand command, out int consumed)
    {
        command = default!;
        consumed = 0;
        const int required = 3;
        if (data.Length < required)
        {
            return false;
        }

        var spriteId = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(1, 2));
        var direction = (byte)(opcode - 128);
        command = new SpriteDirectionCommand(spriteId, direction);
        consumed = required;
        return true;
    }

    private static bool TryReadSpriteAction(ReadOnlySpan<byte> data, byte opcode, out IServerCommand command, out int consumed)
    {
        command = default!;
        consumed = 0;
        const int required = 3;
        if (data.Length < required)
        {
            return false;
        }

        var spriteId = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(1, 2));
        var action = (byte)(opcode - 160);
        command = new SpriteActionCommand(spriteId, action);
        consumed = required;
        return true;
    }
}
