using System;
using Laa.Content.Core.Animations;
using Microsoft.Xna.Framework;

namespace Laa.Monogame.Client.Rendering;

internal static class AnimationRenderHelper
{
    public static Vector2 CalculateDrawPosition(Vector2 anchor, AnimationDirectionSlice direction, AnimationFrameSlice frame, bool mirror)
    {
        var x = anchor.X;
        var y = anchor.Y;

        if (mirror)
        {
            x += direction.OffsetX - frame.CenterX - frame.Source.Width;
        }
        else
        {
            x += frame.CenterX - direction.OffsetX;
        }

        y += frame.CenterY - direction.OffsetY;
        return new Vector2(x, y);
    }

    public static int FindPlayableFrameIndex(AnimationDirectionSlice direction)
    {
        if (direction.Frames.Count == 0)
        {
            return -1;
        }

        for (var i = 0; i < direction.Frames.Count; i++)
        {
            if (direction.Frames[i].Source != Rectangle.Empty)
            {
                return i;
            }
        }

        return -1;
    }
}
