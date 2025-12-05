using System.Collections.Generic;
using System.IO;
using LegacyDataExtractor.Models;

namespace LegacyDataExtractor.Parsing;

public sealed class AttackMappingParser
{
    public AttackMappingFile Parse(string path)
    {
        using var stream = File.OpenRead(path);
        using var reader = new LegacyBinaryReader(stream);

        var entries = new List<AttackMappingEntry>();
        while (reader.BaseStream.Position < reader.BaseStream.Length)
        {
            var animationId = reader.ReadByte();
            var flags = reader.ReadByte();
            var reserved = reader.ReadUInt16();
            entries.Add(new AttackMappingEntry(animationId, flags, reserved));
        }

        return new AttackMappingFile(entries);
    }
}
