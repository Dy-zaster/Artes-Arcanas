using System.Collections.Generic;
using System.IO;
using LegacyDataExtractor.Models;

namespace LegacyDataExtractor.Parsing;

public sealed class CommerceFileParser
{
    private const int CommerceCount = 32; // MAX_TIPOS_COMERCIO + 1
    private const int InventorySlots = 30; // MAX_ARTEFACTOS + 1

    public CommerceFile Parse(string path)
    {
        using var stream = File.OpenRead(path);
        using var reader = new LegacyBinaryReader(stream);

        var version = reader.ReadInt32();
        var inventories = new List<CommerceInventory>(CommerceCount);
        for (var commerce = 0; commerce < CommerceCount; commerce++)
        {
            var items = new List<ArtefactSlot>(InventorySlots);
            for (var slot = 0; slot < InventorySlots; slot++)
            {
                var id = reader.ReadByte();
                var modifier = reader.ReadByte();
                items.Add(new ArtefactSlot(id, modifier));
            }

            inventories.Add(new CommerceInventory(items));
        }

        return new CommerceFile(version, inventories);
    }
}
