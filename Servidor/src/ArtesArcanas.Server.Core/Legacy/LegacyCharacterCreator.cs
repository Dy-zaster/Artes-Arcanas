using System;
using System.Security.Cryptography;
using ArtesArcanas.Server.Core.Configuration;

namespace ArtesArcanas.Server.Core.Legacy;

internal static class LegacyCharacterCreator
{
    private const int MaxSkillSum = 38;
    private const int MaxPerkCount = 3;
    private const int MaxExperienceRequirement = 65000;
    private const byte MaxRaceId = 6;
    private const byte MaxCategoryId = 7;
    private const byte MaxFood = 105;
    private const byte DefaultLevel = 1;
    private const byte MinCategoryLevel = 25;
    private const int BaseGold = 150;
    private const int MaxHitPoints = 8000;
    private static readonly byte[] ClassHighlightedSkills =
    {
        0 | (2 << 3), // Guerrero
        3 | (4 << 3), // Clerigo
        2 | (0 << 3), // Mago
        4 | (3 << 3), // Bribon
        4 | (3 << 3), // Montaraz
        0 | (2 << 3), // Paladin
        4 | (3 << 3), // Bardo
        2 | (3 << 3)  // Guerrero Mago
    };

    private static readonly byte[] RaceCategoryRestrictions =
    {
        0xC0, // Humano
        0x28, // Elfo
        0xD4, // Enano
        0xF0, // Gnomo
        0x00, // Semielfo
        0x74, // Orco
        0x72  // Drow
    };

    private static readonly ushort[] ClassDeniedPerks =
    {
        0x800B, // Guerrero
        0xD800, // Clerigo
        0x4830, // Mago
        0xC003, // Bribon
        0x001B, // Montaraz
        0x9003, // Paladin
        0x0000, // Bardo
        0x0000  // Guerrero Mago
    };

    private const byte SlotRightHand = 0;
    private const byte SlotLeftHand = 1;
    private const byte SlotArmor = 2;
    private const byte SlotHelmet = 3;
    private const byte SlotBracers = 4;
    private const byte SlotRing = 5;
    private const byte SlotAmulet = 6;
    private const byte SlotAmmo = 7;

    private const byte ItemDagger = 16;
    private const byte ItemFood = 144;
    private const byte ItemWater = 152;
    private const byte ItemBandages = 222;
    private const byte ItemWhetstone = 136;
    private const byte ItemFirewood = 209;
    private const byte ItemAntiVenom = 156;
    private const byte ItemManaPotion = 158;
    private const byte ItemHealthPotion = 159;
    private const byte ItemMagicWand = 124;
    private const byte ItemSling = 44;
    private const byte ItemProjectile = 52;
    private const byte ItemHabit = 72;
    private const byte ItemBasicArmor = 56;
    private const byte ItemMageRobe = 77;
    private const byte ItemWarriorArmor = 64;
    private const byte ItemSimpleHelmet = 88;
    private const byte ItemShield = 80;
    private const byte ItemMace = 34;
    private const byte ItemClericSymbol = 116;
    private const byte ItemShortSword = 18;

    private const byte GemVenom = 188;
    private const byte GemFire = 189;
    private const byte GemIce = 186;
    private const byte GemLightning = 187;

    internal static LegacyCreationStatus TryCreate(
        in LegacyCharacterCreationData data,
        string login,
        int remoteIp,
        ServerOptions options,
        LegacyGameData gameData,
        out LegacyUserAccount? account)
    {
        account = null;

        if (!ValidateCharacterData(data))
        {
            return LegacyCreationStatus.Denied;
        }

        var snapshot = BuildSnapshot(data, options, gameData);
        snapshot.SetAvatarName(data.AvatarName);
        var userData = BuildUserData(login, remoteIp, data.PasswordHash);
        account = new LegacyUserAccount(userData, snapshot.ToByteArray());
        return LegacyCreationStatus.Success;
    }

