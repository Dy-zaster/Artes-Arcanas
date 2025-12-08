using Laa.Monogame.Client.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Laa.Monogame.Client.UI;

public enum HudGridMode
{
    Inventory,
    Spells
}

/// <summary>
/// Draws the fixed equipment slots and the inventory/spell grid that live inside the HUD strip.
/// Uses the legacy <c>obj</c> atlas for gear and <c>cjr</c> for runes.
/// </summary>
public sealed class HudPaperDollWidget : IUiWidget
{
    private const int IconSize = 40;
    private const int IconColumns = 8;
    private const int BackpackColumns = 6;
    private const int BackpackVisibleRows = 3;
    private const float BackpackCellSpacing = 42f;
    private const int ScrollStep = BackpackColumns;
    private const int TabTopOffset = -16;
    private const int TabHeight = 28;
    private const int InventoryTabX = 1144;
    private const int SpellTabX = 1210;
    private const int TabRight = 1272;
    public const int VisibleBackpackSlots = BackpackColumns * BackpackVisibleRows;

    private static readonly Rectangle[] EquipmentSlots =
    {
        new(760, 86, IconSize, IconSize), // Right hand
        new(718, 86, IconSize, IconSize), // Left hand
        new(966, 44, IconSize, IconSize), // Armor
        new(966, 2, IconSize, IconSize),  // Helmet
        new(966, 86, IconSize, IconSize), // Bracelet / shield
        new(856, 86, IconSize, IconSize), // Ring
        new(856, 44, IconSize, IconSize), // Amulet
        new(856, 2, IconSize, IconSize)   // Ammunition
    };

    private static readonly Vector2 BackpackOrigin = new(1028f, 2f);
    private static readonly Rectangle InventoryTabBounds = new(
        InventoryTabX,
        TabTopOffset,
        Math.Max(1, SpellTabX - InventoryTabX),
        TabHeight);
    private static readonly Rectangle SpellTabBounds = new(
        SpellTabX,
        TabTopOffset,
        Math.Max(1, TabRight - SpellTabX),
        TabHeight);

    private readonly UiSpriteLibrary _spriteLibrary;
    private readonly List<int> _equipmentIcons = new(EquipmentSlots.Length);
    private readonly List<int> _backpackIcons = new();
    private readonly List<int> _spellIcons = new();
    private HudGridMode _gridMode = HudGridMode.Inventory;
    private int _selectedSpellIndex = -1;
    private int _selectedInventoryIndex = -1;
    private int _inventoryScrollOffset;
    private int _spellScrollOffset;

    public HudPaperDollWidget(UiSpriteLibrary spriteLibrary)
    {
        _spriteLibrary = spriteLibrary ?? throw new ArgumentNullException(nameof(spriteLibrary));
        SetEquipmentIcons(Array.Empty<int>());
        SetBackpackIcons(Array.Empty<int>());
        SetSpellIcons(Array.Empty<int>());
    }

    public event Action<HudGridMode>? GridModeChanged;

    public event Action? SpellTabBlocked;

    public event Action<int, QuickActionIcon>? SpellSlotActivated;
    public event Action<int>? InventorySlotActivated;

    public HudGridMode GridMode => _gridMode;

    public bool SpellTabAvailable => HasSpellIcons();

    public Vector2 AdditionalOffset { get; set; } = Vector2.Zero;

    public void SetEquipmentIcons(IReadOnlyList<int>? iconIds)
    {
        _equipmentIcons.Clear();
        if (iconIds is not null)
        {
            for (var i = 0; i < Math.Min(iconIds.Count, EquipmentSlots.Length); i++)
            {
                _equipmentIcons.Add(iconIds[i]);
            }
        }

        while (_equipmentIcons.Count < EquipmentSlots.Length)
        {
            _equipmentIcons.Add(-1);
        }
    }

    public bool HandleScroll(Point mousePoint, Rectangle contentBounds, int scrollDelta)
    {
        var rect = GetGridBounds(contentBounds);
        if (!rect.Contains(mousePoint))
        {
            return false;
        }

        if (scrollDelta == 0)
        {
            return true;
        }

        var direction = Math.Sign(scrollDelta);
        if (direction == 0)
        {
            return true;
        }

        var mode = _gridMode;
        if (mode == HudGridMode.Spells && !SpellTabAvailable)
        {
            mode = HudGridMode.Inventory;
        }

        if (mode == HudGridMode.Spells)
        {
            ScrollSpellGrid(direction);
            return true;
        }

        ScrollInventoryGrid(direction);
        return true;
    }

    public void SetBackpackIcons(IReadOnlyList<int>? iconIds)
    {
        _backpackIcons.Clear();
        if (iconIds is null)
        {
            return;
        }

        _backpackIcons.AddRange(iconIds);
        _selectedInventoryIndex = -1;
        _inventoryScrollOffset = 0;
    }

