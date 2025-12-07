namespace ArtesArcanas.Server.Core.Legacy;

public sealed record LegacyMerchantInflation(byte MapId, byte MerchantIndex, byte[] Inflations);

public sealed class LegacyPriceStorage
{
    private readonly Dictionary<byte, IReadOnlyList<LegacyMerchantInflation>> _inflations = new();

    public IReadOnlyDictionary<byte, IReadOnlyList<LegacyMerchantInflation>> Inflations => _inflations;

    public static LegacyPriceStorage Load(string path)
    {
        var storage = new LegacyPriceStorage();
        if (!File.Exists(path))
        {
            return storage;
        }

        using var stream = File.OpenRead(path);
        using var reader = new BinaryReader(stream, LegacyConstants.LegacyEncoding, leaveOpen: false);

        var mapId = 0;
        while (stream.Position < stream.Length && mapId <= LegacyConstants.MaxMaps)
        {
            int merchantsDeclared;
            try
            {
                merchantsDeclared = reader.ReadInt32();
            }
            catch (EndOfStreamException)
            {
                break;
            }

            var merchants = Math.Clamp(merchantsDeclared, 0, LegacyConstants.MaxMerchants + 1);
            var list = new List<LegacyMerchantInflation>(merchants);

            var index = 0;
            for (; index < merchants; index++)
            {
                var buffer = reader.ReadBytes(LegacyConstants.MerchantInflationSize);
                if (buffer.Length != LegacyConstants.MerchantInflationSize)
                {
                    break;
                }

                list.Add(new LegacyMerchantInflation((byte)mapId, (byte)index, buffer));
            }

            if (list.Count > 0)
            {
                storage._inflations[(byte)mapId] = list;
            }

            if (index != merchants)
            {
                break;
            }

            mapId++;
        }

        return storage;
    }
}