    private static bool ValidateCharacterData(in LegacyCharacterCreationData data)
    {
        if (data.Race > MaxRaceId || data.Category > MaxCategoryId)
        {
            return false;
        }

        if ((RaceCategoryRestrictions[data.Race] & (1 << data.Category)) != 0)
        {
            return false;
        }

        var totalSkills = data.Strength + data.Dexterity + data.Constitution + data.Wisdom + data.Intelligence;
        if (totalSkills != MaxSkillSum)
        {
            return false;
        }

        if (CountBits(data.Pericias) != MaxPerkCount)
        {
            return false;
        }

        if ((ClassDeniedPerks[data.Category] & data.Pericias) != 0)
        {
            return false;
        }

        return !string.IsNullOrWhiteSpace(data.AvatarName);
    }

    private static LegacyPlayerSnapshot BuildSnapshot(
        in LegacyCharacterCreationData data,
        ServerOptions options,
        LegacyGameData gameData)
    {
        var snapshot = new LegacyPlayerSnapshot
        {
            Activo = 1,
            TipoMonstruo = data.Race,
            CodigoNido = LegacyConstants.NoClanId,
            Categoria = data.Category,
            CodigoMapa = 0,
            Clan = LegacyConstants.NoClanId,
            RitmoDeVida = 0,
            Comida = MaxFood,
            Accion = 0,
            AccionAutomatica = 0,
            Direccion = 0,
            Duenno = ushort.MaxValue,
            AtaqueUtilizado = 0,
            PericiasDinamicas = 0,
            ObjetivoAtacado = ushort.MaxValue,
            ObjetivoASeguir = ushort.MaxValue,
            ControlMovimiento = 0,
            CoordXAnterior = 0,
            CoordYAnterior = 0,
            DestinoX = 0,
            DestinoY = 0,
            TurnosGastados = 0,
            Rostro = 0,
            NivelDeCategoria = MinCategoryLevel,
            Nivel = DefaultLevel,
            Experiencia = (ushort)Math.Min(MaxExperienceRequirement, DefaultLevel * (DefaultLevel + 1) * 100),
            Dinero = BaseGold,
            EspecialidadArma = 7,
            NivelEspecializacion = 0,
            HabilidadResaltada = ClassHighlightedSkills[data.Category],
            Fuerza = data.Strength,
            Destreza = data.Dexterity,
            Constitucion = data.Constitution,
            Inteligencia = data.Intelligence,
            Sabiduria = data.Wisdom,
            ConjuroElegido = DetermineInitialSpell(data.Category),
            Conjuros = 0,
            Pericias = data.Pericias,
            AurasExternas = 0,
            CapacidadIdentificacion = 0,
            Banderas = 0,
            ObjetivoAtaqueAutomatico = 0,
            MensajesEnviados = 0,
            QuestEnCurso = 0,
            QuestLogrado = 0,
            DineroOferta = 0,
            CodigoMonstruoOferta = 0,
            ObjetoOfertaId = 0,
            ObjetoOfertaMod = 0,
            IndiceObjetoOferta = 0,
            IndiceInflacionModificada = 0,
            TipoTransaccion = 0,
            CantidadObjetosOferta = 0
        };

        snapshot.Conjuros = 1u << snapshot.ConjuroElegido;
        snapshot.ObjetivoAtaqueAutomatico = ushort.MaxValue;

        var spawn = ResolveSpawnPosition(options, data.Race);
        snapshot.CodigoMapa = spawn.MapId;
        snapshot.CoordenadaX = spawn.X;
        snapshot.CoordenadaY = spawn.Y;
        snapshot.CoordXAnterior = spawn.X;
        snapshot.CoordYAnterior = spawn.Y;
        snapshot.DestinoX = spawn.X;
        snapshot.DestinoY = spawn.Y;

        snapshot.MaxHp = ComputeMaxHp(data.Category, data.Constitution, snapshot.Nivel);
        snapshot.Hp = snapshot.MaxHp;
        var (maxMana, meditation) = ComputeMana(data.Category, data.Intelligence, data.Wisdom, snapshot.Nivel);
        snapshot.MaxMana = maxMana;
        snapshot.Mana = maxMana;
        snapshot.Meditacion255 = meditation;
        snapshot.DannoBase = ComputeDamageBase(data.Strength);
        snapshot.NivelAtaque = ComputeAttackLevel(data.Category, data.Dexterity, snapshot.Nivel);
        snapshot.Defensa = ComputeDefense(data.Category, data.Dexterity, snapshot.Nivel);

        ApplySpecialty(ref snapshot, data.Category, data.Race);
        EquipInitialItems(ref snapshot, data, gameData.Items);
        snapshot.Rostro = SelectFace(data.Race, data.IsFemale);
        snapshot.Animacion = ComputeAnimation(gameData.AnimationMap, data.IsFemale, data.Race, data.Category, snapshot);
        snapshot.CapacidadIdentificacion = ResolveIdentificationCapacity(data.Category);
        return snapshot;
    }

