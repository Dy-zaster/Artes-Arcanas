using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ArtesArcanas.Server.Core.World;

public interface ILegacyWorldActionHandler
{
    ValueTask HandleAsync(LegacyPlayerContext player, IReadOnlyList<LegacyPlayerAction> actions, CancellationToken cancellationToken);
}
