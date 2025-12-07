using System;
using System.Collections.Generic;
using System.IO;
using ArtesArcanas.Server.Core.Legacy;
using ArtesArcanas.Server.Core.Logging;
using ArtesArcanas.Server.Core.World;

namespace ArtesArcanas.Server.Core.World.Maps;

internal sealed class LegacyMapLoader
{
    private readonly string _directory;
    private readonly IServerLogger _logger;

    public LegacyMapLoader(string directory, IServerLogger logger)
    {
        _directory = directory;
        _logger = logger;
    }

    public IReadOnlyDictionary<byte, LegacyMapDefinition> LoadMaps(int highestMapId)
    {
        var limit = Math.Clamp(highestMapId, 0, LegacyConstants.MaxMaps);
        var maps = new Dictionary<byte, LegacyMapDefinition>();

        for (var id = 0; id <= limit; id++)
        {
            var mapId = (byte)id;
            var path = Path.Combine(_directory, $"{mapId}.mpv");
            if (!File.Exists(path))
            {
                continue;
            }

            try
            {
                var definition = ReadMap(path, mapId);
                if (definition is not null)
                {
                    maps[mapId] = definition;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error cargando mapa {mapId} ({path}).", ex);
            }
        }

        return maps;
    }

    private static LegacyMapDefinition? ReadMap(string path, byte id)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var reader = new BinaryReader(stream, LegacyConstants.LegacyEncoding, leaveOpen: false);

        var header = LegacyMapBinaryHeader.Read(reader);
        var baseTerrain = reader.ReadBytes(LegacyMapBinaryLayout.BaseTerrainSize);
        if (baseTerrain.Length != LegacyMapBinaryLayout.BaseTerrainSize)
        {
            throw new InvalidDataException($"Mapa {id}: datos de terreno incompletos ({baseTerrain.Length}).");
        }
        var grid = LegacyMapGrid.FromBaseTerrain(baseTerrain);

        Skip(reader, header.GraphicCount * LegacyMapBinaryLayout.GraphicRecordSize);
        var sensors = ReadSensors(reader, header.SensorCount);
        var nests = ReadNests(reader, header.NestCount);
        var merchants = ReadMerchants(reader, header.MerchantCount);

        var extended = LegacyMapExtendedHeader.Read(reader, header.BytesOfExtendedData);

        return new LegacyMapDefinition(
            id,
            header.Name,
            header.Flags,
            header.MapNorth,
            header.MapSouth,
            header.MapEast,
            header.MapWest,
            header.Version,
            header.SensorCount,
            header.NestCount,
            header.MerchantCount,
            header.GraphicCount,
            baseTerrain,
            extended,
            grid,
            merchants,
            nests,
            sensors);
    }

    private static void Skip(BinaryReader reader, int bytes)
    {
        if (bytes <= 0)
        {
            return;
        }

        reader.BaseStream.Seek(bytes, SeekOrigin.Current);
    }

    private static IReadOnlyList<LegacyMapNestDefinition> ReadNests(BinaryReader reader, int count)
    {
        if (count <= 0)
        {
            return Array.Empty<LegacyMapNestDefinition>();
        }

        var nests = new List<LegacyMapNestDefinition>(count);
        for (var i = 0; i < count; i++)
        {
            var type = reader.ReadByte();
            var x = reader.ReadByte();
            var y = reader.ReadByte();
            var quantity = reader.ReadByte();
            nests.Add(new LegacyMapNestDefinition(type, x, y, quantity));
        }

        return nests;
    }

    private static IReadOnlyList<LegacyMapMerchantDefinition> ReadMerchants(BinaryReader reader, int count)
    {
        if (count <= 0)
        {
            return Array.Empty<LegacyMapMerchantDefinition>();
        }

        var merchants = new List<LegacyMapMerchantDefinition>(count);
        for (var i = 0; i < count; i++)
        {
            var type = reader.ReadByte();
            var x = reader.ReadByte();
            var y = reader.ReadByte();
            var monster = reader.ReadByte();

            var items = new LegacyInventorySlot[LegacyConstants.InventoryArtifactSlots];
            for (var slot = 0; slot < items.Length; slot++)
            {
                var itemId = reader.ReadByte();
                var modifier = reader.ReadByte();
                items[slot] = new LegacyInventorySlot(itemId, modifier);
            }

            var inflation = reader.ReadBytes(LegacyConstants.InventoryArtifactSlots);
            merchants.Add(new LegacyMapMerchantDefinition(type, x, y, monster, items, inflation));
        }

        Skip(reader, count * LegacyMapBinaryLayout.MerchantTextSize);
        return merchants;
    }

    private static class LegacyMapBinaryLayout
    {
        public const int BaseMapSize = 64 * 64;
        public const int BaseTerrainSize = BaseMapSize;
        public const int GraphicRecordSize = 6;
        public const int SensorRecordSize = 10;
        public const int SensorTextCapacity = 127;
        public const int NestRecordSize = 4;
        public const int MerchantRecordSize = 94;
        public const int MerchantTextSize = 80;
        public const int ExtendedDataSize = 196;
    }

