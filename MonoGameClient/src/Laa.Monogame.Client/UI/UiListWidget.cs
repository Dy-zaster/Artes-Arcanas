using Laa.Monogame.Client.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Laa.Monogame.Client.UI;

public sealed class UiListWidget : IUiWidget
{
    private IReadOnlyList<string> _items = Array.Empty<string>();

    public UiListWidget(int columns, Vector2 cellSize, Color? color = null)
    {
        Columns = Math.Max(1, columns);
        CellSize = cellSize;
        Color = color ?? Color.White;
    }

    public int Columns { get; set; }

    public Vector2 CellSize { get; set; }

    public Vector2 Offset { get; set; } = Vector2.Zero;

    public Color Color { get; set; }

    public void SetItems(IReadOnlyList<string> items)
    {
        _items = items ?? Array.Empty<string>();
    }

    public void Update(GameTime gameTime, MouseState currentMouse, MouseState previousMouse, Rectangle contentBounds)
    {
        // Static preview; no interaction yet.
    }

    public void Draw(SpriteBatch spriteBatch, DebugTextRenderer textRenderer, Rectangle contentBounds)
    {
        if (spriteBatch is null) throw new ArgumentNullException(nameof(spriteBatch));
        if (textRenderer is null) throw new ArgumentNullException(nameof(textRenderer));

        if (_items.Count == 0)
        {
            textRenderer.DrawString(
                spriteBatch,
                "SIN DATOS",
                new Vector2(contentBounds.Left, contentBounds.Top) + Offset,
                Color);
            return;
        }

        var origin = new Vector2(contentBounds.Left, contentBounds.Top) + Offset;
        var columns = Math.Max(1, Columns);
        for (var i = 0; i < _items.Count; i++)
        {
            var row = i / columns;
            var col = i % columns;
            var position = origin + new Vector2(col * CellSize.X, row * CellSize.Y);
            if (position.Y > contentBounds.Bottom - CellSize.Y)
            {
                break;
            }

            var label = _items[i].ToUpperInvariant();
            textRenderer.DrawString(spriteBatch, label, position, Color);
        }
    }
}
