using System;
using Microsoft.Xna.Framework;

namespace Laa.Monogame.Client.Rendering;

/// <summary>
/// Lightweight 2D camera that exposes translation/zoom with simple bounds.
/// </summary>
public sealed class Camera2D
{
    private const float MinZoom = 0.1f;
    private const float MaxZoom = 4f;

    private Vector2 _position;
    private float _zoom = 1f;
    private bool _hasWorldSize;
    private float _worldWidth;
    private float _worldHeight;

    public Camera2D(int viewportWidth, int viewportHeight)
    {
        if (viewportWidth <= 0) throw new ArgumentOutOfRangeException(nameof(viewportWidth));
        if (viewportHeight <= 0) throw new ArgumentOutOfRangeException(nameof(viewportHeight));

        ViewportWidth = viewportWidth;
        ViewportHeight = viewportHeight;
    }

    public int ViewportWidth { get; private set; }

    public int ViewportHeight { get; private set; }

    public Vector2 Position => _position;

    public float ZoomFactor => _zoom;

    public Matrix GetViewMatrix()
    {
        return Matrix.CreateTranslation(new Vector3(-_position, 0f)) *
               Matrix.CreateScale(new Vector3(_zoom, _zoom, 1f));
    }

    public void Move(Vector2 delta)
    {
        _position += delta;
        ClampPosition();
    }

    public void Zoom(float delta)
    {
        var target = MathHelper.Clamp(_zoom + delta, MinZoom, MaxZoom);
        if (Math.Abs(target - _zoom) < float.Epsilon)
        {
            return;
        }

        var viewportCenter = GetViewportCenterWorld();
        _zoom = target;
        CenterOn(viewportCenter);
    }

    public void CenterOn(Vector2 worldPoint)
    {
        var halfWidth = ViewportWidth / (2f * _zoom);
        var halfHeight = ViewportHeight / (2f * _zoom);
        _position = worldPoint - new Vector2(halfWidth, halfHeight);
        ClampPosition();
    }

    public void ResizeViewport(int width, int height)
    {
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));

        ViewportWidth = width;
        ViewportHeight = height;
        ClampPosition();
    }

    public void SetWorldSize(float width, float height)
    {
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));

        _worldWidth = width;
        _worldHeight = height;
        _hasWorldSize = true;
        ClampPosition();
    }

    private Vector2 GetViewportCenterWorld()
    {
        return _position + new Vector2(ViewportWidth / (2f * _zoom), ViewportHeight / (2f * _zoom));
    }

    private void ClampPosition()
    {
        if (!_hasWorldSize)
        {
            return;
        }

        var visibleWidth = ViewportWidth / _zoom;
        var visibleHeight = ViewportHeight / _zoom;

        var maxX = Math.Max(0f, _worldWidth - visibleWidth);
        var maxY = Math.Max(0f, _worldHeight - visibleHeight);

        var clampedX = MathHelper.Clamp(_position.X, 0f, maxX);
        var clampedY = MathHelper.Clamp(_position.Y, 0f, maxY);
        _position = new Vector2(clampedX, clampedY);
    }
}