    private static void ApplySpecialty(ref LegacyPlayerSnapshot snapshot, byte category, byte race)
    {
        if (category == 0)
        {
            snapshot.NivelEspecializacion = (byte)(snapshot.Nivel << 1);
            snapshot.EspecialidadArma = race switch
            {
                2 => 24,
                5 => 35,
                _ => 18
            };
        }
        else if (category == 3)
        {
            snapshot.NivelEspecializacion = (byte)(snapshot.Nivel << 1);
            snapshot.EspecialidadArma = 16;
        }
    }

    private static void EquipInitialItems(ref LegacyPlayerSnapshot snapshot, in LegacyCharacterCreationData data, LegacyItemCatalog items)
    {
        Span<LegacyArtifact> equipment = stackalloc LegacyArtifact[LegacyConstants.EquipmentSlots];
        equipment.Fill(LegacyArtifact.Empty);
        equipment[SlotRightHand] = new LegacyArtifact(ItemDagger, 40);
        equipment[SlotLeftHand] = LegacyArtifact.LeftHandEmpty;

        Span<LegacyArtifact> inventory = stackalloc LegacyArtifact[LegacyConstants.InventoryArtifactSlots];
        inventory.Fill(LegacyArtifact.Empty);
        inventory[0] = new LegacyArtifact(ItemFood, 5);
        inventory[1] = new LegacyArtifact(ItemWater, 5);
        inventory[2] = new LegacyArtifact(ItemBandages, 20);
        inventory[3] = new LegacyArtifact(ItemWhetstone, 50);
        inventory[4] = new LegacyArtifact(ItemFirewood, 40);
        inventory[5] = new LegacyArtifact(ItemAntiVenom, 25);

        switch (data.Category)
        {
            case 2: // Mago
            case 7: // Guerrero Mago
                equipment[SlotArmor] = new LegacyArtifact(ItemMageRobe, 40);
                inventory[6] = new LegacyArtifact(ItemMagicWand, 1);
                inventory[7] = new LegacyArtifact(ItemManaPotion, 10);
                break;
            case 3: // Bribon
            case 6: // Bardo
            case 4: // Montaraz
                equipment[SlotArmor] = new LegacyArtifact(ItemBasicArmor, 25);
                equipment[SlotAmmo] = new LegacyArtifact(ItemProjectile, 60);
                inventory[6] = new LegacyArtifact(ItemSling, 63);
                inventory[7] = new LegacyArtifact(ItemProjectile, 60);
                break;
            case 1: // Clerigo
                equipment[SlotArmor] = new LegacyArtifact(ItemHabit, 25);
                inventory[6] = new LegacyArtifact(ItemMace, 40);
                inventory[7] = new LegacyArtifact(ItemClericSymbol, 1);
                break;
            case 0: // Guerrero
                equipment[SlotLeftHand] = new LegacyArtifact(ItemShield, 30);
                equipment[SlotArmor] = new LegacyArtifact(ItemWarriorArmor, 10);
                equipment[SlotHelmet] = new LegacyArtifact(ItemSimpleHelmet, 30);
                inventory[6] = new LegacyArtifact(ItemShortSword, 30);
                inventory[7] = new LegacyArtifact(ItemHealthPotion, 10);
                break;
            default:
                break;
        }

        WriteArtifacts(ref snapshot, equipment, true);
        WriteArtifacts(ref snapshot, inventory, false);
        ComputeDefenseModifiers(ref snapshot, equipment, items);
    }

