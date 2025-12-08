using System.Text;

namespace Laa.Protocol;

public ref struct SpanReader
{
    private ReadOnlySpan<byte> _span;
    private int _offset;

    public SpanReader(ReadOnlySpan<byte> span)
    {
        _span = span;
        _offset = 0;
    }

    public bool TryReadByte(out byte value)
    {
        if (_offset >= _span.Length)
        {
            value = 0;
            return false;
        }

        value = _span[_offset++];
        return true;
    }

    public bool TryReadUInt16(out ushort value)
    {
        if (_offset + 2 > _span.Length)
        {
            value = 0;
            return false;
        }

        value = BitConverter.ToUInt16(_span.Slice(_offset, 2));
        _offset += 2;
        return true;
    }

    public bool TryReadUInt(out uint value)
    {
        if (_offset + 4 > _span.Length)
        {
            value = 0;
            return false;
        }

        value = BitConverter.ToUInt32(_span.Slice(_offset, 4));
        _offset += 4;
        return true;
    }

    public bool TryReadString(out string value)
    {
        value = string.Empty;
        if (!TryReadByte(out var len))
        {
            return false;
        }

        if (_offset + len > _span.Length)
        {
            return false;
        }

        value = Encoding.UTF8.GetString(_span.Slice(_offset, len));
        _offset += len;
        return true;
    }
}
