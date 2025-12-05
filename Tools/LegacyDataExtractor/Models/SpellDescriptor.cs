namespace LegacyDataExtractor.Models;

public sealed record SpellDescriptor(
    ushort Cost,
    byte Flags,
    byte Type,
    byte RequiredIntelligence,
    byte RequiredWisdom,
    byte RequiredMana,
    byte BaseDamage,
    byte BonusDamage,
    byte DamageType,
    byte AnimationId,
    byte ScrollIcon,
    byte RequiredPlayerLevel,
    byte School,
    ushort Reserved
);
