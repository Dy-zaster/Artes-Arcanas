using System;
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
    }

    public int Id { get; }

    public MonsterDescriptor Descriptor { get; }

    public Vector2 BaseAnchor { get; }

    public Vector2 Position { get; private set; }

    public string AnimationKey { get; }

    public MonsterAction Action { get; private set; }

    public int FacingSeed { get; }

    public void SetAction(MonsterAction action)
    {
        Action = action;
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
