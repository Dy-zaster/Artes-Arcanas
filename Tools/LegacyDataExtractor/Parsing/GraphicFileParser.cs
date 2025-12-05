using System.Collections.Generic;
using System.IO;
using LegacyDataExtractor.Models;

namespace LegacyDataExtractor.Parsing;

public sealed class GraphicFileParser
{
    private const int GraphicCount = 512; // MAX_OBJETOS_GRAFICOS + 1
    private const int NameLength = 23;
    private const int MaskLength = 8;

    public GraphicFile Parse(string path)
    {
        using var stream = File.OpenRead(path);
        using var reader = new LegacyBinaryReader(stream);

        var names = new List<string>(GraphicCount);
        for (var i = 0; i < GraphicCount; i++)
        {
            names.Add(reader.ReadShortString(NameLength));
        }

        var descriptors = new List<GraphicDescriptor>(GraphicCount);
        for (var i = 0; i < GraphicCount; i++)
        {
            var posX = reader.ReadInt16();
            var posY = reader.ReadInt16();
            var occupied = reader.ReadBytesExact(MaskLength);
            var hidden = reader.ReadBytesExact(MaskLength);
            var alignY = reader.ReadByte();
            var unused0 = reader.ReadByte();
            var type = reader.ReadByte();
            var subLayer = reader.ReadByte();
            var reserved = reader.ReadUInt16();
            var resourceEffect = reader.ReadByte();
            var flags = reader.ReadByte();
            var reflectedPosX = reader.ReadInt16();

            descriptors.Add(new GraphicDescriptor(
                posX,
                posY,
                Array.AsReadOnly(occupied),
                Array.AsReadOnly(hidden),
                alignY,
                unused0,
                type,
                subLayer,
                reserved,
                resourceEffect,
                flags,
                reflectedPosX
            ));
        }

        var checksum = reader.ReadInt32();
        return new GraphicFile(names, descriptors, checksum);
    }
}
