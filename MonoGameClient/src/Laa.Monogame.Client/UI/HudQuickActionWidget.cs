using Laa.Monogame.Client.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Laa.Monogame.Client.UI;

public enum QuickActionIconType
{
    Item,
    Spell
}

public readonly record struct QuickActionIcon(QuickActionIconType Type, int Id);

/// <summary>
/// Draws the three quick-action icons (puño, arma, runa) that sit next to the portrait.
/// </summary>
public sealed class HudQuickActionWidget : IUiWidget
{
    private const int IconSize = 32;
    private const float IconSpacing = 44f;
    private const int SourceTileSize = 40;
    private const int SourceColumns = 8;

    private readonly UiSpriteLibrary _spriteLibrary;
    private readonly QuickActionIcon[] _icons = new QuickActionIcon[3];
    private readonly Rectangle[] _computedBounds = new Rectangle[3];
    private int _hoverIndex = -1;
    private int _activeIndex = -1;

    public HudQuickActionWidget(UiSpriteLibrary spriteLibrary)
    {
        _spriteLibrary = spriteLibrary ?? throw new ArgumentNullException(nameof(spriteLibrary));
        for (var i = 0; i < _icons.Length; i++)
        {
            _icons[i] = new QuickActionIcon(QuickActionIconType.Item, -1);
        }
    }

    public Vector2 Offset { get; set; }

    public event Action<int, QuickActionIcon>? ActionInvoked;

    public void SetIcons(IReadOnlyList<QuickActionIcon> icons)
    {
        for (var i = 0; i < _icons.Length; i++)
        {
            _icons[i] = i < icons.Count ? icons[i] : new QuickActionIcon(QuickActionIconType.Item, -1);
        }
    }

    public int ActiveIndex
    {
        get => _activeIndex;
        set => _activeIndex = Math.Clamp(value, -1, _icons.Length - 1);
    }

    public void Update(GameTime gameTime, MouseState currentMouse, MouseState previousMouse, Rectangle contentBounds)
    {
        var origin = new Vector2(contentBounds.Left, contentBounds.Top) + Offset;
        _hoverIndex = -1;
        for (var i = 0; i < _icons.Length; i++)
        {
            _computedBounds[i] = BuildSlotBounds(origin, i);
            if (_icons[i].Id >= 0 && _computedBounds[i].Contains(currentMouse.Position))
            {
                _hoverIndex = i;
            }
        }

        var clicked = currentMouse.LeftButton == ButtonState.Pressed &&
                      previousMouse.LeftButton == ButtonState.Released &&
                      _hoverIndex >= 0 &&
                      _icons[_hoverIndex].Id >= 0;
        if (clicked)
        {
            ActionInvoked?.Invoke(_hoverIndex, _icons[_hoverIndex]);
        }
    }

    public void Draw(SpriteBatch spriteBatch, DebugTextRenderer textRenderer, Rectangle contentBounds)
    {
        if (spriteBatch is null) throw new ArgumentNullException(nameof(spriteBatch));

        var origin = new Vector2(contentBounds.Left, contentBounds.Top) + Offset;
        for (var i = 0; i < _icons.Length; i++)
        {
            var icon = _icons[i];
            if (icon.Id < 0)
            {
                continue;
            }

            var spriteKey = icon.Type == QuickActionIconType.Spell ? "cjr" : "obj";
            if (!_spriteLibrary.TryGetSprite(spriteKey, out var sprite))
            {
                continue;
            }

            var source = ResolveSource(sprite.Source, icon.Id);
            if (source.Width == 0 || source.Height == 0)
            {
                continue;
            }

            var destination = BuildSlotBounds(origin, i);
            spriteBatch.Draw(sprite.Texture, destination, source, Color.White);
            var overlayColor = Color.Transparent;
            if (_activeIndex == i)
            {
                overlayColor = new Color(Color.CornflowerBlue, 0.35f);
            }

            if (_hoverIndex == i)
            {
                overlayColor = new Color(Color.Yellow, 0.35f);
            }

            if (overlayColor.A > 0)
            {
                spriteBatch.Draw(sprite.Texture, destination, source, overlayColor);
            }
        }
    }

    private static Rectangle BuildSlotBounds(Vector2 origin, int index)
    {
        return new Rectangle(
            (int)Math.Floor(origin.X + index * IconSpacing),
            (int)Math.Floor(origin.Y),
            IconSize,
            IconSize);
    }

    private static Rectangle ResolveSource(Rectangle sheetBounds, int index)
    {
        var safeIndex = Math.Max(0, index);
        var column = safeIndex % SourceColumns;
        var row = safeIndex / SourceColumns;
        var x = sheetBounds.X + column * SourceTileSize;
        var y = sheetBounds.Y + row * SourceTileSize;
        if (x + SourceTileSize > sheetBounds.Right || y + SourceTileSize > sheetBounds.Bottom)
        {
            return Rectangle.Empty;
        }

        return new Rectangle(x, y, SourceTileSize, SourceTileSize);
    }
}
