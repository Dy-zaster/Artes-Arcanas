using Laa.Content.Core.Maps;

namespace Laa.Content.Core.Repositories;

public interface IMapRepository
{
    IReadOnlyList<string> ListMaps();
    MapDocument GetMap(string mapId);
}
