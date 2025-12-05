using System.Collections.Generic;
using System.IO;
using LegacyDataExtractor.Models;

namespace LegacyDataExtractor.Parsing;

public sealed class SpellFileParser
{
    private const int SpellCount = 32;
    private const int NameLength = 23;

    public SpellFile Parse(string path)
    {
        using var stream = File.OpenRead(path);
        using var reader = new LegacyBinaryReader(stream);

        var names = new List<string>(SpellCount);
        for (var i = 0; i < SpellCount; i++)
        {
            names.Add(reader.ReadShortString(NameLength));
        }

        var spells = new List<SpellDescriptor>(SpellCount);
        for (var i = 0; i < SpellCount; i++)
        {
            var cost = reader.ReadUInt16();
            var flags = reader.ReadByte();
            var type = reader.ReadByte();
            var reqInt = reader.ReadByte();
            var reqWis = reader.ReadByte();
            var reqMana = reader.ReadByte();
            var baseDamage = reader.ReadByte();
            var bonusDamage = reader.ReadByte();
            var damageType = reader.ReadByte();
            var animationId = reader.ReadByte();
            var scrollIcon = reader.ReadByte();
            var reqLevel = reader.ReadByte();
            var school = reader.ReadByte();
            var reserved = reader.ReadUInt16();

            spells.Add(new SpellDescriptor(
                cost,
                flags,
                type,
                reqInt,
                reqWis,
                reqMana,
                baseDamage,
                bonusDamage,
                damageType,
                animationId,
                scrollIcon,
                reqLevel,
                school,
                reserved
            ));
        }

        var checksum = reader.ReadInt32();
        return new SpellFile(names, spells, checksum);
    }
}
