namespace Laa.Content.Json.Internal;

using System.IO;

internal static class JsonValidation
{
    public static void Ensure(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidDataException(message);
        }
    }
}
