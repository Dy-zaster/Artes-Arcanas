using System;
using System.Collections.Generic;
using System.Linq;
using Laa.Monogame.Client.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Laa.Monogame.Client.UI;

public sealed class UiItemGridWidget : IUiWidget
{
    private IReadOnlyList<string> _items = Array.Empty<string>();

    public UiItemGridWidget(int columns, Vector2 cellSize, Color? color = null)
    {
        Columns = Math.Max(1, columns);
        CellSize = cellSize;
        TextColor = color ?? Color.White;
    }

    public int Columns { get; set; }

    public Vector2 CellSize { get; set; }

    public Vector2 Offset { get; set; } = Vector2.Zero;

    public Vector2 Padding { get; set; } = new Vector2(6f, 2f);

    public Color TextColor { get; set; }

    public void SetItems(IReadOnlyList<string> items)
    {
        _items = items ?? Array.Empty<string>();
    }

    public void Update(GameTime gameTime, MouseState currentMouse, MouseState previousMouse, Rectangle contentBounds)
    {
        // Static preview; scrolling/filtering can be added later.
    }

    public void Draw(SpriteBatch spriteBatch, DebugTextRenderer textRenderer, Rectangle contentBounds)
    {
        if (_items.Count == 0)
        {
            textRenderer.DrawString(
                spriteBatch,
                "NO ITEMS AVAILABLE",
                new Vector2(contentBounds.Left, contentBounds.Top),
                TextColor);
            return;
        }

        var origin = new Vector2(contentBounds.Left, contentBounds.Top) + Offset;
        for (var i = 0; i < _items.Count; i++)
        {
            var row = i / Columns;
            var col = i % Columns;
            var cellOrigin = origin + new Vector2(col * CellSize.X, row * CellSize.Y);
            if (cellOrigin.Y >= contentBounds.Bottom - CellSize.Y)
            {
                break;
            }

            var label = $"{i + 1:D2}. {_items[i]}";
            textRenderer.DrawString(
                spriteBatch,
                label.ToUpperInvariant(),
                cellOrigin + Padding,
                TextColor);
        }
    }
}
