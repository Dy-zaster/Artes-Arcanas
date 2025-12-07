using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Laa.Monogame.Client.Networking;

public enum ServerCommandType
{
    SpritePosition,
    SpriteBatchPosition,
    SpriteAction,
    SpriteDirection,
    LocalPlayerPosition,
    PlayerHealth,
    PlayerMana,
    PlayerFood,
    PlayerMoney,
    PlayerExperience,
    PlayerDamageFromMonster,
    PlayerDamageFromObject,
    PlayerDamageFromSpell,
    PlayerAttackReport,
    SpriteEffect,
    TileEffect,
    PlayerSpellCastReport,
    PlayerStatusEffect,
    SpriteChat,
    SpriteNotification,
    ServerInfoMessage
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

public sealed record PlayerHealthCommand(ushort Value) : IServerCommand
{
    public ServerCommandType Type => ServerCommandType.PlayerHealth;
}

public sealed record PlayerManaCommand(byte Value) : IServerCommand
{
    public ServerCommandType Type => ServerCommandType.PlayerMana;
}

public sealed record PlayerFoodCommand(byte Value) : IServerCommand
{
    public ServerCommandType Type => ServerCommandType.PlayerFood;
}

public sealed record PlayerMoneyCommand(uint Amount) : IServerCommand
{
    public ServerCommandType Type => ServerCommandType.PlayerMoney;
}

public sealed record PlayerExperienceCommand(ushort Value) : IServerCommand
{
    public ServerCommandType Type => ServerCommandType.PlayerExperience;
}

public sealed record PlayerDamageFromMonsterCommand(ushort NewHealth, byte AttackIndex, byte MonsterIndex) : IServerCommand
{
    public ServerCommandType Type => ServerCommandType.PlayerDamageFromMonster;
}

public sealed record PlayerDamageFromObjectCommand(ushort NewHealth, byte ObjectId, ushort AttackerId) : IServerCommand
{
    public ServerCommandType Type => ServerCommandType.PlayerDamageFromObject;
}

public sealed record PlayerDamageFromSpellCommand(ushort NewHealth, byte SpellId, ushort AttackerId) : IServerCommand
{
    public ServerCommandType Type => ServerCommandType.PlayerDamageFromSpell;
}

public sealed record PlayerAttackReportCommand(ushort Damage, ushort RemainingHealth, ushort? TargetSpriteId) : IServerCommand
{
    public ServerCommandType Type => ServerCommandType.PlayerAttackReport;
}

public sealed record PlayerSpellCastReportCommand(byte SpellId, ushort CasterSpriteId) : IServerCommand
{
    public ServerCommandType Type => ServerCommandType.PlayerSpellCastReport;
}

public sealed record SpriteEffectCommand(ushort SpriteId, byte EffectCode) : IServerCommand
{
    public ServerCommandType Type => ServerCommandType.SpriteEffect;
}

public sealed record TileEffectCommand(byte X, byte Y, byte EffectCode) : IServerCommand
{
    public ServerCommandType Type => ServerCommandType.TileEffect;
}

public sealed record PlayerStatusEffectCommand(byte Code) : IServerCommand
{
    public ServerCommandType Type => ServerCommandType.PlayerStatusEffect;
}

public sealed record SpriteChatCommand(ushort SpriteId, string Message) : IServerCommand
{
    public ServerCommandType Type => ServerCommandType.SpriteChat;
}

public sealed record SpriteNotificationCommand(ushort SpriteId, byte Code) : IServerCommand
{
    public ServerCommandType Type => ServerCommandType.SpriteNotification;
}

public sealed record ServerInfoMessageCommand(byte Code) : IServerCommand
{
    public ServerCommandType Type => ServerCommandType.ServerInfoMessage;
}

public sealed class ServerCommandDecoder
{
    private readonly List<byte> _buffer = new();
    private readonly List<string> _errors = new();
    private static readonly Encoding Latin1Encoding = Encoding.Latin1;

    private enum CommandParseStatus
    {
        NeedMoreData,
        Success,
        Skipped,
        Error
    }

    public IReadOnlyList<string> FlushErrors()
    {
        if (_errors.Count == 0)
        {
            return Array.Empty<string>();
        }

        var copy = _errors.ToArray();
        _errors.Clear();
        return copy;
    }

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

        while (offset < span.Length)
        {
            var status = TryReadCommand(span.Slice(offset), out var command, out var consumed, out var error);
            if (status == CommandParseStatus.NeedMoreData)
            {
                break;
            }

            if (consumed <= 0)
            {
                // Prevent infinite loops on bad packets.
                consumed = 1;
            }

            offset += consumed;

            switch (status)
            {
                case CommandParseStatus.Success when command is not null:
                    commands.Add(command);
                    break;
                case CommandParseStatus.Error when !string.IsNullOrWhiteSpace(error):
                    _errors.Add(error);
                    break;
            }
        }

