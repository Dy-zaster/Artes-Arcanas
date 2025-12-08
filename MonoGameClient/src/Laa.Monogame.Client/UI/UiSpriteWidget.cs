using Laa.Monogame.Client.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Laa.Monogame.Client.UI;

public sealed class UiSpriteWidget : IUiWidget
{
    private readonly UiSpriteLibrary _library;

    public UiSpriteWidget(UiSpriteLibrary library, string spriteKey)
    {
        _library = library ?? throw new ArgumentNullException(nameof(library));
        SpriteKey = spriteKey ?? throw new ArgumentNullException(nameof(spriteKey));
    }

    public string SpriteKey { get; set; }

    public Vector2 Offset { get; set; } = Vector2.Zero;

    public Vector2? DesiredSize { get; set; }

    public bool FillContentBounds { get; set; }

    public Color Tint { get; set; } = Color.White;

    public void Update(GameTime gameTime, MouseState currentMouse, MouseState previousMouse, Rectangle contentBounds)
    {
        // Static sprite, no interaction yet.
    }

    public void Draw(SpriteBatch spriteBatch, DebugTextRenderer textRenderer, Rectangle contentBounds)
    {
        if (spriteBatch is null) throw new ArgumentNullException(nameof(spriteBatch));
        if (!_library.TryGetSprite(SpriteKey, out var sprite))
        {
            return;
        }

        var destination = BuildDestinationRectangle(contentBounds, sprite);
        spriteBatch.Draw(
            sprite.Texture,
            destination,
            sprite.Source,
            Tint);
    }

    private Rectangle BuildDestinationRectangle(Rectangle contentBounds, UiSprite sprite)
    {
        if (FillContentBounds)
        {
            var x = (int)Math.Floor(contentBounds.Left + Offset.X);
            var y = (int)Math.Floor(contentBounds.Top + Offset.Y);
            var width = (int)Math.Ceiling(DesiredSize?.X ?? contentBounds.Width);
            var height = (int)Math.Ceiling(DesiredSize?.Y ?? contentBounds.Height);
            return new Rectangle(x, y, Math.Max(1, width), Math.Max(1, height));
        }

        var origin = new Vector2(contentBounds.Left, contentBounds.Top) + Offset;
        var size = DesiredSize ?? new Vector2(sprite.Source.Width, sprite.Source.Height);
        return new Rectangle(
            (int)Math.Floor(origin.X),
            (int)Math.Floor(origin.Y),
            Math.Max(1, (int)Math.Ceiling(size.X)),
            Math.Max(1, (int)Math.Ceiling(size.Y)));
    }
}
