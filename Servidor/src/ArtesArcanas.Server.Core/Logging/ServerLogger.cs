using ArtesArcanas.Server.Core.Configuration;

namespace ArtesArcanas.Server.Core.Logging;

public sealed class ServerLogger : IServerLogger, IDisposable
{
    private readonly object _sync = new();
    private readonly bool _timestamp;
    private readonly bool _writeFile;
    private readonly StreamWriter? _writer;

    public ServerLogger(ServerOptions options, ServerPaths paths)
    {
        _timestamp = options.TimestampLogEntries;
        _writeFile = options.KeepLogFile;

        if (_writeFile)
        {
            _writer = new StreamWriter(new FileStream(paths.LogFile, FileMode.Append, FileAccess.Write, FileShare.Read))
            {
                AutoFlush = true
            };
        }
    }

    public void Info(string message) => Write("INFO", message, ConsoleColor.Gray);
    public void Warning(string message) => Write("WARN", message, ConsoleColor.Yellow);

    public void Error(string message, Exception? exception = null)
    {
        var fullMessage = exception is null ? message : $"{message} :: {exception}";
        Write("ERR ", fullMessage, ConsoleColor.Red);
    }

    private void Write(string level, string message, ConsoleColor color)
    {
        var line = FormatLine(level, message);

        lock (_sync)
        {
            var original = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(line);
            Console.ForegroundColor = original;

            _writer?.WriteLine(line);
        }
    }

    private string FormatLine(string level, string message)
    {
        if (_timestamp)
        {
            return $"{DateTime.Now:yy.MM.dd HH:mm:ss} [{level}] {message}";
        }

        return $"[{level}] {message}";
    }

    public void Dispose()
    {
        _writer?.Dispose();
    }
}
