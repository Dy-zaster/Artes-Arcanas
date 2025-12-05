using System.Collections.Generic;
using System.IO;
using LegacyDataExtractor.Models;

namespace LegacyDataExtractor.Parsing;

public sealed class MonsterFileParser
{
    private const int DamageSlots = 3; // MAX_TIPOS_ATAQUE_MONSTRUO + 1

    public MonsterFile Parse(string path)
    {
        using var stream = File.OpenRead(path);
        using var reader = new LegacyBinaryReader(stream);

        var monsters = new List<MonsterDescriptor>();
        while (reader.BaseStream.Position < reader.BaseStream.Length)
        {
            var typeId = reader.ReadByte();
            if (reader.BaseStream.Position >= reader.BaseStream.Length)
            {
                break;
            }

            var name = reader.ReadShortString(31);
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

            var damages = new List<MonsterDamage>(DamageSlots);
            for (var slot = 0; slot < DamageSlots; slot++)
            {
                var baseDamage = reader.ReadByte();
                var bonusDamage = reader.ReadByte();
                var damageType = reader.ReadByte();
                var nameCode = reader.ReadByte();
                damages.Add(new MonsterDamage(baseDamage, bonusDamage, damageType, nameCode));
            }

            var primaryTreasure = reader.ReadByte();
            var visibility = reader.ReadByte();
            var movementIndex = reader.ReadByte();
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

            monsters.Add(new MonsterDescriptor(
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
                movementIndex,
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
                reserved,
                typeId
            ));
        }

        return new MonsterFile(monsters);
    }
}
