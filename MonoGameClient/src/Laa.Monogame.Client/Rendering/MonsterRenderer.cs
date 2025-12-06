using System;
using System.Collections.Generic;
using System.Linq;
using Laa.Monogame.Client.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Laa.Monogame.Client.Rendering;

public sealed class MonsterRenderer
{
    private const float DefaultFrameDuration = 0.12f;

    private readonly AnimationTextureProvider _animationProvider;
    private readonly List<MonsterSprite> _sprites = new();

    public MonsterRenderer(AnimationTextureProvider animationProvider)
    {
        _animationProvider = animationProvider ?? throw new ArgumentNullException(nameof(animationProvider));
    }

    public void SetMonsters(IEnumerable<MonsterEntity> entities)
    {
        _sprites.Clear();
        if (entities is null)
        {
            return;
        }

        foreach (var entity in entities)
        {
            var animationKey = ResolveAnimationKey(entity);
            if (string.IsNullOrWhiteSpace(animationKey))
            {
                continue;
            }

            if (!_animationProvider.TryGetAnimation(animationKey, out var animation))
            {
                continue;
            }

            if (animation.Directions.Count == 0)
            {
                continue;
            }

            var directionIndex = PickDirection(animation, entity.FacingSeed);
            var frameDuration = DefaultFrameDuration;
            var sprite = new MonsterSprite(entity, animation, directionIndex, frameDuration);
            _sprites.Add(sprite);
        }

        _sprites.Sort((a, b) => a.Anchor.Y.CompareTo(b.Anchor.Y));
    }

    public void Update(GameTime gameTime)
    {
        if (_sprites.Count == 0 || gameTime is null)
        {
            return;
        }

        var elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
        foreach (var sprite in _sprites)
        {
            sprite.Update(elapsed);
        }
    }

    public void Draw(SpriteBatch spriteBatch, Camera2D camera)
    {
        if (spriteBatch is null) throw new ArgumentNullException(nameof(spriteBatch));
        if (camera is null) throw new ArgumentNullException(nameof(camera));
        if (_sprites.Count == 0)
        {
            return;
        }

        spriteBatch.Begin(
            samplerState: SamplerState.PointClamp,
            blendState: BlendState.NonPremultiplied,
            transformMatrix: camera.GetViewMatrix());

        foreach (var sprite in _sprites)
        {
            if (!sprite.TryGetFrame(out var direction, out var frame))
            {
                continue;
            }

            var position = AnimationRenderHelper.CalculateDrawPosition(sprite.Anchor, direction, frame, sprite.Mirror);
            spriteBatch.Draw(
                sprite.Animation.Texture,
                position,
                frame.Source,
                Color.White,
                0f,
                Vector2.Zero,
                Vector2.One,
                sprite.Mirror ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
                0.65f);
        }

        spriteBatch.End();
    }

    private static string ResolveAnimationKey(MonsterEntity entity)
    {
        return entity.AnimationKey;
    }

    private static int PickDirection(AnimationTexture animation, int seed)
    {
        if (animation.Directions.Count == 0)
        {
            return 0;
        }

        var preferred = Math.Abs(seed) % animation.Directions.Count;
        if (HasUsableFrames(animation.Directions[preferred]))
        {
            return preferred;
        }

        for (var i = 0; i < animation.Directions.Count; i++)
        {
            if (HasUsableFrames(animation.Directions[i]))
            {
                return i;
            }
        }

        return preferred;
    }

    private static bool HasUsableFrames(AnimationDirectionSlice direction)
    {
        return direction.Frames.Any(frame => frame.Source != Rectangle.Empty);
    }

    private sealed class MonsterSprite
    {
        private static readonly int[] IdleSequence = { 4 };
        private static readonly int[] MoveSequence = { 0, 1, 2, 3 };
        private static readonly int[] AttackSequence = { 5, 6, 7 };
        private static readonly int[] DeathSequence = { 7 };

        private readonly float _frameDuration;
        private readonly bool _mirror;
        private readonly List<int> _allFrames;
        private readonly Dictionary<MonsterAction, List<int>> _actionFrames;
        private float _timer;
        private int _frameIndex;
        private MonsterAction _lastAction;

        public MonsterSprite(MonsterEntity entity, AnimationTexture animation, int directionIndex, float frameDuration)
        {
            Entity = entity;
            Animation = animation;
            DirectionIndex = directionIndex;
            _frameDuration = frameDuration;
            _mirror = (entity.FacingSeed & 1) != 0;
            var direction = animation.Directions[directionIndex];
            _allFrames = BuildFrames(direction, Enumerable.Range(0, direction.Frames.Count).ToArray());
            _actionFrames = new Dictionary<MonsterAction, List<int>>
            {
                [MonsterAction.Idle] = BuildFrames(direction, IdleSequence),
                [MonsterAction.Moving] = BuildFrames(direction, MoveSequence),
                [MonsterAction.Attack] = BuildFrames(direction, AttackSequence),
                [MonsterAction.Dead] = BuildFrames(direction, DeathSequence)
            };
            _lastAction = entity.Action;
        }

        public MonsterEntity Entity { get; }
        public AnimationTexture Animation { get; }
        public int DirectionIndex { get; }
        public Vector2 Anchor => Entity.Position;
        public bool Mirror => _mirror;

        public void Update(float elapsedSeconds)
        {
            if (_allFrames.Count <= 1)
            {
                return;
            }

            if (_lastAction != Entity.Action)
            {
                _lastAction = Entity.Action;
                _frameIndex = 0;
                _timer = 0f;
            }

            _timer += elapsedSeconds;
            while (_timer >= _frameDuration)
            {
                _timer -= _frameDuration;
                _frameIndex++;
            }
        }

        public bool TryGetFrame(out AnimationDirectionSlice direction, out AnimationFrameSlice frame)
        {
            direction = Animation.Directions[DirectionIndex];
            var frames = GetFramesForAction(Entity.Action);
            if (frames.Count == 0)
            {
                frame = default;
                return false;
            }

            var index = frames[Math.Abs(_frameIndex) % frames.Count];
            frame = direction.Frames[index];
            return frame.Source != Rectangle.Empty;
        }

        private List<int> GetFramesForAction(MonsterAction action)
        {
            if (_actionFrames.TryGetValue(action, out var frames) && frames.Count > 0)
            {
                return frames;
            }

            return _allFrames;
        }

        private static List<int> BuildFrames(AnimationDirectionSlice direction, int[] candidates)
        {
            var list = new List<int>(candidates.Length);
            foreach (var index in candidates)
            {
                if (index >= 0 && index < direction.Frames.Count)
                {
                    var frame = direction.Frames[index];
                    if (frame.Source != Rectangle.Empty)
                    {
                        list.Add(index);
                    }
                }
            }

            return list;
        }

        private static List<int> BuildFrames(AnimationDirectionSlice direction, IEnumerable<int> indices)
        {
            var list = new List<int>();
            foreach (var index in indices)
            {
                if (index >= 0 && index < direction.Frames.Count)
                {
                    var frame = direction.Frames[index];
                    if (frame.Source != Rectangle.Empty)
                    {
                        list.Add(index);
                    }
                }
            }
            if (list.Count == 0 && direction.Frames.Count > 0)
            {
                for (var i = 0; i < direction.Frames.Count; i++)
                {
                    if (direction.Frames[i].Source != Rectangle.Empty)
                    {
                        list.Add(i);
                    }
                }
            }
            return list;
        }
    }
}
