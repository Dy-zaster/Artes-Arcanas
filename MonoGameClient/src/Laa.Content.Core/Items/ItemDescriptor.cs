namespace Laa.Content.Core.Items;

public sealed record ItemDescriptor(
    ushort Cost,
    sbyte Damage1Blunt,
    sbyte Damage1Pierce,
    sbyte Damage2Blunt,
    sbyte Damage2Pierce,
    byte ForbiddenRaces,
    byte ForbiddenClasses,
    sbyte DefenseModifier,
    byte WeaponWeight,
    byte WeaponType,
    byte RangeType,
    byte CraftDiscipline,
    byte RequiredTool,
    byte CraftedQuantity,
    byte CrafterLevel,
    IReadOnlyList<byte> RequiredResources,
    IReadOnlyList<byte> RequiredAmounts,
    byte RepairType,
    byte MinimumLevel,
    int Flags,
    byte AnimationType,
    byte Reserved1,
    ushort Reserved2
);
