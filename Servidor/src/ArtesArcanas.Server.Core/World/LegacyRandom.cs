using System;

namespace ArtesArcanas.Server.Core.World;

internal static class LegacyRandom
{
    private static readonly Random _random = new();
    private static readonly object _sync = new();

    public static int Next(int minValue, int maxValue)
    {
        lock (_sync)
        {
            return _random.Next(minValue, maxValue);
        }
    }
}
