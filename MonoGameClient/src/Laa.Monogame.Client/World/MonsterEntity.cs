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

public sealed class MonsterEntity
{
    public MonsterEntity(int id, MonsterDescriptor descriptor, Vector2 anchor, string animationKey)
    {
        Id = id;
        Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
        Anchor = anchor;
        AnimationKey = animationKey ?? throw new ArgumentNullException(nameof(animationKey));
        Action = MonsterAction.Idle;
        FacingSeed = id;
    }

    public int Id { get; }

    public MonsterDescriptor Descriptor { get; }

    public Vector2 Anchor { get; }

    public string AnimationKey { get; }

    public MonsterAction Action { get; private set; }

    public int FacingSeed { get; }

    public void SetAction(MonsterAction action)
    {
        Action = action;
    }
}
