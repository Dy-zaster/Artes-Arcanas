using System;
using System.Collections.Generic;
using Laa.Monogame.Client.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Laa.Monogame.Client.UI;

public sealed class UiManager : IDisposable
{
    private readonly List<UiPanel> _windows = new();
    private readonly Texture2D _panelTexture;
    private readonly DebugTextRenderer _textRenderer;

    public UiManager(GraphicsDevice graphicsDevice, DebugTextRenderer textRenderer)
    {
        if (graphicsDevice is null) throw new ArgumentNullException(nameof(graphicsDevice));
        _textRenderer = textRenderer ?? throw new ArgumentNullException(nameof(textRenderer));
        _panelTexture = new Texture2D(graphicsDevice, 1, 1);
        _panelTexture.SetData(new[] { Color.White });
    }

    public void AddWindow(UiPanel panel)
    {
        if (panel is null)
        {
            return;
        }

        _windows.Add(panel);
    }

    public void Update(GameTime gameTime, MouseState currentMouse, MouseState previousMouse)
    {
        if (_windows.Count == 0)
        {
            return;
        }

        foreach (var window in _windows)
        {
            window.Update(gameTime, currentMouse, previousMouse);
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        if (_windows.Count == 0 || spriteBatch is null)
        {
            return;
        }

        spriteBatch.Begin(
            samplerState: SamplerState.PointClamp,
            blendState: BlendState.NonPremultiplied);

        foreach (var window in _windows)
        {
            window.Draw(spriteBatch, _textRenderer, _panelTexture);
        }

        spriteBatch.End();
    }

    public void Dispose()
    {
        _panelTexture.Dispose();
    }
}
