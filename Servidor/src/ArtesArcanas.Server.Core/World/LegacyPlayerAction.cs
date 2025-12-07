namespace ArtesArcanas.Server.Core.World;

public enum LegacyPlayerActionType
{
    Unknown = 0,
    MoveStep,
    MoveToCoordinate,
    FollowEntity,
    AttackOffensive,
    AttackDefensive,
    CastSpellSingle,
    CastSpellContinuous,
    CastSpellOnInventoryItem,
    CommandFollowersAttack,
    CommandFollowersFollow,
    CommandFollowersStop,
    ConsumeInventoryItem,
    UseInventoryItem,
    CraftInventoryItem,
    DropInventoryItem,
    InspectGroundItems,
    PickSpecificGroundItem,
    PickAllGroundItems,
    WithdrawMoney,
    SensorClick
}

public readonly record struct LegacyPlayerAction(
    LegacyPlayerActionType Type,
    byte Opcode,
    byte PrimaryByte,
    ushort PrimaryWord,
    byte SecondaryByte,
    byte[] RawPayload)
{
    public static LegacyPlayerAction MoveStep(byte direction, byte[] payload, byte opcode = (byte)'m') =>
        new(LegacyPlayerActionType.MoveStep, opcode, direction, 0, 0, payload);

    public static LegacyPlayerAction MoveToCoordinate(ushort destination, byte[] payload, byte opcode = (byte)'M') =>
        new(LegacyPlayerActionType.MoveToCoordinate, opcode, 0, destination, 0, payload);

    public static LegacyPlayerAction FollowEntity(ushort target, byte[] payload) =>
        new(LegacyPlayerActionType.FollowEntity, (byte)'W', 0, target, 0, payload);

    public static LegacyPlayerAction Attack(byte opcode, ushort target, byte[] payload, bool defensive) =>
        new(defensive ? LegacyPlayerActionType.AttackDefensive : LegacyPlayerActionType.AttackOffensive, opcode, 0, target, 0, payload);

    public static LegacyPlayerAction CastSpell(byte opcode, ushort target, byte[] payload, bool continuous) =>
        new(continuous ? LegacyPlayerActionType.CastSpellContinuous : LegacyPlayerActionType.CastSpellSingle, opcode, 0, target, 0, payload);

    public static LegacyPlayerAction CastSpellOnItem(byte slot, byte[] payload) =>
        new(LegacyPlayerActionType.CastSpellOnInventoryItem, (byte)'J', slot, 0, 0, payload);

    public static LegacyPlayerAction CommandFollowers(byte subCommand, LegacyPlayerActionType type, ushort argument, byte[] payload) =>
        new(type, (byte)'O', 0, argument, subCommand, payload);

    public static LegacyPlayerAction ConsumeItem(byte slot, byte[] payload) =>
        new(LegacyPlayerActionType.ConsumeInventoryItem, (byte)'c', slot, 0, 0, payload);

    public static LegacyPlayerAction UseItem(byte slot, byte[] payload) =>
        new(LegacyPlayerActionType.UseInventoryItem, (byte)'u', slot, 0, 0, payload);

    public static LegacyPlayerAction CraftItem(byte recipe, byte amount, byte[] payload) =>
        new(LegacyPlayerActionType.CraftInventoryItem, (byte)'F', recipe, 0, amount, payload);

    public static LegacyPlayerAction DropItem(byte slot, byte amount, byte[] payload) =>
        new(LegacyPlayerActionType.DropInventoryItem, (byte)'S', slot, 0, amount, payload);

    public static LegacyPlayerAction InspectGround(byte[] payload) =>
        new(LegacyPlayerActionType.InspectGroundItems, (byte)'R', 0, 0, 0, payload);

    public static LegacyPlayerAction PickSpecific(byte slot, byte amount, byte[] payload) =>
        new(LegacyPlayerActionType.PickSpecificGroundItem, (byte)'r', slot, 0, amount, payload);

    public static LegacyPlayerAction PickAll(byte[] payload) =>
        new(LegacyPlayerActionType.PickAllGroundItems, (byte)'a', 0, 0, 0, payload);

    public static LegacyPlayerAction WithdrawMoney(int amount, byte[] payload) =>
        new(LegacyPlayerActionType.WithdrawMoney, (byte)'$', 0, (ushort)(amount & 0xFFFF), (byte)((amount >> 16) & 0xFF), payload);

    public static LegacyPlayerAction SensorClick(byte tileY, byte tileX, byte[] payload) =>
        new(LegacyPlayerActionType.SensorClick, (byte)'s', tileY, 0, tileX, payload);

    public int GetCombinedAmount() => PrimaryWord | (SecondaryByte << 16);
}