        if (offset > 0 && offset <= _buffer.Count)
        {
            _buffer.RemoveRange(0, offset);
        }

        return commands;
    }

    private static CommandParseStatus TryReadCommand(
        ReadOnlySpan<byte> data,
        out IServerCommand? command,
        out int consumed,
        out string? error)
    {
        command = default;
        consumed = 0;
        error = null;
        if (data.IsEmpty)
        {
            return CommandParseStatus.NeedMoreData;
        }

        var opcode = data[0];
        switch (opcode)
        {
            case (byte)'P':
                return TryReadSpritePosition(data, out command, out consumed);
            case (byte)'p':
                return TryReadLocalPlayerPosition(data, out command, out consumed);
            case (byte)'r':
                return TryReadSpriteBatchPosition(data, out command, out consumed, out error);
            case (byte)'e':
                return TryReadPlayerExperience(data, out command, out consumed);
            case (byte)'d':
                return TryReadPlayerAttackReport(data, hasTarget: false, out command, out consumed);
            case (byte)'D':
                return TryReadPlayerAttackReport(data, hasTarget: true, out command, out consumed);
            case (byte)'C':
                return TryReadPlayerSpellReport(data, out command, out consumed);
            case (byte)'=':
                return TryReadSpriteEffect(data, out command, out consumed);
            case (byte)'S':
                return TryReadTileEffect(data, out command, out consumed);
            case (byte)'s':
                return TryReadPlayerStatusEffect(data, out command, out consumed);
            case (byte)'h':
                return TryReadSpriteChat(data, out command, out consumed, out error);
            case (byte)'H':
                return TryReadSpriteNotification(data, out command, out consumed);
            case (byte)'i':
                return TryReadServerInfoMessage(data, out command, out consumed);
            case 0xFF:
                return TryReadPlayerHealth(data, out command, out consumed);
            case 0xFE:
                return TryReadPlayerMana(data, out command, out consumed);
            case 0xFD:
                return TryReadPlayerFood(data, out command, out consumed);
            case 0xFA:
                return TryReadPlayerMoney(data, out command, out consumed);
            case 0xFC:
                return TryReadPlayerDamageFromMonster(data, out command, out consumed);
            case 0xFB:
                return TryReadPlayerDamageFromObject(data, out command, out consumed);
            case 0xF9:
                return TryReadPlayerDamageFromSpell(data, out command, out consumed);
        }

        if (opcode >= 128 && opcode <= 135)
        {
            return TryReadSpriteDirection(data, opcode, out command, out consumed);
        }

        if (opcode >= 160 && opcode <= 175)
        {
            return TryReadSpriteAction(data, opcode, out command, out consumed);
        }

        consumed = 1;
        error = $"Opcode desconocido 0x{opcode:X2}.";
        return CommandParseStatus.Error;
    }

