namespace LegacyDataExtractor.Models;

public sealed record MonsterDamage(
    byte Base,
    byte Bonus,
    byte DamageType,
    byte NameCode
);
