using System.Collections.Generic;

namespace Laa.Content.Core.Monsters;

public sealed record MonsterDocument(IReadOnlyList<MonsterDescriptor> Monsters);