    private static void ComputeDefenseModifiers(ref LegacyPlayerSnapshot snapshot, ReadOnlySpan<LegacyArtifact> equipment, LegacyItemCatalog items)
    {
        var modifier = 0;
        Span<int> armor = stackalloc int[8];
        armor.Clear();
        var magicalBonus = 0;

        for (var slot = SlotArmor; slot <= SlotRing; slot++)
        {
            modifier += CalculateDefenseModifier(equipment[slot], items);
        }

        for (var slot = SlotArmor; slot <= SlotRing; slot++)
        {
            var bonusType = LegacyDamageType.Magic;
            var bonusValue = CalculateDamageBonus(equipment[slot], ref bonusType);
            if (bonusType == LegacyDamageType.Magic)
            {
                magicalBonus += bonusValue;
            }
            else
            {
                armor[(int)bonusType] += bonusValue;
            }
        }

        for (var slot = SlotRightHand; slot <= SlotLeftHand; slot++)
        {
            if (!IsShield(equipment[slot]))
            {
                continue;
            }

            modifier += CalculateAttackDefenseModifier(equipment[slot], items);
            var bonusType = LegacyDamageType.Magic;
            var bonusValue = CalculateDamageBonus(equipment[slot], ref bonusType);
            if (bonusType == LegacyDamageType.Magic)
            {
                magicalBonus += bonusValue;
            }
            else
            {
                armor[(int)bonusType] += bonusValue;
            }
        }

        modifier = Math.Max(-125, Math.Min(125, modifier));
        snapshot.ModificadorDefensa = (sbyte)modifier;

        ApplyArmorDescriptor(equipment[SlotArmor], ref armor[0], ref armor[1], ref armor[2], ref magicalBonus, items);
        ApplyArmorDescriptor(equipment[SlotHelmet], ref armor[0], ref armor[1], ref armor[2], ref magicalBonus, items);

        armor[(int)LegacyDamageType.Ice] += magicalBonus;
        armor[(int)LegacyDamageType.Fire] += magicalBonus;
        armor[(int)LegacyDamageType.Lightning] += magicalBonus;
        armor[(int)LegacyDamageType.Poison] += magicalBonus;

        ApplyAmuletResistances(equipment[SlotAmulet], armor);

        for (var i = 0; i < 8; i++)
        {
            var clamped = (sbyte)Math.Max(sbyte.MinValue, Math.Min(sbyte.MaxValue, armor[i]));
            snapshot.SetArmorValue(i, clamped);
        }
        InitializePartySlots(ref snapshot);
    }

    private static void ApplyArmorDescriptor(
        LegacyArtifact artifact,
        ref int pierce,
        ref int slash,
        ref int blunt,
        ref int magic,
        LegacyItemCatalog items)
    {
        if (artifact.Modifier == 0)
        {
            return;
        }

        var id = artifact.Id;
        var applies =
            (id >= 56 && id < 80) ||
            (id >= 88 && id < 96);

        if (!applies || !items.TryGetDefinition(id, out var definition))
        {
            return;
        }

        pierce += definition.Descriptor.Damage1Blunt;
        slash += definition.Descriptor.Damage1Pierce;
        blunt += definition.Descriptor.Damage2Blunt;
        magic += definition.Descriptor.Damage2Pierce;
    }