    public void SetSpellIcons(IReadOnlyList<int>? iconIds)
    {
        _spellIcons.Clear();
        if (iconIds is not null)
        {
            _spellIcons.AddRange(iconIds);
        }

        if (_selectedSpellIndex >= _spellIcons.Count)
        {
            _selectedSpellIndex = -1;
        }

        _spellScrollOffset = 0;
        if (!HasSpellIcons() && _gridMode == HudGridMode.Spells)
        {
            ApplyGridMode(HudGridMode.Inventory);
        }
    }

    public void RequestGridMode(HudGridMode mode)
    {
        if (mode == HudGridMode.Spells && !HasSpellIcons())
        {
            SpellTabBlocked?.Invoke();
            return;
        }

        ApplyGridMode(mode);
    }

    public void Update(GameTime gameTime, MouseState currentMouse, MouseState previousMouse, Rectangle contentBounds)
    {
        if (currentMouse.LeftButton == ButtonState.Pressed && previousMouse.LeftButton == ButtonState.Released)
        {
            var mousePoint = new Point(currentMouse.X, currentMouse.Y);
            var origin = new Vector2(contentBounds.Left, contentBounds.Top) + AdditionalOffset;
            if (HitTestTab(origin, InventoryTabBounds, mousePoint))
            {
                RequestGridMode(HudGridMode.Inventory);
            }
            else if (HitTestTab(origin, SpellTabBounds, mousePoint))
            {
                RequestGridMode(HudGridMode.Spells);
            }
            else
            {
                var isSpellMode = _gridMode == HudGridMode.Spells;
                var icons = isSpellMode ? _spellIcons : _backpackIcons;
                var startOffset = isSpellMode
                    ? ClampOffset(_spellIcons.Count, _spellScrollOffset)
                    : ClampOffset(_backpackIcons.Count, _inventoryScrollOffset);
                var slot = HitTestGrid(origin, mousePoint, startOffset, icons.Count);
                if (slot >= 0)
                {
                    if (_gridMode == HudGridMode.Spells)
                    {
                        if (slot < _spellIcons.Count)
                        {
                            _selectedSpellIndex = slot;
                            var icon = new QuickActionIcon(QuickActionIconType.Spell, _spellIcons[slot]);
                            SpellSlotActivated?.Invoke(slot, icon);
                        }
                    }
                    else
                    {
                        if (slot < _backpackIcons.Count)
                        {
                            _selectedInventoryIndex = slot;
                            InventorySlotActivated?.Invoke(slot);
                        }
                    }
                }
            }
        }
    }

    public void Draw(SpriteBatch spriteBatch, DebugTextRenderer textRenderer, Rectangle contentBounds)
    {
        if (spriteBatch is null) throw new ArgumentNullException(nameof(spriteBatch));
        if (!_spriteLibrary.TryGetSprite("obj", out var itemSprite))
        {
            return;
        }

        var origin = new Vector2(contentBounds.Left, contentBounds.Top) + AdditionalOffset;
        DrawEquipment(spriteBatch, itemSprite, origin);

        if (_gridMode == HudGridMode.Inventory || !HasSpellIcons())
        {
            DrawGrid(
                spriteBatch,
                itemSprite,
                origin,
                _backpackIcons,
                _inventoryScrollOffset,
                _selectedInventoryIndex);
        }
        else if (_spriteLibrary.TryGetSprite("cjr", out var spellSprite))
        {
            DrawGrid(
                spriteBatch,
                spellSprite,
                origin,
                _spellIcons,
                _spellScrollOffset,
                _selectedSpellIndex);
        }
        else
        {
            DrawGrid(
                spriteBatch,
                itemSprite,
                origin,
                _backpackIcons,
                _inventoryScrollOffset,
                _selectedInventoryIndex);
        }
    }

    private void ApplyGridMode(HudGridMode mode)
    {
        if (_gridMode == mode)
        {
            return;
        }

        _gridMode = mode;
        GridModeChanged?.Invoke(mode);
    }

    private void DrawEquipment(SpriteBatch spriteBatch, UiSprite sprite, Vector2 origin)
    {
        for (var i = 0; i < EquipmentSlots.Length; i++)
        {
            var iconId = i < _equipmentIcons.Count ? _equipmentIcons[i] : -1;
            if (iconId < 0)
            {
                continue;
            }

            var destination = OffsetRect(EquipmentSlots[i], origin);
            var source = ResolveIconSource(sprite.Source, iconId);
            if (source.Width == 0 || source.Height == 0)
            {
                continue;
            }

            spriteBatch.Draw(sprite.Texture, destination, source, Color.White);
        }
    }

