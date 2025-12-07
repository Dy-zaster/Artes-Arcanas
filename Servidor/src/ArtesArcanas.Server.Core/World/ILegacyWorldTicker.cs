using System.Threading;
using System.Threading.Tasks;

namespace ArtesArcanas.Server.Core.World;

public interface ILegacyWorldTicker
{
    ValueTask TickAsync(LegacyWorldState world, CancellationToken cancellationToken);
}
