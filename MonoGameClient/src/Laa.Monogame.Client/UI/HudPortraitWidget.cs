using System;
using Laa.Monogame.Client.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Laa.Monogame.Client.UI;

/// <summary>
/// Renders the 40×40 avatar portrait (rostro) above the “Menú” block in the HUD.
/// </summary>
public sealed class HudPortraitWidget : IUiWidget
{
    private const int PortraitSize = 40;
    private const int PortraitColumns = 8;

    private readonly UiSpriteLibrary _spriteLibrary;
    private int _portraitIndex;
    private bool _hovered;

    public HudPortraitWidget(UiSpriteLibrary spriteLibrary)
    {
        _spriteLibrary = spriteLibrary ?? throw new ArgumentNullException(nameof(spriteLibrary));
    }

    public Vector2 Offset { get; set; } = new(432f, 86f);

    public int PortraitIndex
    {
        get => _portraitIndex;
        set => _portraitIndex = Math.Max(0, value);
    }

    public event Action<int>? PortraitClicked;

    public void Update(GameTime gameTime, MouseState currentMouse, MouseState previousMouse, Rectangle contentBounds)
    {
        var bounds = BuildBounds(contentBounds);
        _hovered = bounds.Contains(currentMouse.Position);
        if (_hovered &&
            currentMouse.LeftButton == ButtonState.Pressed &&
            previousMouse.LeftButton == ButtonState.Released)
        {
            PortraitClicked?.Invoke(_portraitIndex);
        }
    }

    public void Draw(SpriteBatch spriteBatch, DebugTextRenderer textRenderer, Rectangle contentBounds)
    {
        if (spriteBatch is null) throw new ArgumentNullException(nameof(spriteBatch));
        if (!_spriteLibrary.TryGetSprite("ros", out var sprite))
        {
            return;
        }

        var source = ResolvePortraitSource(sprite.Source, _portraitIndex);
        if (source.Width == 0 || source.Height == 0)
        {
            return;
        }

        var destination = BuildBounds(contentBounds);
        spriteBatch.Draw(sprite.Texture, destination, source, Color.White);
        if (_hovered)
        {
            spriteBatch.Draw(sprite.Texture, destination, source, new Color(Color.LightYellow, 0.35f));
        }
    }

    private static Rectangle ResolvePortraitSource(Rectangle sheetBounds, int index)
    {
        var safeIndex = Math.Max(0, index);
        var column = safeIndex % PortraitColumns;
        var row = safeIndex / PortraitColumns;
        var x = sheetBounds.X + column * PortraitSize;
        var y = sheetBounds.Y + row * PortraitSize;
        if (x + PortraitSize > sheetBounds.Right || y + PortraitSize > sheetBounds.Bottom)
        {
            return Rectangle.Empty;
        }

        return new Rectangle(x, y, PortraitSize, PortraitSize);
    }

    private Rectangle BuildBounds(Rectangle contentBounds)
    {
        return new Rectangle(
            (int)Math.Floor(contentBounds.Left + Offset.X),
            (int)Math.Floor(contentBounds.Top + Offset.Y),
            PortraitSize,
            PortraitSize);
    }
}