    private void DrawGrid(
        SpriteBatch spriteBatch,
        UiSprite sprite,
        Vector2 origin,
        IReadOnlyList<int> icons,
        int scrollOffset,
        int selectedIndex)
    {
        if (icons.Count == 0)
        {
            return;
        }

        var start = origin + BackpackOrigin;
        var offset = ClampOffset(icons.Count, scrollOffset);
        for (var slot = 0; slot < VisibleBackpackSlots; slot++)
        {
            var index = offset + slot;
            if (index < 0 || index >= icons.Count)
            {
                break;
            }

            var iconId = icons[index];
            if (iconId < 0)
            {
                continue;
            }

            var column = slot % BackpackColumns;
            var row = slot / BackpackColumns;
            var destination = new Rectangle(
                (int)Math.Floor(start.X + column * BackpackCellSpacing),
                (int)Math.Floor(start.Y + row * BackpackCellSpacing),
                IconSize,
                IconSize);
            var source = ResolveIconSource(sprite.Source, iconId);
            if (source.Width == 0 || source.Height == 0)
            {
                continue;
            }

            spriteBatch.Draw(sprite.Texture, destination, source, Color.White);
            if (selectedIndex >= 0 && index == selectedIndex)
            {
                spriteBatch.Draw(sprite.Texture, destination, source, new Color(Color.CornflowerBlue, 0.4f));
            }
        }
    }

    private static Rectangle OffsetRect(Rectangle rect, Vector2 offset)
    {
        var x = (int)Math.Floor(rect.X + offset.X);
        var y = (int)Math.Floor(rect.Y + offset.Y);
        return new Rectangle(x, y, rect.Width, rect.Height);
    }

    private static Rectangle ResolveIconSource(Rectangle sheetBounds, int iconId)
    {
        if (iconId < 0)
        {
            return Rectangle.Empty;
        }

        var index = Math.Clamp(iconId, 0, 255);
        var column = index % IconColumns;
        var row = index / IconColumns;
        var x = sheetBounds.X + column * IconSize;
        var y = sheetBounds.Y + row * IconSize;
        if (x + IconSize > sheetBounds.Right || y + IconSize > sheetBounds.Bottom)
        {
            return Rectangle.Empty;
        }

        return new Rectangle(x, y, IconSize, IconSize);
    }

    private static bool HitTestTab(Vector2 origin, Rectangle relativeRect, Point mousePoint)
    {
        var rect = new Rectangle(
            (int)Math.Floor(origin.X + relativeRect.X),
            (int)Math.Floor(origin.Y + relativeRect.Y),
            relativeRect.Width,
            relativeRect.Height);
        return rect.Contains(mousePoint);
    }

    private static Rectangle BuildGridBounds(Rectangle contentBounds, Vector2 additionalOffset)
    {
        var origin = new Vector2(contentBounds.Left, contentBounds.Top) + additionalOffset + BackpackOrigin;
        var width = IconSize + (BackpackColumns - 1) * BackpackCellSpacing;
        var height = IconSize + (BackpackVisibleRows - 1) * BackpackCellSpacing;
        return new Rectangle(
            (int)Math.Floor(origin.X),
            (int)Math.Floor(origin.Y),
            (int)Math.Ceiling(width),
            (int)Math.Ceiling(height));
    }

    private bool HasSpellIcons()
    {
        for (var i = 0; i < _spellIcons.Count; i++)
        {
            if (_spellIcons[i] >= 0)
            {
                return true;
            }
        }

        return false;
    }

    private int HitTestGrid(Vector2 origin, Point mousePoint, int startOffset, int totalCount)
    {
        var start = origin + BackpackOrigin;
        for (var i = 0; i < VisibleBackpackSlots; i++)
        {
            var column = i % BackpackColumns;
            var row = i / BackpackColumns;
            var rect = new Rectangle(
                (int)Math.Floor(start.X + column * BackpackCellSpacing),
                (int)Math.Floor(start.Y + row * BackpackCellSpacing),
                IconSize,
                IconSize);
            if (rect.Contains(mousePoint))
            {
                var index = startOffset + i;
                if (index < totalCount)
                {
                    return index;
                }
                break;
            }
        }

        return -1;
    }

    private static int ClampOffset(int totalCount, int offset)
    {
        if (totalCount <= VisibleBackpackSlots)
        {
            return 0;
        }

        var maxOffset = Math.Max(0, totalCount - VisibleBackpackSlots);
        return Math.Clamp(offset, 0, maxOffset);
    }

    private bool ScrollInventoryGrid(int direction)
    {
        if (_backpackIcons.Count <= VisibleBackpackSlots)
        {
            return false;
        }

        var old = _inventoryScrollOffset;
        var maxOffset = Math.Max(0, _backpackIcons.Count - VisibleBackpackSlots);
        _inventoryScrollOffset = Math.Clamp(_inventoryScrollOffset - direction * ScrollStep, 0, maxOffset);
        return _inventoryScrollOffset != old;
    }

    private bool ScrollSpellGrid(int direction)
    {
        if (_spellIcons.Count <= VisibleBackpackSlots)
        {
            return false;
        }

        var old = _spellScrollOffset;
        var maxOffset = Math.Max(0, _spellIcons.Count - VisibleBackpackSlots);
        _spellScrollOffset = Math.Clamp(_spellScrollOffset - direction * ScrollStep, 0, maxOffset);
        return _spellScrollOffset != old;
    }

    private Rectangle GetGridBounds(Rectangle contentBounds)
    {
        return BuildGridBounds(contentBounds, AdditionalOffset);
    }
}