    private static void ApplyAmuletResistances(LegacyArtifact amulet, Span<int> armor)
    {
        if (amulet.Modifier == 0)
        {
            return;
        }

        var bonus = amulet.Modifier >> 5;
        if (bonus == 0)
        {
            return;
        }

        if (bonus > 3)
        {
            bonus = 3;
        }

        switch (amulet.Id)
        {
            case GemVenom:
                armor[(int)LegacyDamageType.Poison] += bonus;
                break;
            case GemFire:
                armor[(int)LegacyDamageType.Fire] += bonus;
                break;
            case GemIce:
                armor[(int)LegacyDamageType.Ice] += bonus;
                break;
            case GemLightning:
                armor[(int)LegacyDamageType.Lightning] += bonus;
                break;
        }
    }

    private static LegacyUserData BuildUserData(string login, int remoteIp, byte[] passwordHash)
    {
        var day = LegacyTime.GetCurrentDayCode();
        var data = new LegacyUserData
        {
            Version = 0,
            State = LegacyUserState.Normal,
            CreationDay = day,
            Permissions = LegacyUserPermissions.None,
            LastIp = remoteIp,
            Login = login,
            ChatPenalty = 0,
            LastLoginDay = day,
            ClanIdentifier = 0,
            ServerIdentifier = 0,
            ProcessBufferedCommands = false,
            IdleKickTimer = 0,
            Reserved = 0
        };

        Array.Copy(passwordHash, data.PasswordHash, Math.Min(passwordHash.Length, data.PasswordHash.Length));
        return data;
    }

    private static (byte MapId, byte X, byte Y) ResolveSpawnPosition(ServerOptions options, byte race)
    {
        if (options.BasePositions.Count > 0)
        {
            foreach (var position in options.BasePositions)
            {
                if (position.RaceId == race)
                {
                    return ((byte)position.MapId, position.X, position.Y);
                }
            }

            foreach (var position in options.BasePositions)
            {
                if (position.RaceId >= 8)
                {
                    return ((byte)position.MapId, position.X, position.Y);
                }
            }

            var first = options.BasePositions[0];
            return ((byte)first.MapId, first.X, first.Y);
        }

        return (0, 0, 0);
    }

    private static byte ComputeDamageBase(byte strength)
    {
        var capped = strength <= 20 ? strength : 0;
        return (byte)(capped << 2);
    }

    private static ushort ComputeMaxHp(byte category, byte constitution, byte level)
    {
        var bonus = category switch
        {
            0 => 16,
            5 => 14,
            2 => 8,
            _ => 12
        };

        bonus += constitution;
        int result;
        if (level <= LegacyConstants.MaxLevelWithBonus)
        {
            result = (bonus * level) >> 1;
        }
        else
        {
            result = (bonus * (level + LegacyConstants.MaxLevelWithBonus)) >> 2;
        }

        if (result > MaxHitPoints)
        {
            result = MaxHitPoints;
        }

        return (ushort)Math.Clamp(result, 0, ushort.MaxValue);
    }

    private static (byte Mana, byte Meditation) ComputeMana(byte category, byte intelligence, byte wisdom, byte level)
    {
        int bonus = category switch
        {
            2 => 40,
            7 or 1 => 15,
            5 or 6 or 4 => 1,
            _ => 0
        };

        if (bonus == 0)
        {
            return (0, 0);
        }

        switch (category)
        {
            case 2:
            case 7:
            case 6:
                bonus += (intelligence << 1) + wisdom;
                break;
            case 1:
            case 5:
            case 4:
                bonus += (wisdom << 1) + intelligence;
                break;
        }

        if (level <= LegacyConstants.MaxLevelWithBonus)
        {
            bonus = (bonus * level) >> 4;
        }
        else
        {
            bonus = (bonus * 3) >> 1;
            var extra = 0;
            if (intelligence >= 18)
            {
                extra += intelligence - 17;
            }

            if (wisdom >= 18)
            {
                extra += wisdom - 17;
            }

            bonus += extra * (level - LegacyConstants.MaxLevelWithBonus);
        }

        if (bonus < 3)
        {
            bonus = 3;
        }

        byte meditation = 0;
        if (bonus >= 255)
        {
            meditation = (byte)((bonus - 251) >> 2);
            bonus = 255;
        }

        return ((byte)bonus, meditation);
    }

