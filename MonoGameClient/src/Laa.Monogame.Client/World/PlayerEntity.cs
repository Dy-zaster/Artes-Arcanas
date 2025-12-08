using Microsoft.Xna.Framework;

namespace Laa.Monogame.Client.World;

public sealed class PlayerEntity : IWorldEntity
{
    public PlayerEntity(int id, Vector2 position)
    {
        Id = id;
        Position = position;
        BaseAnchor = position;
    }

    public int Id { get; }

    public Vector2 Position { get; private set; }

    public Vector2 BaseAnchor { get; private set; }

    public void SetPosition(Vector2 position)
    {
        Position = position;
    }
}
