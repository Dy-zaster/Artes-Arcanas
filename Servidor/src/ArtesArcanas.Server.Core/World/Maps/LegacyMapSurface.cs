namespace ArtesArcanas.Server.Core.World.Maps;

public sealed class LegacyMapSurface
{
    private readonly LegacyMapManager _maps;
    internal const LegacyTerrainFlags PlayerTerrainMask =
        LegacyTerrainFlags.Solid |
        LegacyTerrainFlags.Soft |
        LegacyTerrainFlags.Wilderness |
        LegacyTerrainFlags.Covered;

    public LegacyMapSurface(LegacyMapManager maps)
    {
        _maps = maps;
    }

    public bool TryGetTerrain(byte mapId, int x, int y, out LegacyTerrainFlags terrain) =>
        _maps.TryGetTerrain(mapId, x, y, out terrain);

    public bool IsWalkable(byte mapId, int x, int y) =>
        IsWalkable(mapId, x, y, PlayerTerrainMask);

    public bool IsWalkable(byte mapId, int x, int y, LegacyTerrainFlags allowedTerrain)
    {
        if (x < 0 || x >= LegacyMapGrid.ExpandedWidth ||
            y < 0 || y >= LegacyMapGrid.ExpandedHeight)
        {
            return false;
        }

        if (!_maps.TryGetTerrain(mapId, x, y, out var terrain))
        {
            return false;
        }

        return (terrain & allowedTerrain) != 0;
    }

    public bool TryGetDefinition(byte mapId, out LegacyMapDefinition? definition) =>
        _maps.TryGetDefinition(mapId, out definition);

    public bool TryGetSensor(byte mapId, byte x, byte y, out LegacyMapSensorDefinition? sensor) =>
        _maps.TryGetSensor(mapId, x, y, out sensor);

    public bool TryGetRespawn(byte mapId, out byte respawnMap, out byte respawnX, out byte respawnY)
    {
        respawnMap = 0;
        respawnX = 0;
        respawnY = 0;

        if (!_maps.TryGetDefinition(mapId, out var definition) || definition is null)
        {
            return false;
        }

        respawnMap = definition.RespawnMap;
        respawnX = definition.RespawnX;
        respawnY = definition.RespawnY;
        return true;
    }
}
