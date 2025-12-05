using System.Collections.Generic;
using System.IO;
using LegacyDataExtractor.Models;

namespace LegacyDataExtractor.Parsing;

public sealed class ItemFileParser
{
    private const int ItemCount = 256;
    private const int NameLength = 31;

    public ItemFile Parse(string path)
    {
        using var stream = File.OpenRead(path);
        using var reader = new LegacyBinaryReader(stream);

        var names = ReadNames(reader);
        var descriptors = ReadDescriptors(reader);
        var checksum = reader.ReadInt32();

        return new ItemFile(names, descriptors, checksum);
    }

    private static IReadOnlyList<string> ReadNames(LegacyBinaryReader reader)
    {
        var names = new List<string>(ItemCount);
        for (var i = 0; i < ItemCount; i++)
        {
            names.Add(reader.ReadShortString(NameLength));
        }

        return names;
    }

    private static IReadOnlyList<ItemDescriptor> ReadDescriptors(LegacyBinaryReader reader)
    {
        var items = new List<ItemDescriptor>(ItemCount);
        for (var i = 0; i < ItemCount; i++)
        {
            var cost = reader.ReadUInt16();
            var damage1B = reader.ReadSByte();
            var damage1P = reader.ReadSByte();
            var damage2B = reader.ReadSByte();
            var damage2P = reader.ReadSByte();
            var forbiddenRaces = reader.ReadByte();
            var forbiddenClasses = reader.ReadByte();
            var defenseModifier = reader.ReadSByte();
            var weaponWeight = reader.ReadByte();
            var weaponType = reader.ReadByte();
            var rangeType = reader.ReadByte();
            var craftDiscipline = reader.ReadByte();
            var requiredTool = reader.ReadByte();
            var craftedQuantity = reader.ReadByte();
            var crafterLevel = reader.ReadByte();
            var requiredResources = reader.ReadBytesExact(3);
            var requiredAmounts = reader.ReadBytesExact(3);
            var repairType = reader.ReadByte();
            var minimumLevel = reader.ReadByte();
            var flags = reader.ReadInt32();
            var animationType = reader.ReadByte();
            var reserved1 = reader.ReadByte();
            var reserved2 = reader.ReadUInt16();

            items.Add(new ItemDescriptor(
                cost,
                damage1B,
                damage1P,
                damage2B,
                damage2P,
                forbiddenRaces,
                forbiddenClasses,
                defenseModifier,
                weaponWeight,
                weaponType,
                rangeType,
                craftDiscipline,
                requiredTool,
                craftedQuantity,
                crafterLevel,
                requiredResources,
                requiredAmounts,
                repairType,
                minimumLevel,
                flags,
                animationType,
                reserved1,
                reserved2
            ));
        }

        return items;
    }
}
