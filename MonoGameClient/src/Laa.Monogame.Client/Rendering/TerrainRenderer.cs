using System;
using Laa.Content.Core.Maps;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Laa.Monogame.Client.Rendering;

/// <summary>
/// Simple SpriteBatch-based renderer that draws the terrain grid using placeholder tiles.
/// </summary>
public sealed class TerrainRenderer : IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly int _tileSize;
    private Texture2D? _tileTexture;

    public TerrainRenderer(GraphicsDevice graphicsDevice, int tileSize)
    {
        _graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
        _tileSize = tileSize > 0 ? tileSize : throw new ArgumentOutOfRangeException(nameof(tileSize));
    }

    public int TileSize => _tileSize;

    public void LoadContent()
    {
        _tileTexture = new Texture2D(_graphicsDevice, 1, 1);
        _tileTexture.SetData(new[] { Color.White });
    }

    public void Draw(SpriteBatch spriteBatch, MapDocument? map, Camera2D camera, TilePalette palette)
    {
        if (spriteBatch is null) throw new ArgumentNullException(nameof(spriteBatch));
        if (camera is null) throw new ArgumentNullException(nameof(camera));
        if (palette is null) throw new ArgumentNullException(nameof(palette));
        if (_tileTexture is null || map is null)
        {
            return;
        }

        spriteBatch.Begin(
            samplerState: SamplerState.PointClamp,
            transformMatrix: camera.GetViewMatrix());

        for (var y = 0; y < map.Terrain.Count; y++)
        {
            var row = map.Terrain[y];
            for (var x = 0; x < row.Count; x++)
            {
                var code = row[x];
                if (code == 0)
                {
                    continue;
                }

                var destination = new Rectangle(
                    x * _tileSize,
                    y * _tileSize,
                    _tileSize,
                    _tileSize);
                spriteBatch.Draw(_tileTexture, destination, palette[code]);
            }
        }

        spriteBatch.End();
    }

    public void Dispose()
    {
        _tileTexture?.Dispose();
    }
}
