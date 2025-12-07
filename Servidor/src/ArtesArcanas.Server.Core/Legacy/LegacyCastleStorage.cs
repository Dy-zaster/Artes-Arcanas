using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;

namespace ArtesArcanas.Server.Core.Legacy;

public sealed record LegacyCastleInfo(byte MapId, byte ClanId, byte Taxes, ushort HitPoints, int GuardianFlags, int Gold);

public sealed class LegacyCastleStorage
{
    private readonly List<LegacyCastleInfo> _castles = new(LegacyConstants.MaxMaps + 1);
    private readonly object _sync = new();
    private readonly string _backingFile;

    public IReadOnlyList<LegacyCastleInfo> Castles => _castles;

    public static LegacyCastleStorage Load(string path)
    {
        var storage = new LegacyCastleStorage(path);

        if (!File.Exists(path))
        {
            return storage;
        }

        storage.ReadFile();
        return storage;
    }

    private LegacyCastleStorage(string backingFile)
    {
        _backingFile = backingFile;
    }

    public bool TryUpdate(Func<List<LegacyCastleInfo>, bool> mutator)
    {
        lock (_sync)
        {
            var clone = new List<LegacyCastleInfo>(_castles);
            if (!mutator(clone))
            {
                return false;
            }

            if (!Persist(clone))
            {
                return false;
            }

            _castles.Clear();
            _castles.AddRange(clone);
            return true;
        }
    }

    private void ReadFile()
    {
        using var stream = File.OpenRead(_backingFile);
        using var reader = new BinaryReader(stream, LegacyConstants.LegacyEncoding, leaveOpen: false);

        var mapId = 0;
        while (stream.Position < stream.Length && mapId <= LegacyConstants.MaxMaps)
        {
            var payload = reader.ReadBytes(LegacyConstants.CastleRecordSize);
            if (payload.Length != LegacyConstants.CastleRecordSize)
            {
                break;
            }

            _castles.Add(ParseRecord((byte)mapId, payload));
            mapId++;
        }
    }

    private bool Persist(List<LegacyCastleInfo> castles)
    {
        try
        {
            var directory = Path.GetDirectoryName(_backingFile);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using var stream = new FileStream(_backingFile, FileMode.Create, FileAccess.Write, FileShare.None);
            using var writer = new BinaryWriter(stream, LegacyConstants.LegacyEncoding, leaveOpen: false);

            foreach (var castle in castles)
            {
                var record = BuildRecord(castle);
                writer.Write(record);
            }

            writer.Flush();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static LegacyCastleInfo ParseRecord(byte mapId, byte[] payload)
    {
        var span = payload.AsSpan();

        var gold = BinaryPrimitives.ReadInt32LittleEndian(span[..4]);
        var clan = span[4];
        var taxes = span[5];
        var hp = BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(6, 2));
        var guardian = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(8, 4));

        return new LegacyCastleInfo(mapId, clan, taxes, hp, guardian, gold);
    }

    private static byte[] BuildRecord(LegacyCastleInfo castle)
    {
        var buffer = new byte[LegacyConstants.CastleRecordSize];
        var span = buffer.AsSpan();
        BinaryPrimitives.WriteInt32LittleEndian(span[..4], castle.Gold);
        span[4] = castle.ClanId;
        span[5] = castle.Taxes;
        BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(6, 2), castle.HitPoints);
        BinaryPrimitives.WriteInt32LittleEndian(span.Slice(8, 4), castle.GuardianFlags);
        return buffer;
    }
}
