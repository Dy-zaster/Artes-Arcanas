using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Laa.Monogame.Client.World;
using Microsoft.Xna.Framework;

namespace Laa.Monogame.Client.Networking;

public sealed class MockNetworkClient : INetworkClient
{
    private readonly TimeSpan _tickInterval = TimeSpan.FromSeconds(1.5);
    private readonly List<Func<NetworkMessage>> _script = new();
    private readonly List<MockSpriteState> _sprites = new();
    private Timer? _timer;
    private int _currentIndex;

    public event EventHandler<NetworkEventArgs>? MessageReceived;

    public Task ConnectAsync(string host, int port)
    {
        _currentIndex = 0;
        _script.Clear();
        _sprites.Clear();
        BuildScript();
        _timer = new Timer(OnTick, null, _tickInterval, _tickInterval);
        return Task.CompletedTask;
    }

    public Task DisconnectAsync()
    {
        _timer?.Dispose();
        _timer = null;
        return Task.CompletedTask;
    }

    public Task SendAsync(ReadOnlyMemory<byte> payload)
    {
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }

    private void BuildScript()
    {
        var rand = new Random(0x12345);
        var spriteCount = 5;
        for (var i = 0; i < spriteCount; i++)
        {
            var sprite = new MockSpriteState
            {
                Id = i,
                TilePosition = new Point(rand.Next(10, 60), rand.Next(10, 60)),
                Direction = rand.Next(0, 8)
            };
            _sprites.Add(sprite);
        }

        foreach (var sprite in _sprites)
        {
            _script.Add(() => BuildSpawnChunk(sprite));
        }

        _script.Add(() => BuildBatchMoveChunk());
        _script.Add(() => BuildRandomActionsChunk(rand));
    }

    private void OnTick(object? state)
    {
        if (_currentIndex >= _script.Count)
        {
            _currentIndex = 0;
        }

        var factory = _script[_currentIndex++];
        var message = factory();
        MessageReceived?.Invoke(this, new NetworkEventArgs(message));
    }

    private NetworkMessage BuildSpawnChunk(MockSpriteState state)
    {
        var buffer = Compose(
            BuildPositionCommand(state.Id, state.TilePosition.X, state.TilePosition.Y, state.Direction),
            BuildActionCommand(state.Id, (byte)MonsterAction.Idle));
        return new NetworkMessage(NetworkMessageType.RawServerStream, buffer);
    }

    private NetworkMessage BuildBatchMoveChunk()
    {
        var entries = new List<byte[]>();
        var rand = new Random(0x9876);
        foreach (var sprite in _sprites)
        {
            var delta = new Point(rand.Next(-2, 3), rand.Next(-2, 3));
            sprite.TilePosition = new Point(
                Math.Clamp(sprite.TilePosition.X + delta.X, 5, 70),
                Math.Clamp(sprite.TilePosition.Y + delta.Y, 5, 70));
            sprite.Direction = (sprite.Direction + rand.Next(0, 3)) % 8;
            entries.Add(BuildPositionEntry(sprite));
        }

        var buffer = BuildBatchPositionCommand(entries);
        return new NetworkMessage(NetworkMessageType.RawServerStream, buffer);
    }

    private NetworkMessage BuildRandomActionsChunk(Random rand)
    {
        var commands = _sprites
            .Select(sprite =>
            {
                var roll = rand.NextDouble();
                var action = roll switch
                {
                    < 0.2 => MonsterAction.Attack,
                    < 0.3 => MonsterAction.Dead,
                    < 0.6 => MonsterAction.Moving,
                    _ => MonsterAction.Idle
                };
                return BuildActionCommand(sprite.Id, (byte)action);
            })
            .ToArray();

        var buffer = Compose(commands);
        return new NetworkMessage(NetworkMessageType.RawServerStream, buffer);
    }

    private static byte[] BuildPositionCommand(int id, int tileX, int tileY, int direction)
    {
        var buffer = new byte[6];
        buffer[0] = (byte)'P';
        BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(1, 2), (ushort)id);
        buffer[3] = (byte)Math.Clamp(tileX, 0, 255);
        buffer[4] = (byte)Math.Clamp(tileY, 0, 255);
        buffer[5] = (byte)Math.Clamp(direction, 0, 7);
        return buffer;
    }

    private static byte[] BuildPositionEntry(MockSpriteState state)
    {
        var buffer = new byte[5];
        BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(0, 2), (ushort)state.Id);
        buffer[2] = (byte)Math.Clamp(state.TilePosition.X, 0, 255);
        buffer[3] = (byte)Math.Clamp(state.TilePosition.Y, 0, 255);
        buffer[4] = (byte)Math.Clamp(state.Direction, 0, 7);
        return buffer;
    }

    private static byte[] BuildBatchPositionCommand(IEnumerable<byte[]> entries)
    {
        var entryList = entries.ToList();
        var count = Math.Clamp(entryList.Count, 0, 255);
        var length = 2 + count * 5;
        var buffer = new byte[length];
        buffer[0] = (byte)'r';
        buffer[1] = (byte)count;
        var offset = 2;
        for (var i = 0; i < count; i++)
        {
            var entry = entryList[i];
            entry.AsSpan().CopyTo(buffer.AsSpan(offset, 5));
            offset += 5;
        }

        return buffer;
    }

    private static byte[] BuildActionCommand(int id, byte action)
    {
        var opcode = (byte)(160 + action);
        var buffer = new byte[3];
        buffer[0] = opcode;
        BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(1, 2), (ushort)id);
        return buffer;
    }

    private static byte[] Compose(params byte[][] commands)
    {
        var total = commands.Sum(cmd => cmd.Length);
        var buffer = new byte[total];
        var offset = 0;
        foreach (var command in commands)
        {
            command.AsSpan().CopyTo(buffer.AsSpan(offset, command.Length));
            offset += command.Length;
        }

        return buffer;
    }

    private sealed class MockSpriteState
    {
        public int Id { get; set; }
        public Point TilePosition { get; set; }
        public int Direction { get; set; }
    }
}
