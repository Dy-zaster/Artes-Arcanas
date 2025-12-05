using System;
using System.Collections.Generic;
using System.IO;
using LegacyDataExtractor.Models;

namespace LegacyDataExtractor.Parsing;

public sealed class MapFileParser
{
    private const int TerrainSize = 64;
    private const int InventorySlots = 30; // MAX_ARTEFACTOS + 1

    public MapFile Parse(string path)
    {
        using var stream = File.OpenRead(path);
        using var reader = new LegacyBinaryReader(stream);

        var header = ReadHeader(reader);
        var terrain = ReadTerrain(reader);
        var graphics = ReadGraphics(reader, header.GraphicCount);
        var sensors = ReadSensors(reader, header.SensorCount);
        var nests = ReadNests(reader, header.NestCount);
        var merchants = ReadMerchants(reader, header.MerchantCount);
        var extended = ReadExtendedData(reader, header.ExtendedBytes);

        return new MapFile(header, terrain, graphics, sensors, nests, merchants, extended);
    }

    private static MapHeader ReadHeader(LegacyBinaryReader reader)
    {
        var name = reader.ReadShortString(23);
        var graphicCount = reader.ReadUInt16();
        var flags = reader.ReadUInt16();
        var north = reader.ReadByte();
        var south = reader.ReadByte();
        var east = reader.ReadByte();
        var west = reader.ReadByte();
        var version = reader.ReadByte();
        var nests = reader.ReadByte();
        var npcCount = reader.ReadByte(); // reserved
        var merchants = reader.ReadByte();
        var sensors = reader.ReadByte();
        _ = reader.ReadByte(); // nousado1
        _ = reader.ReadByte(); // nousado2
        _ = reader.ReadByte(); // nousado3
        _ = reader.ReadByte(); // nousado5
        _ = reader.ReadByte(); // nousado6
        _ = reader.ReadByte(); // nousado7
        var extendedBytes = reader.ReadByte();

        return new MapHeader(
            name,
            graphicCount,
            flags,
            north,
            south,
            east,
            west,
            version,
            nests,
            merchants,
            sensors,
            extendedBytes
        );
    }

    private static IReadOnlyList<IReadOnlyList<byte>> ReadTerrain(LegacyBinaryReader reader)
    {
        var rows = new List<IReadOnlyList<byte>>(TerrainSize);
        for (var y = 0; y < TerrainSize; y++)
        {
            var row = reader.ReadBytesExact(TerrainSize);
            rows.Add(Array.AsReadOnly(row));
        }
        return rows;
    }

    private static IReadOnlyList<StaticGraphic> ReadGraphics(LegacyBinaryReader reader, int count)
    {
        var result = new List<StaticGraphic>(count);
        for (var i = 0; i < count; i++)
        {
            var code = reader.ReadUInt16();
            var x = reader.ReadByte();
            var y = reader.ReadByte();
            var flags = reader.ReadByte();
            var subLayer = reader.ReadByte();
            result.Add(new StaticGraphic(code, x, y, flags, subLayer));
        }

        return result;
    }

    private static IReadOnlyList<SensorRecord> ReadSensors(LegacyBinaryReader reader, int count)
    {
        var result = new List<SensorRecord>(count);
        for (var i = 0; i < count; i++)
        {
            var type = reader.ReadByte();
            var x = reader.ReadByte();
            var y = reader.ReadByte();
            var key1 = reader.ReadByte();
            var key2 = reader.ReadByte();
            var data1 = reader.ReadByte();
            var data2 = reader.ReadByte();
            var data3 = reader.ReadByte();
            var data4 = reader.ReadByte();
            var flags = reader.ReadByte();
            var text = reader.ReadShortString(127);
            result.Add(new SensorRecord(type, x, y, key1, key2, data1, data2, data3, data4, flags, text));
        }

        return result;
    }

    private static IReadOnlyList<NestRecord> ReadNests(LegacyBinaryReader reader, int count)
    {
        var result = new List<NestRecord>(count);
        for (var i = 0; i < count; i++)
        {
            var type = reader.ReadByte();
            var x = reader.ReadByte();
            var y = reader.ReadByte();
            var qty = reader.ReadByte();
            result.Add(new NestRecord(type, x, y, qty));
        }

        return result;
    }

