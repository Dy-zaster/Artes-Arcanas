namespace Laa.Monogame.Client.UI;

public sealed class HudMessageLog
{
    private const int MaxMessages = 512;
    private readonly List<string> _messages = new();
    private int _viewIndex;

    public void Add(string message, int visibleCount)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        var lines = message
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => line.Length > 0);

        foreach (var line in lines)
        {
            _messages.Add(line);
        }

        if (_messages.Count > MaxMessages)
        {
            var overflow = _messages.Count - MaxMessages;
            _messages.RemoveRange(0, overflow);
            _viewIndex = Math.Max(0, _viewIndex - overflow);
        }

        StickToBottom(visibleCount);
    }

    public void Scroll(int delta, int visibleCount)
    {
        if (_messages.Count <= visibleCount)
        {
            _viewIndex = 0;
            return;
        }

        var maxIndex = Math.Max(0, _messages.Count - visibleCount);
        _viewIndex = Math.Clamp(_viewIndex + delta, 0, maxIndex);
    }

    public void StickToBottom(int visibleCount)
    {
        if (_messages.Count <= visibleCount)
        {
            _viewIndex = 0;
            return;
        }

        _viewIndex = Math.Max(0, _messages.Count - visibleCount);
    }

    public IReadOnlyList<string> GetVisibleLines(int visibleCount)
    {
        if (visibleCount <= 0 || _messages.Count == 0)
        {
            return Array.Empty<string>();
        }

        var start = Math.Clamp(_viewIndex, 0, Math.Max(0, _messages.Count - visibleCount));
        var lines = new List<string>(visibleCount);
        for (var i = 0; i < visibleCount; i++)
        {
            var index = start + i;
            if (index >= 0 && index < _messages.Count)
            {
                lines.Add(_messages[index]);
            }
            else
            {
                lines.Add(string.Empty);
            }
        }

        return lines;
    }
}
