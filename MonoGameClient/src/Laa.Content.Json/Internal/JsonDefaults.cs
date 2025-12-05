using System.Text.Json;

namespace Laa.Content.Json.Internal;

internal static class JsonDefaults
{
    public static JsonSerializerOptions CreateOptions() => new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };
}
