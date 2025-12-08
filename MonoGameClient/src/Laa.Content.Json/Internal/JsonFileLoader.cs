using System.Text.Json;

namespace Laa.Content.Json.Internal;

internal static class JsonFileLoader
{
    public static T Load<T>(string path, JsonSerializerOptions options)
    {
        using var stream = File.OpenRead(path);
        return JsonSerializer.Deserialize<T>(stream, options)
            ?? throw new InvalidDataException($"File '{path}' could not be deserialized to {typeof(T).Name}.");
    }
}