    private static IReadOnlyList<MerchantRecord> ReadMerchants(LegacyBinaryReader reader, int count)
    {
        var merchants = new List<MerchantRecord>(count);
        for (var i = 0; i < count; i++)
        {
            var type = reader.ReadByte();
            var x = reader.ReadByte();
            var y = reader.ReadByte();
            var monster = reader.ReadByte();

            var inventory = new List<ArtefactSlot>(InventorySlots);
            for (var slot = 0; slot < InventorySlots; slot++)
            {
                var id = reader.ReadByte();
                var modifier = reader.ReadByte();
                inventory.Add(new ArtefactSlot(id, modifier));
            }

            var inflation = new byte[InventorySlots];
            for (var slot = 0; slot < InventorySlots; slot++)
            {
                inflation[slot] = reader.ReadByte();
            }

            var text = reader.ReadShortString(79);
            merchants.Add(new MerchantRecord(type, x, y, monster, inventory, Array.AsReadOnly(inflation), text));
        }

        return merchants;
    }

    private static MapExtendedData ReadExtendedData(LegacyBinaryReader reader, byte length)
    {
        if (length == 0)
        {
            return MapExtendedData.Empty;
        }

        var buffer = reader.ReadBytesExact(length);
        using var memory = new MemoryStream(buffer, writable: false);
        using var extReader = new LegacyBinaryReader(memory, leaveOpen: true);

        byte respawnX = 0;
        byte respawnY = 0;
        byte respawnMap = 0;
        int dungeonFlags = 0;
        int autoResetFlags = 0;

        if (TryReadByte(extReader, out var value)) respawnX = value;
        if (TryReadByte(extReader, out value)) respawnY = value;
        if (TryReadByte(extReader, out value)) respawnMap = value;
        TryReadByte(extReader, out _); // unused padding
        if (TryReadInt32(extReader, out var intValue)) dungeonFlags = intValue;
        if (TryReadInt32(extReader, out intValue)) autoResetFlags = intValue;

        var flagBehavior = ReadByteArray(extReader, 32);
        var flagData1 = ReadByteArray(extReader, 32);
        var flagData2 = ReadByteArray(extReader, 32);
        var detectorBehavior = ReadByteArray(extReader, 8);
        var detectorData1 = ReadByteArray(extReader, 8);
        var detectorData2 = ReadByteArray(extReader, 8);
        var flagsToDetect = ReadIntArray(extReader, 8);
        var expectedFlagStates = ReadIntArray(extReader, 8);

        return new MapExtendedData(
            respawnX,
            respawnY,
            respawnMap,
            dungeonFlags,
            autoResetFlags,
            Array.AsReadOnly(flagBehavior),
            Array.AsReadOnly(flagData1),
            Array.AsReadOnly(flagData2),
            Array.AsReadOnly(detectorBehavior),
            Array.AsReadOnly(detectorData1),
            Array.AsReadOnly(detectorData2),
            Array.AsReadOnly(flagsToDetect),
            Array.AsReadOnly(expectedFlagStates)
        );
    }

    private static bool TryReadByte(BinaryReader reader, out byte value)
    {
        if (reader.BaseStream.Position < reader.BaseStream.Length)
        {
            value = reader.ReadByte();
            return true;
        }

        value = 0;
        return false;
    }

    private static bool TryReadInt32(BinaryReader reader, out int value)
    {
        if (reader.BaseStream.Position + sizeof(int) <= reader.BaseStream.Length)
        {
            value = reader.ReadInt32();
            return true;
        }

        value = 0;
        return false;
    }

    private static byte[] ReadByteArray(BinaryReader reader, int length)
    {
        var remaining = reader.BaseStream.Length - reader.BaseStream.Position;
        if (remaining <= 0)
        {
            return Array.Empty<byte>();
        }

        var sliceLength = (int)Math.Min(length, remaining);
        return reader.ReadBytes(sliceLength);
    }

    private static int[] ReadIntArray(BinaryReader reader, int length)
    {
        var values = new List<int>(length);
        for (var i = 0; i < length; i++)
        {
            if (TryReadInt32(reader, out var value))
            {
                values.Add(value);
            }
            else
            {
                break;
            }
        }

        return values.ToArray();
    }
}
