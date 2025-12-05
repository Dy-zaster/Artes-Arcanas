namespace Laa.Content.Core.Maps;

public sealed record SensorRecord(
    byte Type,
    byte X,
    byte Y,
    byte Key1,
    byte Key2,
    byte Data1,
    byte Data2,
    byte Data3,
    byte Data4,
    byte Flags,
    string Text
);
