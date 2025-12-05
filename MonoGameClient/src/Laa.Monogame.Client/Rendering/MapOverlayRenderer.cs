using System;
using Laa.Content.Core.Maps;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Laa.Monogame.Client.Rendering;

/// <summary>
/// Paints lightweight overlays (sensors, nests, merchants) to help visualize map metadata.
/// </summary>
public sealed class MapOverlayRenderer : IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly int _tileWidth;
    private readonly int _tileHeight;
    private Texture2D? _markerTexture;

    public MapOverlayRenderer(GraphicsDevice graphicsDevice, int tileWidth, int tileHeight)
    {
        _graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
        _tileWidth = tileWidth > 0 ? tileWidth : throw new ArgumentOutOfRangeException(nameof(tileWidth));
        _tileHeight = tileHeight > 0 ? tileHeight : throw new ArgumentOutOfRangeException(nameof(tileHeight));
    }

    public void LoadContent()
    {
        _markerTexture = new Texture2D(_graphicsDevice, 1, 1);
        _markerTexture.SetData(new[] { Color.White });
    }

    public void Draw(SpriteBatch spriteBatch, MapDocument? map, Camera2D camera, OverlayLayers layers)
    {
        if (spriteBatch is null) throw new ArgumentNullException(nameof(spriteBatch));
        if (camera is null) throw new ArgumentNullException(nameof(camera));
        if (_markerTexture is null || map is null || layers == OverlayLayers.None)
        {
            return;
        }

        spriteBatch.Begin(
            samplerState: SamplerState.PointClamp,
            transformMatrix: camera.GetViewMatrix(),
            blendState: BlendState.NonPremultiplied);

        if (layers.HasFlag(OverlayLayers.Sensors))
        {
            foreach (var sensor in map.Sensors)
            {
                DrawMarker(spriteBatch, sensor.X, sensor.Y, Color.Orange * 0.75f);
            }
        }

        if (layers.HasFlag(OverlayLayers.Nests))
        {
            foreach (var nest in map.Nests)
            {
                var intensity = MathHelper.Clamp(nest.Quantity / 10f, 0.25f, 1f);
                DrawMarker(spriteBatch, nest.X, nest.Y, Color.LimeGreen * intensity);
            }
        }

        if (layers.HasFlag(OverlayLayers.Merchants))
        {
            foreach (var merchant in map.Merchants)
            {
                DrawMarker(spriteBatch, merchant.X, merchant.Y, Color.MediumPurple);
            }
        }

        spriteBatch.End();
    }

    private void DrawMarker(SpriteBatch spriteBatch, byte tileX, byte tileY, Color color)
    {
        if (_markerTexture is null)
        {
            return;
        }

        var markerWidth = Math.Max(4, _tileWidth / 2);
        var markerHeight = Math.Max(4, _tileHeight / 2);
        var offsetX = (_tileWidth - markerWidth) / 2;
        var offsetY = (_tileHeight - markerHeight) / 2;
        var destination = new Rectangle(
            tileX * _tileWidth + offsetX,
            tileY * _tileHeight + offsetY,
            markerWidth,
            markerHeight);

        spriteBatch.Draw(_markerTexture, destination, color);
    }

    public void Dispose()
    {
        _markerTexture?.Dispose();
    }
}
