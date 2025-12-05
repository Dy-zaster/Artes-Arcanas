namespace LegacyDataExtractor.Models;

public sealed record NestRecord(
    byte Type,
    byte X,
    byte Y,
    byte Quantity
);
