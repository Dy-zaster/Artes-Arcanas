using System;
using System.Collections.Generic;
using System.Text;
using Laa.Monogame.Client.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Laa.Monogame.Client.UI;

public sealed class UiTextInputWidget : IUiWidget
{
    private bool _isFocused;
    private KeyboardState _previousKeyboard;

    public UiTextInputWidget(Vector2 relativePosition, Point size, string placeholder = "", bool isPassword = false)
    {
        RelativePosition = relativePosition;
        Size = size;
        Placeholder = placeholder ?? string.Empty;
        IsPassword = isPassword;
    }

    public Vector2 RelativePosition { get; set; }

    public Point Size { get; set; }

    public string Text { get; set; } = string.Empty;

    public string Placeholder { get; set; }

    public bool IsPassword { get; set; }

    public Color BackgroundColor { get; set; } = new Color(0f, 0f, 0f, 0.7f);

    public Color BorderColor { get; set; } = Color.DimGray;

    public Color TextColor { get; set; } = Color.White;

    public Color PlaceholderColor { get; set; } = Color.Gray;

    public void Update(GameTime gameTime, MouseState currentMouse, MouseState previousMouse, Rectangle contentBounds)
    {
        var rect = GetBounds(contentBounds);
        var mousePoint = new Point(currentMouse.X, currentMouse.Y);
        var justClicked = currentMouse.LeftButton == ButtonState.Pressed && previousMouse.LeftButton == ButtonState.Released;
        if (justClicked)
        {
            _isFocused = rect.Contains(mousePoint);
        }

        var keyboard = Keyboard.GetState();
        if (_isFocused)
        {
            AppendTypedCharacters(_previousKeyboard, keyboard);
        }

        _previousKeyboard = keyboard;
    }

    public void Draw(SpriteBatch spriteBatch, DebugTextRenderer textRenderer, Rectangle contentBounds)
    {
        if (spriteBatch is null) throw new ArgumentNullException(nameof(spriteBatch));
        if (textRenderer is null) throw new ArgumentNullException(nameof(textRenderer));

        var rect = GetBounds(contentBounds);
        spriteBatch.Draw(textRenderer.PixelTexture, rect, BackgroundColor);
        DrawBorder(spriteBatch, textRenderer.PixelTexture, rect, 1, BorderColor);

        var displayText = string.IsNullOrEmpty(Text)
            ? Placeholder
            : (IsPassword ? new string('*', Text.Length) : Text);
        var color = string.IsNullOrEmpty(Text) ? PlaceholderColor : TextColor;
        var textPos = new Vector2(rect.Left + 6, rect.Top + 4);
        textRenderer.DrawString(spriteBatch, displayText, textPos, color);
    }

    private Rectangle GetBounds(Rectangle contentBounds)
    {
        var x = (int)(contentBounds.Left + RelativePosition.X);
        var y = (int)(contentBounds.Top + RelativePosition.Y);
        return new Rectangle(x, y, Size.X, Size.Y);
    }

    private void AppendTypedCharacters(KeyboardState previous, KeyboardState current)
    {
        foreach (var key in current.GetPressedKeys())
        {
            if (previous.IsKeyDown(key))
            {
                continue;
            }

            if (key == Keys.Back)
            {
                if (Text.Length > 0)
                {
                    Text = Text[..^1];
                }
                continue;
            }

            if (KeyToChar(key, current, out var ch))
            {
                Text += ch;
            }
        }
    }

    private static bool KeyToChar(Keys key, KeyboardState state, out char ch)
    {
        ch = '\0';
        var shift = state.IsKeyDown(Keys.LeftShift) || state.IsKeyDown(Keys.RightShift);

        if (key >= Keys.A && key <= Keys.Z)
        {
            var baseChar = (char)('a' + (key - Keys.A));
            ch = shift ? char.ToUpperInvariant(baseChar) : baseChar;
            return true;
        }

        if (key >= Keys.D0 && key <= Keys.D9)
        {
            ch = shift ? GetShiftedDigitChar(key) : (char)('0' + (key - Keys.D0));
            return true;
        }

        if (key >= Keys.NumPad0 && key <= Keys.NumPad9)
        {
            ch = (char)('0' + (key - Keys.NumPad0));
            return true;
        }

        return key switch
        {
            Keys.Space => Assign(' ', out ch),
            Keys.OemMinus => Assign(shift ? '_' : '-', out ch),
            Keys.OemPeriod => Assign('.', out ch),
            Keys.OemComma => Assign(',', out ch),
            Keys.OemPlus => Assign('+', out ch),
            Keys.OemQuestion => Assign('?', out ch),
            Keys.OemSemicolon => Assign(shift ? ':' : ';', out ch),
            _ => false
        };
    }

    private static bool Assign(char value, out char ch)
    {
        ch = value;
        return true;
    }

    private static char GetShiftedDigitChar(Keys key)
    {
        return key switch
        {
            Keys.D1 => '!',
            Keys.D2 => '@',
            Keys.D3 => '#',
            Keys.D4 => '$',
            Keys.D5 => '%',
            Keys.D6 => '^',
            Keys.D7 => '&',
            Keys.D8 => '*',
            Keys.D9 => '(',
            Keys.D0 => ')',
            _ => '?'
        };
    }

    private static void DrawBorder(SpriteBatch spriteBatch, Texture2D pixel, Rectangle bounds, int thickness, Color color)
    {
        spriteBatch.Draw(pixel, new Rectangle(bounds.Left, bounds.Top, bounds.Width, thickness), color);
        spriteBatch.Draw(pixel, new Rectangle(bounds.Left, bounds.Bottom - thickness, bounds.Width, thickness), color);
        spriteBatch.Draw(pixel, new Rectangle(bounds.Left, bounds.Top, thickness, bounds.Height), color);
        spriteBatch.Draw(pixel, new Rectangle(bounds.Right - thickness, bounds.Top, thickness, bounds.Height), color);
    }
}
