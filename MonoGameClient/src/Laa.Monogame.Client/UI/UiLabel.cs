using Laa.Monogame.Client.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Laa.Monogame.Client.UI;

public sealed class UiLabel : IUiWidget
{
    public UiLabel(string text, Vector2 relativePosition, Color? color = null)
    {
        Text = text ?? string.Empty;
        RelativePosition = relativePosition;
        Color = color ?? Color.White;
    }

    public string Text { get; set; }

    public Vector2 RelativePosition { get; set; }

    public Color Color { get; set; }

    public void Update(GameTime gameTime, MouseState currentMouse, MouseState previousMouse, Rectangle contentBounds)
    {
        // Labels are static placeholders; future widgets can override if needed.
    }

    public void Draw(SpriteBatch spriteBatch, DebugTextRenderer textRenderer, Rectangle contentBounds)
    {
        if (spriteBatch is null) throw new ArgumentNullException(nameof(spriteBatch));
        if (textRenderer is null) throw new ArgumentNullException(nameof(textRenderer));

        var position = new Vector2(contentBounds.Left, contentBounds.Top) + RelativePosition;
        textRenderer.DrawString(spriteBatch, Text, position, Color);
    }
}
