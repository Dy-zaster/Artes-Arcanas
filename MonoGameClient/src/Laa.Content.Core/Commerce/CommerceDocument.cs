using System.Collections.Generic;

namespace Laa.Content.Core.Commerce;

public sealed record CommerceDocument(int Version, IReadOnlyList<CommerceInventory> Inventories);
