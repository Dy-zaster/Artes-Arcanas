using System.Collections.Generic;

namespace Laa.Content.Core.Animations;

public sealed record AnimationDocument(IReadOnlyList<AnimationEntry> Animations);

public sealed record AnimationEntry(
    string Key,
    AnimationKind Kind,
    IReadOnlyList<AnimationDirection> Directions,
    int StyleFlags
);

public sealed record AnimationDirection(
    short MaxWidth,
    byte OffsetX,
    byte OffsetY,
    IReadOnlyList<AnimationFrame> Frames
);

public sealed record AnimationFrame(
    short AccumulatedY,
    byte Width,
    byte CenterX,
    byte CenterY
);

public enum AnimationKind
{
    Unknown = 0,
    Monster = 1,
    Player = 2
}