    private static byte ComputeAttackLevel(byte category, byte dexterity, byte level)
    {
        int bonus = category switch
        {
            5 => 6,
            0 or 4 or 3 => 6,
            _ => 4
        };

        if (category == 5)
        {
            bonus = 6;
        }

        bonus += dexterity;

        int result;
        if (level <= LegacyConstants.MaxLevelWithBonus)
        {
            result = (bonus * level) >> 3;
        }
        else
        {
            result = bonus * 3;
            var superior = dexterity > 17 ? dexterity - 17 : 0;
            result += superior * (level - LegacyConstants.MaxLevelWithBonus);
        }

        result = Math.Clamp(result, 0, 250);
        return (byte)result;
    }

    private static byte ComputeDefense(byte category, byte dexterity, byte level)
    {
        int bonus = category switch
        {
            5 => 2,
            3 or 6 => 2,
            _ => 1
        };

        var baseValue = 46 + level;
        if (dexterity <= 20)
        {
            baseValue += dexterity * bonus;
        }

        baseValue = Math.Clamp(baseValue, 0, 255);
        return (byte)baseValue;
    }

    private static byte ResolveIdentificationCapacity(byte category) =>
        category switch
        {
            1 or 5 => 0x02,
            2 or 7 => 0x01,
            _ => 0
        };

    private static byte DetermineInitialSpell(byte category) =>
        category switch
        {
            1 or 5 or 4 => 9,
            _ => 6
        };

    private static byte ComputeAnimation(LegacyAnimationMap animationMap, bool isFemale, byte race, byte category, LegacyPlayerSnapshot snapshot)
    {
        snapshot.GetEquipmentArtifact(SlotArmor, out var armorId, out var armorMod);
        int index;
        if (armorId >= 4 && armorMod > 0)
        {
            index = armorId - 56;
            if (index >= 24)
            {
                index -= 168;
            }
        }
        else if (snapshot.Hp == 0 && snapshot.Nivel > LegacyConstants.MaxNewbieLevel)
        {
            index = 31;
        }
        else
        {
            index = 30;
        }

        return animationMap.Resolve((byte)(index & 0xFF), category, race, isFemale);
    }

    private static bool IsShield(in LegacyArtifact artifact) =>
        (artifact.Id >> 3) == 10;

    private static int CalculateDefenseModifier(in LegacyArtifact artifact, LegacyItemCatalog items)
    {
        if (artifact.Modifier == 0)
        {
            return 0;
        }

        var id = artifact.Id;
        var allowed =
            (id >= 56 && id <= 103) ||
            (id >= 248 && id <= 253);

        if (!allowed || !items.TryGetDefinition(id, out _))
        {
            return 0;
        }

        return CalculateAttackDefenseModifier(artifact, items);
    }

    private static int CalculateAttackDefenseModifier(in LegacyArtifact artifact, LegacyItemCatalog items)
    {
        if (artifact.Modifier == 0 || !items.TryGetDefinition(artifact.Id, out var definition))
        {
            return 0;
        }

        var magicType = (artifact.Modifier >> 6) & 0x03;
        if (magicType != 1 || (artifact.Modifier & 0x10) != 0)
        {
            return definition.Descriptor.DefenseModifier;
        }

        var value = (artifact.Modifier & 0x07) + 1;
        if ((artifact.Modifier & 0x08) != 0)
        {
            value = -value;
        }

        return value * 5 + definition.Descriptor.DefenseModifier;
    }

