using System.Collections.Generic;

namespace ArtesArcanas.Server.Core.Legacy;

public sealed record LegacyMonsterDamage(byte BaseDamage, byte BonusDamage, byte DamageType, byte NameCode);

public sealed class LegacyMonsterDescriptor
{
    public LegacyMonsterDescriptor(
        byte typeId,
        string name,
        ushort terrainFlags,
        byte level,
        byte alignment,
        int resistances,
        byte defense,
        byte secondaryTreasure,
        byte attackLevel,
        byte behavior,
        byte regeneration,
        byte treasureModifier,
        ushort experience,
        IReadOnlyList<LegacyMonsterDamage> damages,
        byte primaryTreasure,
        byte visibility,
        byte movement,
        byte deathStyle,
        byte size,
        byte randomTreasure,
        byte animationStyle,
        byte deathOutcome,
        int castableSpells,
        byte attackInterval,
        byte treasureModifier2,
        ushort averageHp,
        int skillMask,
        int reserved)
    {
        TypeId = typeId;
        Name = name;
        TerrainFlags = terrainFlags;
        Level = level;
        Alignment = alignment;
        Resistances = resistances;
        Defense = defense;
        SecondaryTreasure = secondaryTreasure;
        AttackLevel = attackLevel;
        Behavior = behavior;
        Regeneration = regeneration;
        TreasureModifier = treasureModifier;
        Experience = experience;
        Damages = damages;
        PrimaryTreasure = primaryTreasure;
        Visibility = visibility;
        Movement = movement;
        DeathStyle = deathStyle;
        Size = size;
        RandomTreasure = randomTreasure;
        AnimationStyle = animationStyle;
        DeathOutcome = deathOutcome;
        CastableSpells = castableSpells;
        AttackInterval = attackInterval;
        TreasureModifier2 = treasureModifier2;
        AverageHp = averageHp;
        SkillMask = skillMask;
        Reserved = reserved;
    }

    public byte TypeId { get; }
    public string Name { get; }
    public ushort TerrainFlags { get; }
    public byte Level { get; }
    public byte Alignment { get; }
    public int Resistances { get; }
    public byte Defense { get; }
    public byte SecondaryTreasure { get; }
    public byte AttackLevel { get; }
    public byte Behavior { get; }
    public byte Regeneration { get; }
    public byte TreasureModifier { get; }
    public ushort Experience { get; }
    public IReadOnlyList<LegacyMonsterDamage> Damages { get; }
    public byte PrimaryTreasure { get; }
    public byte Visibility { get; }
    public byte Movement { get; }
    public byte DeathStyle { get; }
    public byte Size { get; }
    public byte RandomTreasure { get; }
    public byte AnimationStyle { get; }
    public byte DeathOutcome { get; }
    public int CastableSpells { get; }
    public byte AttackInterval { get; }
    public byte TreasureModifier2 { get; }
    public ushort AverageHp { get; }
    public int SkillMask { get; }
    public int Reserved { get; }
}
