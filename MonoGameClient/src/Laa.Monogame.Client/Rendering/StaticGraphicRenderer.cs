using Laa.Content.Core.Maps;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Laa.Monogame.Client.Rendering;

public sealed class StaticGraphicRenderer : IDisposable
{
    private const int DescriptorCodeMask = 0x03FF;
    private const int OccupiedMaskHalfWidth = 4;

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

    public void Draw(SpriteBatch spriteBatch, IReadOnlyList<StaticGraphic> graphics, Camera2D camera, int mapHeightTiles)
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

            var effects = (graphic.Flags & StaticGraphicFlags.Mirror) != 0
                ? SpriteEffects.FlipHorizontally
                : SpriteEffects.None;

            var baseX = (graphic.X - OccupiedMaskHalfWidth) * _tileWidth + (_tileWidth / 2); // slight right shift to align with terrain
            var baseY = graphic.Y * _tileHeight - entry.AlignY * _tileHeight + (_tileHeight / 2); // small down shift to match legacy anchoring
            var offsetX = effects == SpriteEffects.FlipHorizontally ? entry.ReflectedOffsetX : entry.OffsetX;
            var drawPosition = new Vector2(baseX + offsetX, baseY + entry.OffsetY);
            var layerDepth = ComputeDepth(graphic, mapHeightTiles);

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
            (graphic.X - OccupiedMaskHalfWidth) * _tileWidth,
            graphic.Y * _tileHeight,
            _tileWidth,
            _tileHeight);
        spriteBatch.Draw(_fallbackTexture, destination, Color.DimGray * 0.75f);
    }

    private static float ComputeDepth(StaticGraphic graphic, int mapHeightTiles)
    {
        if (mapHeightTiles <= 0) return 0.4f;
        var yNorm = Math.Clamp(graphic.Y / (float)mapHeightTiles, 0f, 1f);
        var subLayerNorm = graphic.SubLayer / 32f;
        return MathHelper.Clamp(0.05f + yNorm * 0.7f + subLayerNorm * 0.01f, 0f, 0.9f);
    }
}
