using Laa.Monogame.Client.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Laa.Monogame.Client.UI;

public interface IUiWidget
{
    void Update(GameTime gameTime, MouseState currentMouse, MouseState previousMouse, Rectangle contentBounds);

    void Draw(SpriteBatch spriteBatch, DebugTextRenderer textRenderer, Rectangle contentBounds);
}
