using System.Collections.Generic;
using System.IO;
using LegacyDataExtractor.Models;

namespace LegacyDataExtractor.Parsing;

public sealed class AnimationMappingParser
{
    public AnimationMappingFile Parse(string path)
    {
        using var stream = File.OpenRead(path);
        using var reader = new LegacyBinaryReader(stream);

        var ids = new List<byte>();
        while (reader.BaseStream.Position < reader.BaseStream.Length)
        {
            ids.Add(reader.ReadByte());
        }

        return new AnimationMappingFile(ids);
    }
}
