using System;
using System.Collections.Generic;
using Laa.Monogame.Client.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Laa.Monogame.Client.UI;

public enum InventorySelectionKind
{
    None,
    Equipment,
    Backpack
}

public readonly record struct InventorySelectionChangedEventArgs(
    InventorySelectionKind Kind,
    int Index,
    string Label
)
{
    public static readonly InventorySelectionChangedEventArgs None =
        new(InventorySelectionKind.None, -1, "NINGUNO");
}

public sealed class UiInventoryWidget : IUiWidget
{
    private readonly List<KeyValuePair<string, string>> _equipment = new();
    private readonly List<string> _backpack = new();
    private int _selectedEquipmentIndex = -1;
    private int _selectedBackpackIndex = -1;

    public Color EquipmentColor { get; set; } = Color.LightGreen;

    public Color ItemColor { get; set; } = Color.White;

    public Vector2 EquipmentOrigin { get; set; } = new(12f, 40f);

    public float EquipmentLineHeight { get; set; } = 20f;
    public float EquipmentColumnWidth { get; set; } = 150f;

    public Vector2 BackpackOrigin { get; set; } = new(180f, 40f);

    public Vector2 BackpackCellSize { get; set; } = new(84f, 20f);

    public int BackpackColumns { get; set; } = 4;

    public string SelectionText { get; private set; } = "NINGUNO";

    public event Action<InventorySelectionChangedEventArgs>? SelectionChanged;

    public void SetEquipment(IEnumerable<KeyValuePair<string, string>> entries)
    {
        _equipment.Clear();
        if (entries is null)
        {
            return;
        }

        foreach (var pair in entries)
        {
            var slot = pair.Key?.ToUpperInvariant() ?? "SIN SLOT";
            var name = pair.Value ?? string.Empty;
            _equipment.Add(new KeyValuePair<string, string>(slot, name));
        }

        _selectedEquipmentIndex = -1;
        _selectedBackpackIndex = -1;
        SelectionText = "NINGUNO";
        SelectionChanged?.Invoke(InventorySelectionChangedEventArgs.None);
    }

    public void SetBackpack(IReadOnlyList<string> items)
    {
        _backpack.Clear();
        if (items is null)
        {
            return;
        }

        _backpack.AddRange(items);
        _selectedEquipmentIndex = -1;
        _selectedBackpackIndex = -1;
        SelectionText = "NINGUNO";
        SelectionChanged?.Invoke(InventorySelectionChangedEventArgs.None);
    }

    public void Update(GameTime gameTime, MouseState currentMouse, MouseState previousMouse, Rectangle contentBounds)
    {
        if (currentMouse.LeftButton == ButtonState.Pressed && previousMouse.LeftButton == ButtonState.Released)
        {
            var point = new Point(currentMouse.X, currentMouse.Y);
            if (contentBounds.Contains(point))
            {
                if (TrySelectEquipment(point, contentBounds))
                {
                    return;
                }

                TrySelectBackpack(point, contentBounds);
            }
        }
    }

    public void Draw(SpriteBatch spriteBatch, DebugTextRenderer textRenderer, Rectangle contentBounds)
    {
        if (spriteBatch is null) throw new ArgumentNullException(nameof(spriteBatch));
        if (textRenderer is null) throw new ArgumentNullException(nameof(textRenderer));

        DrawEquipment(spriteBatch, textRenderer, contentBounds);
        DrawBackpack(spriteBatch, textRenderer, contentBounds);
    }

    private void DrawEquipment(SpriteBatch spriteBatch, DebugTextRenderer textRenderer, Rectangle contentBounds)
    {
        var origin = new Vector2(contentBounds.Left, contentBounds.Top) + EquipmentOrigin;
        var cursor = origin;
        if (_equipment.Count == 0)
        {
            textRenderer.DrawString(
                spriteBatch,
                "SIN EQUIPAMIENTO",
                cursor,
                EquipmentColor);
            return;
        }

        for (var i = 0; i < _equipment.Count; i++)
        {
            var entry = _equipment[i];
            var text = $"{entry.Key}: {entry.Value}".ToUpperInvariant();
            var color = i == _selectedEquipmentIndex ? Color.Red : EquipmentColor;
            textRenderer.DrawString(spriteBatch, text, cursor, color);
            cursor.Y += EquipmentLineHeight;
        }
    }

