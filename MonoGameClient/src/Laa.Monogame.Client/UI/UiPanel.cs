using System;
using System.Collections.Generic;
using Laa.Monogame.Client.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Laa.Monogame.Client.UI;

public sealed class UiPanel
{
    private readonly List<IUiWidget> _children = new();
    private bool _isDragging;
    private Point _dragOffset;

    public UiPanel(string title, Rectangle bounds)
    {
        Title = title ?? string.Empty;
        Bounds = bounds;
    }

    public string Title { get; set; }

    public Rectangle Bounds { get; private set; }

    public bool Visible { get; set; } = true;

    public bool Draggable { get; set; } = true;

    public bool DragAnywhere { get; set; }

    public int HeaderHeight { get; set; } = 24;

    public bool UseDefaultChrome { get; set; } = true;

    public int ContentPadding { get; set; } = 8;

    public void SetBounds(Rectangle bounds)
    {
        Bounds = bounds;
    }

    public void AddWidget(IUiWidget widget)
    {
        if (widget is null)
        {
            return;
        }

        _children.Add(widget);
    }

    internal void Update(GameTime gameTime, MouseState currentMouse, MouseState previousMouse)
    {
        if (!Visible)
        {
            return;
        }

        HandleDragging(currentMouse, previousMouse);
        var contentBounds = CalculateContentBounds();
        foreach (var child in _children)
        {
            child.Update(gameTime, currentMouse, previousMouse, contentBounds);
        }
    }

    internal void Draw(SpriteBatch spriteBatch, DebugTextRenderer textRenderer, Texture2D backgroundTexture)
    {
        if (!Visible)
        {
            return;
        }

        if (spriteBatch is null) throw new ArgumentNullException(nameof(spriteBatch));
        if (textRenderer is null) throw new ArgumentNullException(nameof(textRenderer));
        if (backgroundTexture is null) throw new ArgumentNullException(nameof(backgroundTexture));

        if (UseDefaultChrome)
        {
            var headerRect = new Rectangle(Bounds.X, Bounds.Y, Bounds.Width, HeaderHeight);
            var bodyRect = new Rectangle(Bounds.X, Bounds.Y + HeaderHeight, Bounds.Width, Bounds.Height - HeaderHeight);
            spriteBatch.Draw(backgroundTexture, headerRect, new Color(0.1f, 0.1f, 0.1f, 0.85f));
            spriteBatch.Draw(backgroundTexture, bodyRect, new Color(0f, 0f, 0f, 0.75f));

            var titlePosition = new Vector2(headerRect.Left + 8, headerRect.Top + 5);
            textRenderer.DrawString(spriteBatch, Title.ToUpperInvariant(), titlePosition, Color.LightGreen);
            DrawBorder(spriteBatch, backgroundTexture, Bounds, 1, new Color(0f, 0f, 0f, 0.9f));
        }

        var contentBounds = CalculateContentBounds();
        foreach (var child in _children)
        {
            child.Draw(spriteBatch, textRenderer, contentBounds);
        }
    }

    private void HandleDragging(MouseState currentMouse, MouseState previousMouse)
    {
        if (!Draggable)
        {
            _isDragging = false;
            return;
        }

        var mousePoint = new Point(currentMouse.X, currentMouse.Y);
        var headerRect = new Rectangle(Bounds.X, Bounds.Y, Bounds.Width, HeaderHeight);
        var dragRect = DragAnywhere || !UseDefaultChrome ? Bounds : headerRect;
        var pressedNow = currentMouse.LeftButton == ButtonState.Pressed;
        var pressedBefore = previousMouse.LeftButton == ButtonState.Pressed;

        if (!_isDragging && pressedNow && !pressedBefore && dragRect.Contains(mousePoint))
        {
            _isDragging = true;
            _dragOffset = new Point(mousePoint.X - Bounds.X, mousePoint.Y - Bounds.Y);
        }
        else if (!pressedNow)
        {
            _isDragging = false;
        }

        if (_isDragging)
        {
            var newX = mousePoint.X - _dragOffset.X;
            var newY = mousePoint.Y - _dragOffset.Y;
            Bounds = new Rectangle(newX, newY, Bounds.Width, Bounds.Height);
        }
    }

    private Rectangle CalculateContentBounds()
    {
        var padding = Math.Max(0, ContentPadding);
        var headerOffset = UseDefaultChrome ? HeaderHeight : 0;
        var x = Bounds.X + padding;
        var y = Bounds.Y + headerOffset + padding;
        var width = Math.Max(0, Bounds.Width - padding * 2);
        var height = Math.Max(0, Bounds.Height - headerOffset - padding * 2);
        return new Rectangle(x, y, width, height);
    }

    private static void DrawBorder(SpriteBatch spriteBatch, Texture2D texture, Rectangle bounds, int thickness, Color color)
    {
        spriteBatch.Draw(texture, new Rectangle(bounds.Left, bounds.Top, bounds.Width, thickness), color);
        spriteBatch.Draw(texture, new Rectangle(bounds.Left, bounds.Bottom - thickness, bounds.Width, thickness), color);
        spriteBatch.Draw(texture, new Rectangle(bounds.Left, bounds.Top, thickness, bounds.Height), color);
        spriteBatch.Draw(texture, new Rectangle(bounds.Right - thickness, bounds.Top, thickness, bounds.Height), color);
    }
}
