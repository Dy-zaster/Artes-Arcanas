using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Laa.Content.Core.Items;

namespace ArtesArcanas.Server.Core.Legacy;

public sealed class LegacyItemCatalog
{
    private readonly string[] _names;
    private readonly ItemDescriptor[] _descriptors;

    private LegacyItemCatalog(string[] names, ItemDescriptor[] descriptors, int checksum)
    {
        _names = names;
        _descriptors = descriptors;
        Checksum = checksum;
    }

    public int Checksum { get; }
    public int Count => Math.Min(_names.Length, _descriptors.Length);

    public static LegacyItemCatalog Load(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("No se encontró el catálogo de objetos legacy.", filePath);
        }

        using var stream = File.OpenRead(filePath);
        var document = JsonSerializer.Deserialize<ItemDocument>(stream, CreateOptions());
        if (document is null)
        {
            throw new InvalidOperationException($"No se pudo deserializar el catálogo de objetos desde {filePath}.");
        }

        if (document.Items.Count != document.Names.Count)
        {
            throw new InvalidOperationException($"Catálogo de objetos inconsistente ({document.Items.Count} descriptores vs {document.Names.Count} nombres).");
        }

        return new LegacyItemCatalog(document.Names.ToArray(), document.Items.ToArray(), document.Checksum);
    }

    public bool TryGetDefinition(int itemId, out LegacyItemDefinition definition)
    {
        definition = default;
        if ((uint)itemId >= (uint)_names.Length || (uint)itemId >= (uint)_descriptors.Length)
        {
            return false;
        }

        definition = new LegacyItemDefinition(itemId, _names[itemId] ?? string.Empty, _descriptors[itemId]);
        return true;
    }

    public string GetNameOrFallback(int itemId)
    {
        if (TryGetDefinition(itemId, out var definition) && !string.IsNullOrWhiteSpace(definition.Name))
        {
            return definition.Name;
        }

        return $"Objeto #{itemId}";
    }

    private static JsonSerializerOptions CreateOptions() =>
        new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
}

public readonly record struct LegacyItemDefinition(int Id, string Name, ItemDescriptor Descriptor);
