using System;

namespace ArtesArcanas.Server.Core.Legacy;

internal static class LegacyTime
{
    private static readonly DateTime Epoch = new(1899, 12, 30, 0, 0, 0, DateTimeKind.Utc);
    private const int DayOffset = 30000;

    public static ushort GetCurrentDayCode()
    {
        var utc = DateTime.UtcNow.Date;
        var days = (int)(utc - Epoch).TotalDays - DayOffset;
        if (days < 0)
        {
            days = 0;
        }
        else if (days > ushort.MaxValue)
        {
            days = ushort.MaxValue;
        }

        return (ushort)days;
    }
}
