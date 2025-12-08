using Laa.Content.Core.Monsters;
using Microsoft.Xna.Framework;

namespace Laa.Monogame.Client.World;

public enum MonsterAction
{
    Idle = 0,
    Attack = 1,
    Moving = 2,
    Dead = 3
}

public sealed class MonsterEntity : IWorldEntity
{
    public MonsterEntity(int id, MonsterDescriptor descriptor, Vector2 anchor, string animationKey)
    {
        Id = id;
        Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
        BaseAnchor = anchor;
        Position = anchor;
        AnimationKey = animationKey ?? throw new ArgumentNullException(nameof(animationKey));
        Action = MonsterAction.Idle;
        FacingSeed = id;
        Direction = 0;
        Mirror = false;
        Health = descriptor.AverageHp;
        MaxHealth = descriptor.AverageHp;
    }

    public int Id { get; }

    public MonsterDescriptor Descriptor { get; }

    public Vector2 BaseAnchor { get; }

    public Vector2 Position { get; private set; }

    public string AnimationKey { get; }

    public MonsterAction Action { get; private set; }

    public int FacingSeed { get; }

    public byte Direction { get; private set; }

    public bool Mirror { get; private set; }

    public ushort Health { get; private set; }

    public ushort MaxHealth { get; private set; }

    public void SetAction(MonsterAction action)
    {
        Action = action;
    }

    public void SetState(byte direction, bool mirror, MonsterAction action, ushort health, ushort maxHealth)
    {
        Direction = direction;
        Mirror = mirror;
        Action = action;
        Health = health;
        MaxHealth = maxHealth == 0 ? (ushort)1 : maxHealth;
    }

    public void SetPosition(Vector2 position)
    {
        Position = position;
    }

    public void ResetPosition()
    {
        Position = BaseAnchor;
    }
}