    private readonly record struct LegacyMapBinaryHeader(
        string Name,
        ushort GraphicCount,
        ushort Flags,
        byte MapNorth,
        byte MapSouth,
        byte MapEast,
        byte MapWest,
        byte Version,
        byte NestCount,
        byte SensorCount,
        byte MerchantCount,
        byte BytesOfExtendedData)
    {
        public static LegacyMapBinaryHeader Read(BinaryReader reader)
        {
            var name = ReadShortString(reader, 23);
            var graphics = reader.ReadUInt16();
            var flags = reader.ReadUInt16();
            var north = reader.ReadByte();
            var south = reader.ReadByte();
            var east = reader.ReadByte();
            var west = reader.ReadByte();
            var version = reader.ReadByte();
            var nests = reader.ReadByte();
            reader.ReadByte(); // N_NPC (unused)
            var merchants = reader.ReadByte();
            var sensors = reader.ReadByte();
            reader.ReadByte(); // nousado1
            reader.ReadByte(); // nousado2
            reader.ReadByte(); // nousado3
            reader.ReadByte(); // nousado5
            reader.ReadByte(); // nousado6
            reader.ReadByte(); // nousado7
            var extendedBytes = reader.ReadByte();

            return new LegacyMapBinaryHeader(
                name,
                graphics,
                flags,
                north,
                south,
                east,
                west,
                version,
                nests,
                sensors,
                merchants,
                extendedBytes);
        }
    }

    private sealed class LegacyMapExtendedHeader
    {
        public static LegacyMapExtendedData Read(BinaryReader reader, byte declaredBytes)
        {
            if (declaredBytes == 0)
            {
                return new LegacyMapExtendedData(0, 0, 0, 0, 0, new byte[32], new byte[32], new byte[32]);
            }

            var captureBytes = Math.Min((int)declaredBytes, LegacyMapBinaryLayout.ExtendedDataSize);
            var buffer = reader.ReadBytes(captureBytes);
            Skip(reader, declaredBytes - captureBytes);

            if (buffer.Length == 0)
            {
                return new LegacyMapExtendedData(0, 0, 0, 0, 0, new byte[32], new byte[32], new byte[32]);
            }

            using var memory = new MemoryStream(buffer);
            using var extReader = new BinaryReader(memory);

            var respawnX = extReader.ReadByte();
            var respawnY = extReader.ReadByte();
            var respawnMap = extReader.ReadByte();
            extReader.ReadByte(); // noUsadoX2
            var flags = extReader.ReadInt32();
            var autoReset = extReader.ReadInt32();
            var behaviors = extReader.ReadBytes(32);
            var data1 = extReader.ReadBytes(32);
            var data2 = extReader.ReadBytes(32);

            return new LegacyMapExtendedData(
                respawnX,
                respawnY,
                respawnMap,
                flags,
                autoReset,
                EnsureLength(behaviors, 32),
                EnsureLength(data1, 32),
                EnsureLength(data2, 32));
        }
    }

    private static byte[] EnsureLength(byte[] input, int size)
    {
        if (input.Length == size)
        {
            return input;
        }

        var buffer = new byte[size];
        Array.Copy(input, buffer, Math.Min(input.Length, size));
        return buffer;
    }

    private static string ReadShortString(BinaryReader reader, int capacity)
    {
            var declaredLength = reader.ReadByte();
            var raw = reader.ReadBytes(capacity);
            var actual = Math.Min((int)declaredLength, capacity);
        if (actual <= 0)
        {
            return string.Empty;
        }

        return LegacyConstants.LegacyEncoding.GetString(raw, 0, actual);
    }
}
    private static IReadOnlyList<LegacyMapSensorDefinition> ReadSensors(BinaryReader reader, int count)
    {
        if (count <= 0)
        {
            return Array.Empty<LegacyMapSensorDefinition>();
        }

        var sensors = new List<LegacyMapSensorDefinition>(count);
        for (var i = 0; i < count; i++)
        {
            var type = (LegacySensorType)reader.ReadByte();
            var x = reader.ReadByte();
            var y = reader.ReadByte();
            var key1 = reader.ReadByte();
            var key2 = reader.ReadByte();
            var data1 = reader.ReadByte();
            var data2 = reader.ReadByte();
            var data3 = reader.ReadByte();
            var data4 = reader.ReadByte();
            var flags = (LegacySensorFlags)reader.ReadByte();
            var text = ReadShortString(reader, LegacyMapBinaryLayout.SensorTextCapacity);
            sensors.Add(new LegacyMapSensorDefinition(type, x, y, key1, key2, data1, data2, data3, data4, flags, text));
        }

        return sensors;
    }
