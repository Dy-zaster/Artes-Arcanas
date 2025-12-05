using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Laa.Monogame.Client.Rendering;

/// <summary>
/// Handles basic keyboard + mouse input so the camera can pan/zoom over the map.
/// </summary>
public sealed class CameraController
{
    private const float MoveSpeedPixelsPerSecond = 400f;
    private const float ZoomStepPerWheelNotch = 0.2f;

    private readonly Camera2D _camera;
    private int _previousScrollValue;

    public CameraController(Camera2D camera)
    {
        _camera = camera;
    }

    public void Update(GameTime gameTime, KeyboardState keyboardState, MouseState mouseState)
    {
        var elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
        var moveDirection = Vector2.Zero;

        if (keyboardState.IsKeyDown(Keys.W) || keyboardState.IsKeyDown(Keys.Up))
        {
            moveDirection.Y -= 1f;
        }

        if (keyboardState.IsKeyDown(Keys.S) || keyboardState.IsKeyDown(Keys.Down))
        {
            moveDirection.Y += 1f;
        }

        if (keyboardState.IsKeyDown(Keys.A) || keyboardState.IsKeyDown(Keys.Left))
        {
            moveDirection.X -= 1f;
        }

        if (keyboardState.IsKeyDown(Keys.D) || keyboardState.IsKeyDown(Keys.Right))
        {
            moveDirection.X += 1f;
        }

        if (moveDirection.LengthSquared() > float.Epsilon)
        {
            moveDirection.Normalize();
            _camera.Move(moveDirection * MoveSpeedPixelsPerSecond * elapsed);
        }

        HandleZoom(mouseState, keyboardState);
    }

    private void HandleZoom(MouseState mouseState, KeyboardState keyboardState)
    {
        var scrollDelta = mouseState.ScrollWheelValue - _previousScrollValue;
        if (scrollDelta != 0)
        {
            var zoomDelta = (scrollDelta / 120f) * ZoomStepPerWheelNotch;
            _camera.Zoom(zoomDelta);
        }

        _previousScrollValue = mouseState.ScrollWheelValue;

        if (keyboardState.IsKeyDown(Keys.OemPlus) || keyboardState.IsKeyDown(Keys.Add))
        {
            _camera.Zoom(ZoomStepPerWheelNotch * 0.5f);
        }

        if (keyboardState.IsKeyDown(Keys.OemMinus) || keyboardState.IsKeyDown(Keys.Subtract))
        {
            _camera.Zoom(-ZoomStepPerWheelNotch * 0.5f);
        }
    }
}
