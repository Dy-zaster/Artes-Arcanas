using Laa.Content.Core.Animations;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Laa.Monogame.Client.Rendering;

/// <summary>
/// Simple debug helper that cycles through legacy animations and renders them in world space.
/// </summary>
public sealed class AnimationPreviewPlayer
{
    private const double DefaultFrameDurationSeconds = 0.12;

    private readonly AnimationTextureProvider _textureProvider;
    private readonly List<string> _keys;

    private AnimationTexture? _current;
    private string? _currentKey;
    private int _currentIndex = -1;
    private int _currentDirection;
    private int _currentFrame;
    private double _frameTimer;
    private bool _mirror;

    public AnimationPreviewPlayer(AnimationTextureProvider textureProvider, IEnumerable<string> animationKeys)
    {
        _textureProvider = textureProvider ?? throw new ArgumentNullException(nameof(textureProvider));
        if (animationKeys is null) throw new ArgumentNullException(nameof(animationKeys));

        _keys = animationKeys
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(k => k, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (_keys.Count > 0)
        {
            SetAnimationIndex(0);
        }
    }

    public string? CurrentKey => _currentKey;

    public AnimationKind? CurrentKind => _current?.Kind;

    public int DirectionIndex => _currentDirection;

    public bool Mirror => _mirror;

    public bool HasAnimation => _current is not null && _current.Directions.Count > 0;

    public void StepAnimation(int delta)
    {
        if (_keys.Count == 0)
        {
            return;
        }

        var nextIndex = (_currentIndex + delta) % _keys.Count;
        if (nextIndex < 0)
        {
            nextIndex += _keys.Count;
        }

        SetAnimationIndex(nextIndex);
    }

    public void StepDirection(int delta)
    {
        if (_current is null || _current.Directions.Count == 0)
        {
            return;
        }

        _currentDirection = (_currentDirection + delta) % _current.Directions.Count;
        if (_currentDirection < 0)
        {
            _currentDirection += _current.Directions.Count;
        }

        _currentFrame = 0;
        _frameTimer = 0;
    }

    public void ToggleMirror()
    {
        _mirror = !_mirror;
    }

    public void Update(GameTime gameTime)
    {
        if (_current is null)
        {
            return;
        }

        if (_currentDirection >= _current.Directions.Count)
        {
            _currentDirection = 0;
        }

        var direction = _current.Directions[_currentDirection];
        if (direction is null || direction.Frames.Count == 0)
        {
            return;
        }

        var elapsed = gameTime?.ElapsedGameTime.TotalSeconds ?? 0d;
        _frameTimer += elapsed;
        var frameDuration = direction.Frames.Count > 4
            ? DefaultFrameDurationSeconds
            : DefaultFrameDurationSeconds * 1.5;

        while (_frameTimer >= frameDuration)
        {
            _frameTimer -= frameDuration;
            _currentFrame = (_currentFrame + 1) % direction.Frames.Count;
        }
    }

    public void Draw(SpriteBatch spriteBatch, Camera2D camera, Vector2 anchor)
    {
        if (spriteBatch is null) throw new ArgumentNullException(nameof(spriteBatch));
        if (camera is null) throw new ArgumentNullException(nameof(camera));
        if (_current is null)
        {
            return;
        }

        var direction = _current.Directions.Count > 0
            ? _current.Directions[_currentDirection]
            : null;

        if (direction is null || direction.Frames.Count == 0)
        {
            return;
        }

        var frame = direction.Frames[_currentFrame];
        if (frame.Source == Rectangle.Empty)
        {
            return;
        }

        spriteBatch.Begin(
            samplerState: SamplerState.PointClamp,
            blendState: BlendState.NonPremultiplied,
            transformMatrix: camera.GetViewMatrix());

        var position = AnimationRenderHelper.CalculateDrawPosition(anchor, direction, frame, _mirror);
        spriteBatch.Draw(
            _current.Texture,
            position,
            frame.Source,
            Color.White,
            0f,
            Vector2.Zero,
            Vector2.One,
            _mirror ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
            0.5f);

        spriteBatch.End();
    }

    public bool TrySetAnimation(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        if (!_textureProvider.TryGetAnimation(key, out var animation))
        {
            return false;
        }

        var index = _keys.FindIndex(k => string.Equals(k, key, StringComparison.OrdinalIgnoreCase));
        ApplyAnimation(animation, key, index);
        return true;
    }

    private void SetAnimationIndex(int index)
    {
        if (_keys.Count == 0)
        {
            ApplyAnimation(null, null, -1);
            return;
        }

        var clamped = Math.Clamp(index, 0, _keys.Count - 1);
        var key = _keys[clamped];
        if (_textureProvider.TryGetAnimation(key, out var animation))
        {
            ApplyAnimation(animation, key, clamped);
        }
        else
        {
            ApplyAnimation(null, key, clamped);
        }
    }

    private void ApplyAnimation(AnimationTexture? animation, string? key, int index)
    {
        _current = animation;
        _currentKey = key;
        _currentIndex = index;
        _currentDirection = 0;
        _currentFrame = 0;
        _frameTimer = 0;
        _mirror = false;
    }

}
