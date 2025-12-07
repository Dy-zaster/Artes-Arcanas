namespace ArtesArcanas.Server.Core.Logging;

public interface IServerLogger
{
    void Info(string message);
    void Warning(string message);
    void Error(string message, Exception? exception = null);
}
