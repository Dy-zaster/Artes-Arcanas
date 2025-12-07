using System.Collections.Generic;
using System.Linq;

namespace ArtesArcanas.Server.Core.World.Maps;

public sealed class LegacyMapManager
{
    private readonly IReadOnlyDictionary<byte, LegacyMapDefinition> _definitions;

    public LegacyMapManager(IReadOnlyDictionary<byte, LegacyMapDefinition> definitions)
    {
        _definitions = definitions;
    }

    public int Count => _definitions.Count;

    public bool TryGetDefinition(byte mapId, out LegacyMapDefinition? definition) =>
        _definitions.TryGetValue(mapId, out definition);

    public IReadOnlyCollection<LegacyMapDefinition> GetAll() => _definitions.Values.ToArray();

    public bool TryGetSensor(byte mapId, byte x, byte y, out LegacyMapSensorDefinition? sensor)
    {
        sensor = null;
        if (!_definitions.TryGetValue(mapId, out var definition) || definition is null)
        {
            return false;
        }

        return definition.TryGetSensor(x, y, out sensor);
    }

    public bool TryGetTerrain(byte mapId, int x, int y, out LegacyTerrainFlags terrain)
    {
        if (!_definitions.TryGetValue(mapId, out var definition))
        {
            terrain = LegacyTerrainFlags.None;
            return false;
        }

        terrain = definition.Grid.GetTerrain(x, y);
        return true;
    }
}