    private void DrawBackpack(SpriteBatch spriteBatch, DebugTextRenderer textRenderer, Rectangle contentBounds)
    {
        var origin = new Vector2(contentBounds.Left, contentBounds.Top) + BackpackOrigin;
        var columns = Math.Max(1, BackpackColumns);
        if (_backpack.Count == 0)
        {
            textRenderer.DrawString(spriteBatch, "MOCHILA VACIA", origin, ItemColor);
            return;
        }

        for (var i = 0; i < _backpack.Count; i++)
        {
            var row = i / columns;
            var col = i % columns;
            var position = origin + new Vector2(col * BackpackCellSize.X, row * BackpackCellSize.Y);
            if (position.Y > contentBounds.Bottom - BackpackCellSize.Y)
            {
                break;
            }

            var label = $"{i + 1:D2} {_backpack[i]}".ToUpperInvariant();
            var color = i == _selectedBackpackIndex ? Color.DarkOrange : ItemColor;
            textRenderer.DrawString(spriteBatch, label, position, color);
        }
    }

    private bool TrySelectEquipment(Point mousePoint, Rectangle contentBounds)
    {
        if (_equipment.Count == 0)
        {
            return false;
        }

        var startX = contentBounds.Left + EquipmentOrigin.X;
        var endX = startX + EquipmentColumnWidth;
        var startY = contentBounds.Top + EquipmentOrigin.Y;
        if (mousePoint.X < startX || mousePoint.X > endX)
        {
            return false;
        }

        var relativeY = mousePoint.Y - startY;
        if (relativeY < 0)
        {
            return false;
        }

        var index = (int)(relativeY / EquipmentLineHeight);
        if (index < 0 || index >= _equipment.Count)
        {
            return false;
        }

        _selectedEquipmentIndex = index;
        _selectedBackpackIndex = -1;
        var entry = _equipment[index];
        SelectionText = $"{entry.Key}: {entry.Value}".ToUpperInvariant();
        SelectionChanged?.Invoke(new InventorySelectionChangedEventArgs(
            InventorySelectionKind.Equipment,
            index,
            SelectionText));
        return true;
    }

    private bool TrySelectBackpack(Point mousePoint, Rectangle contentBounds)
    {
        if (_backpack.Count == 0)
        {
            return false;
        }

        var startX = contentBounds.Left + BackpackOrigin.X;
        var startY = contentBounds.Top + BackpackOrigin.Y;
        var width = BackpackColumns * BackpackCellSize.X;
        var height = Math.Ceiling(_backpack.Count / (float)Math.Max(1, BackpackColumns)) * BackpackCellSize.Y;
        var rect = new Rectangle(
            (int)Math.Floor(startX),
            (int)Math.Floor(startY),
            (int)Math.Ceiling(width),
            (int)Math.Ceiling(height));

        if (!rect.Contains(mousePoint))
        {
            return false;
        }

        var localX = mousePoint.X - rect.Left;
        var localY = mousePoint.Y - rect.Top;
        var col = (int)(localX / BackpackCellSize.X);
        var row = (int)(localY / BackpackCellSize.Y);
        var index = row * Math.Max(1, BackpackColumns) + col;
        if (index < 0 || index >= _backpack.Count)
        {
            return false;
        }

        _selectedBackpackIndex = index;
        _selectedEquipmentIndex = -1;
        SelectionText = $"MOCHILA: {_backpack[index]}".ToUpperInvariant();
        SelectionChanged?.Invoke(new InventorySelectionChangedEventArgs(
            InventorySelectionKind.Backpack,
            index,
            SelectionText));
        return true;
    }
}
