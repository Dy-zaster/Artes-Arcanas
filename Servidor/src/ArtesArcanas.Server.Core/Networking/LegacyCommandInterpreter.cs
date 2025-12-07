using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ArtesArcanas.Server.Core.Logging;
using ArtesArcanas.Server.Core.World;

namespace ArtesArcanas.Server.Core.Networking;

internal sealed class LegacyCommandInterpreter
{
    private readonly LegacyWorldState _world;
    private readonly IServerLogger _logger;
    private readonly ConcurrentDictionary<ushort, CommandAccumulator> _buffers = new();

    public LegacyCommandInterpreter(LegacyWorldState world, IServerLogger logger)
    {
        _world = world;
        _logger = logger;
    }

    public void Append(LegacyCommandBuffer buffer)
    {
        if (!_world.TryGetPlayer(buffer.SessionCode, out _))
        {
            _buffers.TryRemove(buffer.SessionCode, out _);
            return;
        }

        var accumulator = _buffers.GetOrAdd(buffer.SessionCode, _ => new CommandAccumulator());
        accumulator.Append(buffer.Data);
        ProcessBuffer(buffer.SessionCode, accumulator);
    }

    private void ProcessBuffer(ushort sessionCode, CommandAccumulator accumulator)
    {
        while (TryProcessNext(sessionCode, accumulator))
        {
        }
    }

    private bool TryProcessNext(ushort sessionCode, CommandAccumulator accumulator)
    {
        if (accumulator.Available <= 0)
        {
            return false;
        }

        var reader = new CommandBufferReader(accumulator);
        if (!reader.TryReadByte(out var opcodeByte))
        {
            return false;
        }

        var opcode = (char)opcodeByte;
        switch (opcode)
        {
            case 'm':
                {
                    if (!reader.TryReadByte(out var direction))
                    {
                        return false;
                    }

                    _world.EnqueueAction(sessionCode, LegacyPlayerAction.MoveStep(direction, reader.GetConsumedBytes()));
                    accumulator.Consume(reader.Consumed);
                    return true;
                }
            case 'M':
                {
                    if (!reader.TryReadUInt16(out var packedDestination))
                    {
                        return false;
                    }

                    _world.EnqueueAction(sessionCode, LegacyPlayerAction.MoveToCoordinate(packedDestination, reader.GetConsumedBytes()));
                    accumulator.Consume(reader.Consumed);
                    return true;
                }
            case 'W':
                {
                    if (!reader.TryReadUInt16(out var followTarget))
                    {
                        return false;
                    }

                    _world.EnqueueAction(sessionCode, LegacyPlayerAction.FollowEntity(followTarget, reader.GetConsumedBytes()));
                    accumulator.Consume(reader.Consumed);
                    return true;
                }
            case 'A':
            case 'B':
                {
                    if (!reader.TryReadUInt16(out var attackTarget))
                    {
                        return false;
                    }

                    var defensive = opcode == 'B';
                    _world.EnqueueAction(sessionCode, LegacyPlayerAction.Attack((byte)opcode, attackTarget, reader.GetConsumedBytes(), defensive));
                    accumulator.Consume(reader.Consumed);
                    return true;
                }
            case 'y':
            case 'Y':
                {
                    if (!reader.TryReadUInt16(out var spellTarget))
                    {
                        return false;
                    }

                    var continuous = opcode == 'Y';
                    _world.EnqueueAction(sessionCode, LegacyPlayerAction.CastSpell((byte)opcode, spellTarget, reader.GetConsumedBytes(), continuous));
                    accumulator.Consume(reader.Consumed);
                    return true;
                }
            case 'J':
                {
                    if (!reader.TryReadByte(out var inventorySlot))
                    {
                        return false;
                    }

                    _world.EnqueueAction(sessionCode, LegacyPlayerAction.CastSpellOnItem(inventorySlot, reader.GetConsumedBytes()));
                    accumulator.Consume(reader.Consumed);
                    return true;
                }
            case 'O':
                {
                    if (!reader.TryReadByte(out var subOp))
                    {
                        return false;
                    }

                    switch ((char)subOp)
                    {
                        case 'a':
                            if (!reader.TryReadUInt16(out var attackCode))
                            {
                                return false;
                            }

                            _world.EnqueueAction(sessionCode,
                                LegacyPlayerAction.CommandFollowers((byte)subOp, LegacyPlayerActionType.CommandFollowersAttack, attackCode, reader.GetConsumedBytes()));
                            accumulator.Consume(reader.Consumed);
                            return true;
                        case 's':
                            if (!reader.TryReadUInt16(out var followCode))
                            {
                                return false;
                            }

                            _world.EnqueueAction(sessionCode,
                                LegacyPlayerAction.CommandFollowers((byte)subOp, LegacyPlayerActionType.CommandFollowersFollow, followCode, reader.GetConsumedBytes()));
                            accumulator.Consume(reader.Consumed);
                            return true;
                        case 'd':
                            _world.EnqueueAction(sessionCode,
                                LegacyPlayerAction.CommandFollowers((byte)subOp, LegacyPlayerActionType.CommandFollowersStop, 0, reader.GetConsumedBytes()));
                            accumulator.Consume(reader.Consumed);
                            return true;
                        default:
                            var unknownControl = accumulator.Drain();
                            _logger.Warning($"[{sessionCode}] Subcomando de control de monstruos no soportado '{(char)subOp}' ({BitConverter.ToString(unknownControl)})");
                            return false;
                    }
                }
            case 'c':
                {
                    if (!reader.TryReadByte(out var consumeSlot))
                    {
                        return false;
                    }

                    _world.EnqueueAction(sessionCode, LegacyPlayerAction.ConsumeItem(consumeSlot, reader.GetConsumedBytes()));
                    accumulator.Consume(reader.Consumed);
                    return true;
                }
            case 'u':
                {
                    if (!reader.TryReadByte(out var useSlot))
                    {
                        return false;
                    }

                    _world.EnqueueAction(sessionCode, LegacyPlayerAction.UseItem(useSlot, reader.GetConsumedBytes()));
                    accumulator.Consume(reader.Consumed);
                    return true;
                }
            case 'F':
                {
                    if (!reader.TryReadByte(out var recipe))
                    {
                        return false;
                    }

                    if (!reader.TryReadByte(out var count))
                    {
                        return false;
                    }

                    _world.EnqueueAction(sessionCode, LegacyPlayerAction.CraftItem(recipe, count, reader.GetConsumedBytes()));
                    accumulator.Consume(reader.Consumed);
                    return true;
                }
            case 'S':
                {
                    if (!reader.TryReadByte(out var slot))
                    {
                        return false;
                    }

                    if (!reader.TryReadByte(out var quantity))
                    {
                        return false;
                    }

                    _world.EnqueueAction(sessionCode, LegacyPlayerAction.DropItem(slot, quantity, reader.GetConsumedBytes()));
                    accumulator.Consume(reader.Consumed);
                    return true;
                }
            case 'R':
                {
                    _world.EnqueueAction(sessionCode, LegacyPlayerAction.InspectGround(reader.GetConsumedBytes()));
                    accumulator.Consume(reader.Consumed);
                    return true;
                }
            case 'r':
                {
                    if (!reader.TryReadByte(out var pickSlot))
                    {
                        return false;
                    }

                    if (!reader.TryReadByte(out var pickQuantity))
                    {
                        return false;
                    }

                    _world.EnqueueAction(sessionCode, LegacyPlayerAction.PickSpecific(pickSlot, pickQuantity, reader.GetConsumedBytes()));
                    accumulator.Consume(reader.Consumed);
                    return true;
                }
            case 'a':
                {
                    _world.EnqueueAction(sessionCode, LegacyPlayerAction.PickAll(reader.GetConsumedBytes()));
                    accumulator.Consume(reader.Consumed);
                    return true;
                }
            case 's':
                {
                    if (!reader.TryReadByte(out var y) || !reader.TryReadByte(out var x))
                    {
                        return false;
                    }

                    _world.EnqueueAction(sessionCode, LegacyPlayerAction.SensorClick(y, x, reader.GetConsumedBytes()));
                    accumulator.Consume(reader.Consumed);
                    return true;
                }
            case '$':
                {
                    if (!reader.TryReadUInt16(out var low))
                    {
                        return false;
                    }

                    if (!reader.TryReadByte(out var high))
                    {
                        return false;
                    }

                    var amount = low | (high << 16);
                    _world.EnqueueAction(sessionCode, LegacyPlayerAction.WithdrawMoney(amount, reader.GetConsumedBytes()));
                    accumulator.Consume(reader.Consumed);
                    return true;
                }
            default:
                {
                    var data = accumulator.Drain();
                    _logger.Warning($"[{sessionCode}] Comando legacy no soportado '{opcode}' ({BitConverter.ToString(data)})");
                    return false;
                }
        }
    }