    private static int CalculateDamageBonus(in LegacyArtifact artifact, ref LegacyDamageType type)
    {
        if (artifact.Modifier == 0)
        {
            type = LegacyDamageType.Magic;
            return 0;
        }

        var magicType = (artifact.Modifier >> 6) & 0x03;
        if (magicType == 1)
        {
            if ((artifact.Modifier & 0x10) == 0)
            {
                type = LegacyDamageType.Magic;
                return 0;
            }

            var value = (artifact.Modifier & 0x07) + 1;
            if ((artifact.Modifier & 0x08) != 0)
            {
                value = -value;
            }

            type = LegacyDamageType.Magic;
            return value;
        }

        if (magicType == 2)
        {
            var value = (artifact.Modifier & 0x07) + 1;
            var element = (artifact.Modifier & 0x18) >> 3;
            type = (LegacyDamageType)(element + 3);
            return value;
        }

        type = LegacyDamageType.Magic;
        return 0;
    }

    private static byte SelectFace(byte race, bool isFemale)
    {
        if (isFemale)
        {
            return race switch
            {
                2 => 58,
                3 => 59,
                0 => (byte)(40 + NextRandom(8)),
                4 => (byte)(40 + NextRandom(12)),
                1 => (byte)(44 + NextRandom(8)),
                5 => (byte)(56 + NextRandom(2)),
                6 => (byte)(60 + NextRandom(2)),
                _ => 63
            };
        }

        return race switch
        {
            2 => (byte)NextRandom(8),
            3 => (byte)(8 + NextRandom(8)),
            0 => (byte)(16 + NextRandom(8)),
            4 => (byte)(20 + NextRandom(8)),
            1 => (byte)(24 + NextRandom(8)),
            5 => (byte)(32 + NextRandom(8)),
            6 => (byte)(52 + NextRandom(4)),
            _ => 63
        };
    }

    private static int NextRandom(int maxExclusive) =>
        maxExclusive <= 0 ? 0 : RandomNumberGenerator.GetInt32(maxExclusive);

    private static int CountBits(uint value)
    {
        var count = 0;
        while (value != 0)
        {
            count += (int)(value & 1);
            value >>= 1;
        }

        return count;
    }

    private static void WriteArtifacts(ref LegacyPlayerSnapshot snapshot, ReadOnlySpan<LegacyArtifact> artifacts, bool equipment)
    {
        for (var i = 0; i < artifacts.Length; i++)
        {
            if (equipment)
            {
                snapshot.SetEquipmentArtifact(i, artifacts[i].Id, artifacts[i].Modifier);
            }
            else
            {
                snapshot.SetInventoryArtifact(i, artifacts[i].Id, artifacts[i].Modifier);
            }
        }
    }

    private static void InitializePartySlots(ref LegacyPlayerSnapshot snapshot)
    {
        for (var i = 0; i < LegacyConstants.PartySlots; i++)
        {
            snapshot.SetPartySlot(i, ushort.MaxValue);
        }
    }

    private enum LegacyDamageType
    {
        Slash = 0,
        Pierce = 1,
        Blunt = 2,
        Poison = 3,
        Fire = 4,
        Ice = 5,
        Lightning = 6,
        Magic = 7
    }

    private readonly struct LegacyArtifact
    {
        public LegacyArtifact(byte id, byte modifier)
        {
            Id = id;
            Modifier = modifier;
        }

        public byte Id { get; }
        public byte Modifier { get; }

        public static LegacyArtifact Empty => new(0, 0);
        public static LegacyArtifact LeftHandEmpty => new(1, 1);
    }
}

internal readonly record struct LegacyCharacterCreationData(
    ushort Pericias,
    byte Race,
    byte Category,
    bool IsFemale,
    byte Strength,
    byte Dexterity,
    byte Constitution,
    byte Wisdom,
    byte Intelligence,
    string AvatarName,
    byte[] PasswordHash);

internal enum LegacyCreationStatus
{
    Success,
    Denied,
    Error
}
