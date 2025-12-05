using Laa.Content.Core.Graphics;

namespace Laa.Content.Core.Repositories;

public interface ITerrainTileSetRepository
{
    TerrainTileSet GetTileSet(string mapId);
}
