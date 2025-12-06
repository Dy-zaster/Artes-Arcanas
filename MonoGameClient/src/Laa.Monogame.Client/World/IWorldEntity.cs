using Microsoft.Xna.Framework;

namespace Laa.Monogame.Client.World;

public interface IWorldEntity
{
    int Id { get; }
    Vector2 Position { get; }
    Vector2 BaseAnchor { get; }
}
