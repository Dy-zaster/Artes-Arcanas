using System;
using System.Collections.Generic;
using System.IO;

namespace ArtesArcanas.Server.Core.Legacy;

public sealed class LegacyMonsterCatalog
{
    private const int DamageSlotCount = 3;
    private readonly IReadOnlyDictionary<byte, LegacyMonsterDescriptor> _descriptors;

    private LegacyMonsterCatalog(IReadOnlyDictionary<byte, LegacyMonsterDescriptor> descriptors)
    {
        _descriptors = descriptors;
    }

    public static LegacyMonsterCatalog Empty { get; } =
        new LegacyMonsterCatalog(new Dictionary<byte, LegacyMonsterDescriptor>());

    public int Count => _descriptors.Count;

    public static LegacyMonsterCatalog Load(string path)
    {
        if (!File.Exists(path))
        {
            return Empty;
        }

        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var reader = new BinaryReader(stream, LegacyConstants.LegacyEncoding, leaveOpen: false);

        var descriptors = new Dictionary<byte, LegacyMonsterDescriptor>();
        while (reader.BaseStream.Position < reader.BaseStream.Length)
        {
            var typeId = reader.ReadByte();
            if (reader.BaseStream.Position >= reader.BaseStream.Length)
            {
                break;
            }

            var name = ReadShortString(reader, 31);
            var terrain = reader.ReadUInt16();
            var level = reader.ReadByte();
            var alignment = reader.ReadByte();
            var resistances = reader.ReadInt32();
            var defense = reader.ReadByte();
            var treasure2 = reader.ReadByte();
            var attackLevel = reader.ReadByte();
            var behavior = reader.ReadByte();
            var regeneration = reader.ReadByte();
            var treasureModifier = reader.ReadByte();
            var experience = reader.ReadUInt16();

            var damages = new List<LegacyMonsterDamage>(DamageSlotCount);
            for (var slot = 0; slot < DamageSlotCount; slot++)
            {
                var baseDamage = reader.ReadByte();
                var bonusDamage = reader.ReadByte();
                var damageType = reader.ReadByte();
                var nameCode = reader.ReadByte();
                damages.Add(new LegacyMonsterDamage(baseDamage, bonusDamage, damageType, nameCode));
            }

            var primaryTreasure = reader.ReadByte();
            var visibility = reader.ReadByte();
            var movement = reader.ReadByte();
            var deathStyle = reader.ReadByte();
            var size = reader.ReadByte();
            var randomTreasure = reader.ReadByte();
            var animationStyle = reader.ReadByte();
            var deathOutcome = reader.ReadByte();
            var castableSpells = reader.ReadInt32();
            var attackInterval = reader.ReadByte();
            var treasureModifier2 = reader.ReadByte();
            var averageHp = reader.ReadUInt16();
            var skillMask = reader.ReadInt32();
            var reserved = reader.ReadInt32();

            var descriptor = new LegacyMonsterDescriptor(
                typeId,
                name,
                terrain,
                level,
                alignment,
                resistances,
                defense,
                treasure2,
                attackLevel,
                behavior,
                regeneration,
                treasureModifier,
                experience,
                damages,
                primaryTreasure,
                visibility,
                movement,
                deathStyle,
                size,
                randomTreasure,
                animationStyle,
                deathOutcome,
                castableSpells,
                attackInterval,
                treasureModifier2,
                averageHp,
                skillMask,
                reserved);

            descriptors[typeId] = descriptor;
        }

        return new LegacyMonsterCatalog(descriptors);
    }

    public bool TryGetDescriptor(byte typeId, out LegacyMonsterDescriptor? descriptor)
    {
        if (_descriptors.TryGetValue(typeId, out var existing))
        {
            descriptor = existing;
            return true;
        }

        descriptor = null;
        return false;
    }

    private static string ReadShortString(BinaryReader reader, int capacity)
    {
        var declaredLength = reader.ReadByte();
        var raw = reader.ReadBytes(capacity);
        var actual = Math.Min((int)declaredLength, capacity);
        if (actual <= 0)
        {
            return string.Empty;
        }

        return LegacyConstants.LegacyEncoding.GetString(raw, 0, actual);
    }
}
