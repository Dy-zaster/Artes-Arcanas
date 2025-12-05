namespace Laa.Content.Core.Monsters;

public sealed record MonsterDamage(
    byte Base,
    byte Bonus,
    byte DamageType,
    byte NameCode
);
