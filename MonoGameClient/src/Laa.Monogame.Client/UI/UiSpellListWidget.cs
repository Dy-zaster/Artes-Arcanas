using Laa.Monogame.Client.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Laa.Monogame.Client.UI;

public sealed record UiSpellGroup(string Title, IReadOnlyList<string> Entries);

public sealed class UiSpellListWidget : IUiWidget
{
    private readonly List<UiSpellGroup> _groups = new();

    public Color HeaderColor { get; set; } = Color.LightBlue;

    public Color EntryColor { get; set; } = Color.White;

    public float LineHeight { get; set; } = 20f;

    public float Indent { get; set; } = 16f;

    public void SetGroups(IEnumerable<UiSpellGroup> groups)
    {
        _groups.Clear();
        if (groups is null)
        {
            return;
        }

        _groups.AddRange(groups);
    }

    public void Update(GameTime gameTime, MouseState currentMouse, MouseState previousMouse, Rectangle contentBounds)
    {
        // Static list; scrollbars can be added later.
    }

    public void Draw(SpriteBatch spriteBatch, DebugTextRenderer textRenderer, Rectangle contentBounds)
    {
        if (_groups.Count == 0)
        {
            textRenderer.DrawString(
                spriteBatch,
                "NO SPELL DATA AVAILABLE",
                new Vector2(contentBounds.Left, contentBounds.Top),
                EntryColor);
            return;
        }

        var cursor = new Vector2(contentBounds.Left, contentBounds.Top);
        foreach (var group in _groups)
        {
            if (cursor.Y + LineHeight > contentBounds.Bottom)
            {
                break;
            }

            textRenderer.DrawString(
                spriteBatch,
                group.Title.ToUpperInvariant(),
                cursor,
                HeaderColor);
            cursor.Y += LineHeight;

            foreach (var entry in group.Entries)
            {
                if (cursor.Y + LineHeight > contentBounds.Bottom)
                {
                    break;
                }

                textRenderer.DrawString(
                    spriteBatch,
                    entry.ToUpperInvariant(),
                    cursor + new Vector2(Indent, 0f),
                    EntryColor);
                cursor.Y += LineHeight;
            }

            cursor.Y += LineHeight * 0.5f;
            if (cursor.Y > contentBounds.Bottom)
            {
                break;
            }
        }
    }
}
