using System;
using System.Collections.Generic;
using Laa.Content.Core.Maps;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Laa.Monogame.Client.Rendering;

public sealed class StaticGraphicRenderer : IDisposable
{
    private const int DescriptorCodeMask = 0x03FF;
    private const int MirrorFlagMask = 0x0400;

    private readonly GraphicTextureProvider _textureProvider;
    private readonly Texture2D _fallbackTexture;
    private readonly int _tileWidth;
    private readonly int _tileHeight;

    public StaticGraphicRenderer(GraphicsDevice graphicsDevice, GraphicTextureProvider textureProvider, int tileWidth, int tileHeight)
    {
        if (graphicsDevice is null) throw new ArgumentNullException(nameof(graphicsDevice));
        _textureProvider = textureProvider ?? throw new ArgumentNullException(nameof(textureProvider));
        _tileWidth = tileWidth > 0 ? tileWidth : throw new ArgumentOutOfRangeException(nameof(tileWidth));
        _tileHeight = tileHeight > 0 ? tileHeight : throw new ArgumentOutOfRangeException(nameof(tileHeight));
        _fallbackTexture = new Texture2D(graphicsDevice, 1, 1);
        _fallbackTexture.SetData(new[] { Color.White });
    }

    public void Draw(SpriteBatch spriteBatch, IReadOnlyList<StaticGraphic> graphics, Camera2D camera)
    {
        if (spriteBatch is null) throw new ArgumentNullException(nameof(spriteBatch));
        if (camera is null) throw new ArgumentNullException(nameof(camera));
        if (graphics.Count == 0)
        {
            return;
        }

        spriteBatch.Begin(
            samplerState: SamplerState.PointClamp,
            blendState: BlendState.NonPremultiplied,
            transformMatrix: camera.GetViewMatrix());

        foreach (var graphic in graphics)
        {
            var descriptorIndex = graphic.CodeFlags & DescriptorCodeMask;
            if (!_textureProvider.TryGetTexture(descriptorIndex, out var entry))
            {
                DrawPlaceholder(spriteBatch, graphic);
                continue;
            }

            var effects = (graphic.CodeFlags & MirrorFlagMask) != 0
                ? SpriteEffects.FlipHorizontally
                : SpriteEffects.None;

            var baseX = graphic.X * _tileWidth;
            var baseY = graphic.Y * _tileHeight - entry.AlignY * _tileHeight;
            var offsetX = effects == SpriteEffects.FlipHorizontally ? entry.ReflectedOffsetX : entry.OffsetX;
            var drawPosition = new Vector2(baseX + offsetX, baseY + entry.OffsetY);
            var layerDepth = MathHelper.Clamp(graphic.SubLayer / 32f, 0f, 1f);

            spriteBatch.Draw(
                entry.Texture,
                drawPosition,
                entry.SourceRectangle,
                Color.White,
                0f,
                Vector2.Zero,
                Vector2.One,
                effects,
                layerDepth);
        }

        spriteBatch.End();
    }

    public void Dispose()
    {
        _textureProvider.Dispose();
        _fallbackTexture.Dispose();
    }

    private void DrawPlaceholder(SpriteBatch spriteBatch, StaticGraphic graphic)
    {
        var destination = new Rectangle(
            graphic.X * _tileWidth,
            graphic.Y * _tileHeight,
            _tileWidth,
            _tileHeight);
        spriteBatch.Draw(_fallbackTexture, destination, Color.DimGray * 0.75f);
    }
}
