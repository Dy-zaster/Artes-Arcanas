using Laa.Monogame.Client.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Laa.Monogame.Client.UI;

public sealed class UiButtonWidget : IUiWidget
{
    private bool _wasPressed;

    public UiButtonWidget(string text, Vector2 relativePosition, Point size, Color? background = null, Color? textColor = null)
    {
        Text = text ?? string.Empty;
        RelativePosition = relativePosition;
        Size = size;
        BackgroundColor = background ?? new Color(0.15f, 0.15f, 0.15f, 0.9f);
        TextColor = textColor ?? Color.White;
    }

    public string Text { get; set; }

    public Vector2 RelativePosition { get; set; }

    public Point Size { get; set; }

    public Color BackgroundColor { get; set; }

    public Color TextColor { get; set; }

    public bool IsEnabled { get; set; } = true;

    public event Action? Clicked;

    public void Update(GameTime gameTime, MouseState currentMouse, MouseState previousMouse, Rectangle contentBounds)
    {
        if (!IsEnabled)
        {
            _wasPressed = false;
            return;
        }

        var rect = GetBounds(contentBounds);
        var mousePoint = new Point(currentMouse.X, currentMouse.Y);
        var isHovering = rect.Contains(mousePoint);
        var isPressed = currentMouse.LeftButton == ButtonState.Pressed && isHovering;

        if (_wasPressed && currentMouse.LeftButton == ButtonState.Released && isHovering)
        {
            Clicked?.Invoke();
        }

        _wasPressed = isPressed;
    }

    public void Draw(SpriteBatch spriteBatch, DebugTextRenderer textRenderer, Rectangle contentBounds)
    {
        if (spriteBatch is null) throw new ArgumentNullException(nameof(spriteBatch));
        if (textRenderer is null) throw new ArgumentNullException(nameof(textRenderer));

        var rect = GetBounds(contentBounds);
        spriteBatch.Draw(
            texture: textRenderer.PixelTexture,
            destinationRectangle: rect,
            color: IsEnabled ? BackgroundColor : BackgroundColor * 0.4f);

        var textSize = textRenderer.MeasureString(Text);
        var textPos = new Vector2(
            rect.Left + (rect.Width - textSize.X) / 2f,
            rect.Top + (rect.Height - textSize.Y) / 2f);
        var textColor = IsEnabled ? TextColor : TextColor * 0.5f;
        textRenderer.DrawString(spriteBatch, Text, textPos, textColor);
    }

    private Rectangle GetBounds(Rectangle contentBounds)
    {
        var x = (int)(contentBounds.Left + RelativePosition.X);
        var y = (int)(contentBounds.Top + RelativePosition.Y);
        return new Rectangle(x, y, Size.X, Size.Y);
    }
}