    private sealed class CommandAccumulator
    {
        private readonly List<byte> _buffer = new();
        private int _offset;

        public int Available => _buffer.Count - _offset;

        public void Append(ReadOnlySpan<byte> data)
        {
            if (data.IsEmpty)
            {
                return;
            }

            _buffer.AddRange(data.ToArray());
        }

        public void Consume(int count)
        {
            if (count <= 0)
            {
                return;
            }

            _offset += count;
            CompactIfNeeded();
        }

        public byte[] Drain()
        {
            var span = AsSpan();
            var copy = span.ToArray();
            _buffer.Clear();
            _offset = 0;
            return copy;
        }

        private void CompactIfNeeded()
        {
            if (_offset == 0)
            {
                return;
            }

            if (_offset < 1024 && _offset < _buffer.Count / 2)
            {
                return;
            }

            _buffer.RemoveRange(0, _offset);
            _offset = 0;
        }

        internal ReadOnlySpan<byte> AsSpan() =>
            CollectionsMarshal.AsSpan(_buffer).Slice(_offset);

        internal byte[] Snapshot(int count)
        {
            if (count <= 0)
            {
                return Array.Empty<byte>();
            }

            return CollectionsMarshal.AsSpan(_buffer).Slice(_offset, count).ToArray();
        }
    }

    private ref struct CommandBufferReader
    {
        private readonly CommandAccumulator _accumulator;
        private int _consumed;

        public CommandBufferReader(CommandAccumulator accumulator)
        {
            _accumulator = accumulator;
            _consumed = 0;
        }

        public int Consumed => _consumed;

        public bool TryReadByte(out byte value)
        {
            if (!Ensure(1))
            {
                value = 0;
                return false;
            }

            value = _accumulator.AsSpan()[_consumed];
            _consumed += 1;
            return true;
        }

        public bool TryReadUInt16(out ushort value)
        {
            if (!Ensure(2))
            {
                value = 0;
                return false;
            }

            value = BinaryPrimitives.ReadUInt16LittleEndian(_accumulator.AsSpan().Slice(_consumed, 2));
            _consumed += 2;
            return true;
        }

        public byte[] GetConsumedBytes() => _accumulator.Snapshot(_consumed);

        private bool Ensure(int count) => _accumulator.Available >= _consumed + count;
    }
}
