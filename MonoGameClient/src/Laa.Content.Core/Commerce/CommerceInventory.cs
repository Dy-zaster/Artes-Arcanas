using System.Collections.Generic;
using Laa.Content.Core.Inventory;

namespace Laa.Content.Core.Commerce;

public sealed record CommerceInventory(IReadOnlyList<ArtefactSlot> Items);
