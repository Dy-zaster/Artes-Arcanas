using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Laa.Monogame.Client.Rendering;

internal sealed class WorldEffectSystem : IDisposable
{
    private struct WorldEffect
    {
        public WorldEffect(Vector2 position, Color color, float radius, float lifetime)
        {
            Position = position;
            Color = color;
            Radius = radius;
            Lifetime = Math.Max(0.05f, lifetime);
            Age = 0f;
        }

        public Vector2 Position;

        public Color Color;

        public float Radius;

        public float Lifetime;

        public float Age;
    }

    private readonly List<WorldEffect> _effects = new();
    private readonly Texture2D _circleTexture;
    private readonly Vector2 _circleOrigin;

    public WorldEffectSystem(GraphicsDevice graphicsDevice)
    {
        if (graphicsDevice is null)
        {
            throw new ArgumentNullException(nameof(graphicsDevice));
        }

        _circleTexture = CreateCircleTexture(graphicsDevice, 48);
        _circleOrigin = new Vector2(_circleTexture.Width / 2f, _circleTexture.Height / 2f);
    }

    public void AddEffect(Vector2 worldPosition, Color color, float radius, float lifetime)
    {
        _effects.Add(new WorldEffect(worldPosition, color, radius, lifetime));
    }

    public void Update(GameTime gameTime)
    {
        if (_effects.Count == 0)
        {
            return;
        }

        var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        for (var i = _effects.Count - 1; i >= 0; i--)
        {
            var effect = _effects[i];
            effect.Age += dt;
            if (effect.Age >= effect.Lifetime)
            {
                _effects.RemoveAt(i);
            }
            else
            {
                _effects[i] = effect;
            }
        }
    }

    public void Draw(SpriteBatch spriteBatch, Camera2D? camera)
    {
        if (spriteBatch is null || camera is null || _effects.Count == 0)
        {
            return;
        }

        spriteBatch.Begin(
            blendState: BlendState.Additive,
            samplerState: SamplerState.LinearClamp,
            transformMatrix: camera.GetViewMatrix());

        foreach (ref readonly var effect in CollectionsMarshal.AsSpan(_effects))
        {
            var normalizedAge = MathHelper.Clamp(effect.Age / effect.Lifetime, 0f, 1f);
            var scale = Math.Max(0.1f, (effect.Radius * 2f) / _circleTexture.Width);
            var color = effect.Color * (1f - normalizedAge);
            spriteBatch.Draw(
                _circleTexture,
                effect.Position,
                null,
                color,
                0f,
                _circleOrigin,
                scale,
                SpriteEffects.None,
                0f);
        }

        spriteBatch.End();
    }

    public void Dispose()
    {
        _circleTexture.Dispose();
    }

    private static Texture2D CreateCircleTexture(GraphicsDevice device, int diameter)
    {
        var radius = diameter / 2f;
        var texture = new Texture2D(device, diameter, diameter);
        var data = new Color[diameter * diameter];

        for (var y = 0; y < diameter; y++)
        {
            for (var x = 0; x < diameter; x++)
            {
                var dx = x - radius;
                var dy = y - radius;
                var dist = MathF.Sqrt(dx * dx + dy * dy);
                var alpha = MathHelper.Clamp(1f - dist / radius, 0f, 1f);
                data[y * diameter + x] = new Color(1f, 1f, 1f, alpha * alpha);
            }
        }

        texture.SetData(data);
        return texture;
    }
}
