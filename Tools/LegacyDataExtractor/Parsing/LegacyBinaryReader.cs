using System.Text;

namespace LegacyDataExtractor.Parsing;

internal sealed class LegacyBinaryReader : BinaryReader
{
    private static readonly Encoding Latin1 = Encoding.GetEncoding(1252);

    public LegacyBinaryReader(Stream input, bool leaveOpen = false)
        : base(input, Latin1, leaveOpen)
    {
    }

    public string ReadShortString(int maxLength)
    {
        var declaredLength = ReadByte();
        var buffer = ReadBytes(maxLength);
        var actualLength = Math.Min(declaredLength, buffer.Length);
        return actualLength == 0
            ? string.Empty
            : Latin1.GetString(buffer, 0, actualLength);
    }

    public byte[] ReadBytesExact(int count)
    {
        var buffer = ReadBytes(count);
        if (buffer.Length != count)
        {
            throw new EndOfStreamException($"Expected {count} bytes but only received {buffer.Length}.");
        }

        return buffer;
    }
}
