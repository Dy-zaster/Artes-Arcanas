using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using ArtesArcanas.Server.Core.Legacy;

namespace ArtesArcanas.Server.Core.World;

internal sealed class LegacyGroundItemStore
{
    private readonly ConcurrentDictionary<uint, LegacyGroundTile> _tiles = new();
    private int _nextId = 1;

    public bool TryAddItem(
        byte mapId,
        byte x,
        byte y,
        ushort ownerCode,
        LegacyInventorySlot slotData,
        byte quantity,
        out LegacyGroundItem entry,
        out bool createdBag)
    {
        entry = default;
        createdBag = false;
        var key = ComposeKey(mapId, x, y);
        var tile = _tiles.GetOrAdd(key, _ => new LegacyGroundTile(mapId, x, y));
        lock (tile.Sync)
        {
            createdBag = !tile.HasItems;

            if (!tile.TryReserveSlot(out var bagSlot))
            {
                return false;
            }

            entry = new LegacyGroundItem(
                Interlocked.Increment(ref _nextId),
                ownerCode,
                bagSlot,
                slotData.ItemId,
                slotData.Modifier,
                Math.Max((byte)1, quantity),
                DateTime.UtcNow);

            tile.SetSlot(entry);
            return true;
        }
    }

    public IReadOnlyList<LegacyGroundItem> GetItems(byte mapId, byte x, byte y)
    {
        if (!_tiles.TryGetValue(ComposeKey(mapId, x, y), out var tile))
        {
            return Array.Empty<LegacyGroundItem>();
        }

        lock (tile.Sync)
        {
            return tile.Snapshot();
        }
    }

    public IReadOnlyList<LegacyGroundItem> TakeAll(byte mapId, byte x, byte y)
    {
        if (!_tiles.TryGetValue(ComposeKey(mapId, x, y), out var tile))
        {
            return Array.Empty<LegacyGroundItem>();
        }

        lock (tile.Sync)
        {
            var snapshot = tile.Snapshot();
            if (snapshot.Count == 0)
            {
                return Array.Empty<LegacyGroundItem>();
            }

            tile.Clear();
            _tiles.TryRemove(ComposeKey(mapId, x, y), out _);
            return snapshot;
        }
    }

    public bool TryTake(byte mapId, byte x, byte y, byte bagSlot, out LegacyGroundItem? removed, out bool bagCleared)
    {
        removed = null;
        bagCleared = false;
        var key = ComposeKey(mapId, x, y);
        if (!_tiles.TryGetValue(key, out var tile))
        {
            return false;
        }

        lock (tile.Sync)
        {
            if (!tile.TryRemoveSlot(bagSlot, out removed))
            {
                return false;
            }

            if (!tile.HasItems)
            {
                bagCleared = true;
                _tiles.TryRemove(key, out _);
            }

            return true;
        }
    }

    public void PutBack(byte mapId, byte x, byte y, LegacyGroundItem entry)
    {
        var key = ComposeKey(mapId, x, y);
        var tile = _tiles.GetOrAdd(key, _ => new LegacyGroundTile(mapId, x, y));
        lock (tile.Sync)
        {
            tile.SetSlot(entry);
        }
    }

    public bool TryGetBagSlots(byte mapId, byte x, byte y, out LegacyInventorySlot[] slots)
    {
        slots = Array.Empty<LegacyInventorySlot>();
        if (!_tiles.TryGetValue(ComposeKey(mapId, x, y), out var tile))
        {
            return false;
        }

        lock (tile.Sync)
        {
            if (!tile.HasItems)
            {
                return false;
            }

            slots = tile.BuildSlotSnapshot();
            return true;
        }
    }

    public bool TryGetBagType(byte mapId, byte x, byte y, out LegacyBagType bagType)
    {
        bagType = LegacyBagType.None;
        if (!_tiles.TryGetValue(ComposeKey(mapId, x, y), out var tile))
        {
            return false;
        }

        lock (tile.Sync)
        {
            bagType = tile.BagType;
            return tile.HasItems;
        }
    }

