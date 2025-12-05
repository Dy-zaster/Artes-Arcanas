using System.Collections.Generic;

namespace Laa.Content.Core.Monsters;

public sealed record MonsterDescriptor(
    string Name,
    ushort TerrainFlags,
    byte Level,
    byte Alignment,
    int Resistances,
    byte Defense,
    byte SecondaryTreasure,
    byte AttackLevel,
    byte Behavior,
    byte Regeneration,
    byte TreasureModifier,
    ushort Experience,
    IReadOnlyList<MonsterDamage> Damages,
    byte PrimaryTreasure,
    byte Visibility,
    byte MovementIndex,
    byte DeathStyle,
    byte Size,
    byte RandomTreasure,
    byte AnimationStyle,
    byte DeathOutcome,
    int CastableSpells,
    byte AttackInterval,
    byte TreasureModifier2,
    ushort AverageHp,
    int SkillMask,
    int Reserved,
    byte TypeId
);
