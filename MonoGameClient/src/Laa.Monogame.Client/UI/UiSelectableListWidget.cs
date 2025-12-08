using Laa.Monogame.Client.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Laa.Monogame.Client.UI;

public sealed class UiSelectableListWidget : IUiWidget
{
    private IReadOnlyList<string> _items = Array.Empty<string>();
    private readonly HashSet<int> _selected = new();

    public UiSelectableListWidget(int columns, Vector2 cellSize, Color? color = null, bool multiSelect = false, int maxSelection = int.MaxValue)
    {
        Columns = Math.Max(1, columns);
        CellSize = cellSize;
        Color = color ?? Color.White;
        MultiSelect = multiSelect;
        MaxSelection = maxSelection;
    }

    public int Columns { get; set; }

    public Vector2 CellSize { get; set; }

    public Vector2 Offset { get; set; } = Vector2.Zero;

    public Color Color { get; set; }

    public bool MultiSelect { get; set; }

    public int MaxSelection { get; set; } = int.MaxValue;

    public event Action<IReadOnlyCollection<int>>? SelectionChanged;
    public event Action<int>? ItemActivated;

    public IReadOnlyCollection<int> SelectedIndices => _selected;

    public void SetItems(IReadOnlyList<string> items)
    {
        _items = items ?? Array.Empty<string>();
        _selected.Clear();
        SelectionChanged?.Invoke(_selected);
    }

    public void Update(GameTime gameTime, MouseState currentMouse, MouseState previousMouse, Rectangle contentBounds)
    {
        var justClicked = currentMouse.LeftButton == ButtonState.Pressed && previousMouse.LeftButton == ButtonState.Released;
        if (!justClicked)
        {
            return;
        }

        var origin = new Vector2(contentBounds.Left, contentBounds.Top) + Offset;
        var mousePoint = new Point(currentMouse.X, currentMouse.Y);
        for (var i = 0; i < _items.Count; i++)
        {
            var row = i / Columns;
            var col = i % Columns;
            var x = origin.X + col * CellSize.X;
            var y = origin.Y + row * CellSize.Y;
            var rect = new Rectangle((int)x, (int)y, (int)CellSize.X, (int)CellSize.Y);
            if (rect.Contains(mousePoint))
            {
                ToggleSelection(i);
                ItemActivated?.Invoke(i);
                break;
            }
        }
    }

    public void Draw(SpriteBatch spriteBatch, DebugTextRenderer textRenderer, Rectangle contentBounds)
    {
        if (_items.Count == 0)
        {
            textRenderer.DrawString(spriteBatch, "SIN DATOS", new Vector2(contentBounds.Left, contentBounds.Top) + Offset, Color);
            return;
        }

        var origin = new Vector2(contentBounds.Left, contentBounds.Top) + Offset;
        for (var i = 0; i < _items.Count; i++)
        {
            var row = i / Columns;
            var col = i % Columns;
            var position = origin + new Vector2(col * CellSize.X, row * CellSize.Y);
            if (position.Y > contentBounds.Bottom - CellSize.Y)
            {
                break;
            }

            var selected = _selected.Contains(i);
            var label = selected ? $"> {_items[i].ToUpperInvariant()}" : _items[i].ToUpperInvariant();
            textRenderer.DrawString(spriteBatch, label, position, selected ? Color.Yellow : Color);
        }
    }

    private void ToggleSelection(int index)
    {
        if (!MultiSelect)
        {
            _selected.Clear();
            _selected.Add(index);
            SelectionChanged?.Invoke(_selected);
            return;
        }

        if (_selected.Contains(index))
        {
            _selected.Remove(index);
        }
        else
        {
            if (_selected.Count >= MaxSelection)
            {
                return;
            }
            _selected.Add(index);
        }

        SelectionChanged?.Invoke(_selected);
    }
}