    public bool TrySetBagType(byte mapId, byte x, byte y, LegacyBagType bagType, out LegacyBagType previousType)
    {
        previousType = LegacyBagType.None;
        if (!_tiles.TryGetValue(ComposeKey(mapId, x, y), out var tile))
        {
            return false;
        }

        lock (tile.Sync)
        {
            previousType = tile.BagType;
            if (tile.BagType == bagType)
            {
                return false;
            }

            tile.BagType = bagType;
            return true;
        }
    }

    public IReadOnlyList<LegacyGroundBagInfo> GetBags(byte mapId)
    {
        var results = new List<LegacyGroundBagInfo>();
        foreach (var entry in _tiles)
        {
            var tile = entry.Value;
            if (tile.MapId != mapId)
            {
                continue;
            }

            lock (tile.Sync)
            {
                if (!tile.HasItems)
                {
                    continue;
                }

                results.Add(new LegacyGroundBagInfo(tile.MapId, tile.X, tile.Y, tile.BagType));
            }
        }

        return results;
    }

    private static uint ComposeKey(byte mapId, byte x, byte y) =>
        (uint)((mapId << 16) | (y << 8) | x);

    private sealed class LegacyGroundTile
    {
        private readonly LegacyGroundItem?[] _slots = new LegacyGroundItem?[LegacyConstants.InventoryArtifactSlots];
        private int _occupiedSlots;

        public LegacyGroundTile(byte mapId, byte x, byte y)
        {
            MapId = mapId;
            X = x;
            Y = y;
        }

        public byte MapId { get; }
        public byte X { get; }
        public byte Y { get; }
        public object Sync { get; } = new();
        public LegacyBagType BagType { get; set; } = LegacyBagType.None;
        public bool HasItems => _occupiedSlots > 0;

        public bool TryReserveSlot(out byte bagSlot)
        {
            for (var i = 0; i < _slots.Length; i++)
            {
                if (_slots[i].HasValue)
                {
                    continue;
                }

                bagSlot = (byte)i;
                return true;
            }

            bagSlot = 0;
            return false;
        }

        public void SetSlot(LegacyGroundItem entry)
        {
            if (entry.BagSlot >= _slots.Length)
            {
                return;
            }

            if (!_slots[entry.BagSlot].HasValue)
            {
                _occupiedSlots++;
            }

            _slots[entry.BagSlot] = entry;
        }

        public bool TryRemoveSlot(byte slot, out LegacyGroundItem? removed)
        {
            removed = null;
            if (slot >= _slots.Length)
            {
                return false;
            }

            if (!_slots[slot].HasValue)
            {
                return false;
            }

            removed = _slots[slot];
            _slots[slot] = null;
            _occupiedSlots = Math.Max(0, _occupiedSlots - 1);
            return true;
        }

        public IReadOnlyList<LegacyGroundItem> Snapshot()
        {
            var items = new List<LegacyGroundItem>();
            for (var i = 0; i < _slots.Length; i++)
            {
                if (_slots[i].HasValue)
                {
                    items.Add(_slots[i]!.Value);
                }
            }

            return items;
        }

        public LegacyInventorySlot[] BuildSlotSnapshot()
        {
            var slots = new LegacyInventorySlot[_slots.Length];
            for (var i = 0; i < _slots.Length; i++)
            {
                slots[i] = _slots[i].HasValue
                    ? new LegacyInventorySlot(_slots[i]!.Value.ItemId, _slots[i]!.Value.Modifier)
                    : LegacyInventorySlot.Empty;
            }

            return slots;
        }

        public void Clear()
        {
            Array.Clear(_slots, 0, _slots.Length);
            _occupiedSlots = 0;
            BagType = LegacyBagType.None;
        }
    }
}

internal readonly record struct LegacyGroundItem(
    int Id,
    ushort OwnerCode,
    byte BagSlot,
    byte ItemId,
    byte Modifier,
    byte Quantity,
    DateTime CreatedUtc);

internal readonly record struct LegacyGroundBagInfo(
    byte MapId,
    byte X,
    byte Y,
    LegacyBagType BagType);