    private static CommandParseStatus TryReadSpritePosition(ReadOnlySpan<byte> data, out IServerCommand? command, out int consumed)
    {
        command = default;
        consumed = 0;
        const int required = 6;
        if (data.Length < required)
        {
            return CommandParseStatus.NeedMoreData;
        }

        var spriteId = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(1, 2));
        var x = data[3];
        var y = data[4];
        var direction = data[5];
        command = new SpritePositionCommand(spriteId, x, y, direction);
        consumed = required;
        return CommandParseStatus.Success;
    }

    private static CommandParseStatus TryReadLocalPlayerPosition(ReadOnlySpan<byte> data, out IServerCommand? command, out int consumed)
    {
        command = default;
        consumed = 0;
        const int required = 4;
        if (data.Length < required)
        {
            return CommandParseStatus.NeedMoreData;
        }

        var x = data[1];
        var y = data[2];
        var direction = data[3];
        command = new LocalPlayerPositionCommand(x, y, direction);
        consumed = required;
        return CommandParseStatus.Success;
    }

    private static CommandParseStatus TryReadSpriteBatchPosition(
        ReadOnlySpan<byte> data,
        out IServerCommand? command,
        out int consumed,
        out string? error)
    {
        command = default;
        consumed = 0;
        error = null;
        if (data.Length < 2)
        {
            return CommandParseStatus.NeedMoreData;
        }

        var count = data[1];
        if (count == 0)
        {
            consumed = 2;
            error = "Comando 'r' sin entradas.";
            return CommandParseStatus.Error;
        }

        const int maxEntries = 32;
        if (count > maxEntries)
        {
            consumed = 2;
            error = $"Comando 'r' excede el máximo ({count}>{maxEntries}).";
            return CommandParseStatus.Error;
        }

        var required = 2 + count * 5;
        if (data.Length < required)
        {
            return CommandParseStatus.NeedMoreData;
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
        return CommandParseStatus.Success;
    }

    private static CommandParseStatus TryReadSpriteDirection(ReadOnlySpan<byte> data, byte opcode, out IServerCommand? command, out int consumed)
    {
        command = default;
        consumed = 0;
        const int required = 3;
        if (data.Length < required)
        {
            return CommandParseStatus.NeedMoreData;
        }

        var spriteId = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(1, 2));
        var direction = (byte)(opcode - 128);
        command = new SpriteDirectionCommand(spriteId, direction);
        consumed = required;
        return CommandParseStatus.Success;
    }

    private static CommandParseStatus TryReadSpriteAction(ReadOnlySpan<byte> data, byte opcode, out IServerCommand? command, out int consumed)
    {
        command = default;
        consumed = 0;
        const int required = 3;
        if (data.Length < required)
        {
            return CommandParseStatus.NeedMoreData;
        }

        var spriteId = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(1, 2));
        var action = (byte)(opcode - 160);
        command = new SpriteActionCommand(spriteId, action);
        consumed = required;
        return CommandParseStatus.Success;
    }

    private static CommandParseStatus TryReadPlayerHealth(ReadOnlySpan<byte> data, out IServerCommand? command, out int consumed)
    {
        command = default;
        consumed = 0;
        const int required = 3;
        if (data.Length < required)
        {
            return CommandParseStatus.NeedMoreData;
        }

        var value = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(1, 2));
        command = new PlayerHealthCommand(value);
        consumed = required;
        return CommandParseStatus.Success;
    }

    private static CommandParseStatus TryReadPlayerMana(ReadOnlySpan<byte> data, out IServerCommand? command, out int consumed)
    {
        command = default;
        consumed = 0;
        const int required = 2;
        if (data.Length < required)
        {
            return CommandParseStatus.NeedMoreData;
        }

        command = new PlayerManaCommand(data[1]);
        consumed = required;
        return CommandParseStatus.Success;
    }

    private static CommandParseStatus TryReadPlayerFood(ReadOnlySpan<byte> data, out IServerCommand? command, out int consumed)
    {
        command = default;
        consumed = 0;
        const int required = 2;
        if (data.Length < required)
        {
            return CommandParseStatus.NeedMoreData;
        }

        command = new PlayerFoodCommand(data[1]);
        consumed = required;
        return CommandParseStatus.Success;
    }

    private static CommandParseStatus TryReadPlayerMoney(ReadOnlySpan<byte> data, out IServerCommand? command, out int consumed)
    {
        command = default;
        consumed = 0;
        const int required = 5;
        if (data.Length < required)
        {
            return CommandParseStatus.NeedMoreData;
        }

        var gold = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(1, 4));
        command = new PlayerMoneyCommand(gold);
        consumed = required;
        return CommandParseStatus.Success;
    }

    private static CommandParseStatus TryReadPlayerExperience(ReadOnlySpan<byte> data, out IServerCommand? command, out int consumed)
    {
        command = default;
        consumed = 0;
        const int required = 3;
        if (data.Length < required)
        {
            return CommandParseStatus.NeedMoreData;
        }

        var xp = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(1, 2));
        command = new PlayerExperienceCommand(xp);
        consumed = required;
        return CommandParseStatus.Success;
    }

    private static CommandParseStatus TryReadPlayerDamageFromMonster(ReadOnlySpan<byte> data, out IServerCommand? command, out int consumed)
    {
        command = default;
        consumed = 0;
        const int required = 5;
        if (data.Length < required)
        {
            return CommandParseStatus.NeedMoreData;
        }

        var hp = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(1, 2));
        var attackIndex = data[3];
        var monsterIndex = data[4];
        command = new PlayerDamageFromMonsterCommand(hp, attackIndex, monsterIndex);
        consumed = required;
        return CommandParseStatus.Success;
    }

    private static CommandParseStatus TryReadPlayerDamageFromObject(ReadOnlySpan<byte> data, out IServerCommand? command, out int consumed)
    {
        command = default;
        consumed = 0;
        const int required = 6;
        if (data.Length < required)
        {
            return CommandParseStatus.NeedMoreData;
        }

        var hp = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(1, 2));
        var objectId = data[3];
        var attackerId = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(4, 2));
        command = new PlayerDamageFromObjectCommand(hp, objectId, attackerId);
        consumed = required;
        return CommandParseStatus.Success;
    }

    private static CommandParseStatus TryReadPlayerDamageFromSpell(ReadOnlySpan<byte> data, out IServerCommand? command, out int consumed)
    {
        command = default;
        consumed = 0;
        const int required = 6;
        if (data.Length < required)
        {
            return CommandParseStatus.NeedMoreData;
        }

        var hp = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(1, 2));
        var spellId = data[3];
        var attackerId = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(4, 2));
        command = new PlayerDamageFromSpellCommand(hp, spellId, attackerId);
        consumed = required;
        return CommandParseStatus.Success;
    }

    private static CommandParseStatus TryReadPlayerAttackReport(
        ReadOnlySpan<byte> data,
        bool hasTarget,
        out IServerCommand? command,
        out int consumed)
    {
        command = default;
        consumed = 0;
        var required = hasTarget ? 7 : 5;
        if (data.Length < required)
        {
            return CommandParseStatus.NeedMoreData;
        }

        var damage = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(1, 2));
        var remaining = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(3, 2));
        ushort? target = null;
        if (hasTarget)
        {
            target = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(5, 2));
        }

        command = new PlayerAttackReportCommand(damage, remaining, target);
        consumed = required;
        return CommandParseStatus.Success;
    }

    private static CommandParseStatus TryReadPlayerSpellReport(
        ReadOnlySpan<byte> data,
        out IServerCommand? command,
        out int consumed)
    {
        command = default;
        consumed = 0;
        const int required = 4;
        if (data.Length < required)
        {
            return CommandParseStatus.NeedMoreData;
        }

        var spellId = data[1];
        var caster = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(2, 2));
        command = new PlayerSpellCastReportCommand(spellId, caster);
        consumed = required;
        return CommandParseStatus.Success;
    }

    private static CommandParseStatus TryReadSpriteEffect(
        ReadOnlySpan<byte> data,
        out IServerCommand? command,
        out int consumed)
    {
        command = default;
        consumed = 0;
        const int required = 4;
        if (data.Length < required)
        {
            return CommandParseStatus.NeedMoreData;
        }

        var spriteId = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(1, 2));
        var effect = data[3];
        command = new SpriteEffectCommand(spriteId, effect);
        consumed = required;
        return CommandParseStatus.Success;
    }

    private static CommandParseStatus TryReadTileEffect(
        ReadOnlySpan<byte> data,
        out IServerCommand? command,
        out int consumed)
    {
        command = default;
        consumed = 0;
        const int required = 4;
        if (data.Length < required)
        {
            return CommandParseStatus.NeedMoreData;
        }

        var x = data[1];
        var y = data[2];
        var effect = data[3];
        command = new TileEffectCommand(x, y, effect);
        consumed = required;
        return CommandParseStatus.Success;
    }

    private static CommandParseStatus TryReadPlayerStatusEffect(
        ReadOnlySpan<byte> data,
        out IServerCommand? command,
        out int consumed)
    {
        command = default;
        consumed = 0;
        const int required = 2;
        if (data.Length < required)
        {
            return CommandParseStatus.NeedMoreData;
        }

        var code = data[1];
        command = new PlayerStatusEffectCommand(code);
        consumed = required;
        return CommandParseStatus.Success;
    }

    private static CommandParseStatus TryReadSpriteChat(
        ReadOnlySpan<byte> data,
        out IServerCommand? command,
        out int consumed,
        out string? error)
    {
        command = default;
        consumed = 0;
        error = null;
        const int header = 4;
        if (data.Length < header)
        {
            return CommandParseStatus.NeedMoreData;
        }

        var spriteId = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(1, 2));
        var length = data[3];
        var required = header + length;
        if (data.Length < required)
        {
            return CommandParseStatus.NeedMoreData;
        }

        string message;
        try
        {
            message = length > 0 ? Latin1Encoding.GetString(data.Slice(4, length)) : string.Empty;
        }
        catch (Exception ex)
        {
            error = $"Error decodificando mensaje de sprite {spriteId}: {ex.Message}";
            consumed = required;
            return CommandParseStatus.Error;
        }

        command = new SpriteChatCommand(spriteId, message);
        consumed = required;
        return CommandParseStatus.Success;
    }

    private static CommandParseStatus TryReadSpriteNotification(
        ReadOnlySpan<byte> data,
        out IServerCommand? command,
        out int consumed)
    {
        command = default;
        consumed = 0;
        const int required = 4;
        if (data.Length < required)
        {
            return CommandParseStatus.NeedMoreData;
        }

        var spriteId = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(1, 2));
        var code = data[3];
        command = new SpriteNotificationCommand(spriteId, code);
        consumed = required;
        return CommandParseStatus.Success;
    }

    private static CommandParseStatus TryReadServerInfoMessage(
        ReadOnlySpan<byte> data,
        out IServerCommand? command,
        out int consumed)
    {
        command = default;
        consumed = 0;
        const int required = 2;
        if (data.Length < required)
        {
            return CommandParseStatus.NeedMoreData;
        }

        var code = data[1];
        command = new ServerInfoMessageCommand(code);
        consumed = required;
        return CommandParseStatus.Success;
    }
}
