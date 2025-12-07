using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ArtesArcanas.Server.Core.Legacy;
using ArtesArcanas.Server.Core.Logging;
using ArtesArcanas.Server.Core.Networking;
using ArtesArcanas.Server.Core.World.Maps;

namespace ArtesArcanas.Server.Core.World;

public sealed class LegacyWorldActionSink : ILegacyWorldActionHandler
{
    private static readonly sbyte[] DeltaX = { 0, 0, -1, 1, 1, -1, -1, 1 };
    private static readonly sbyte[] DeltaY = { -1, 1, 0, 0, -1, 1, -1, 1 };
    private const byte ItemIdLenna = 209;
    private const byte ItemIdMagicTrap = 225;
    private static readonly LegacyInventorySlot EmptyRightHandSlot = new(0, 1);
    private static readonly LegacyInventorySlot EmptyLeftHandSlot = new(1, 1);

    private readonly IServerLogger _logger;
    private readonly LegacySessionManager _sessions;
    private readonly LegacyWorldState _world;
    private readonly LegacyGameData _gameData;
    private readonly LegacyItemCatalog _items;
    private readonly LegacyMapSurface _mapSurface;
    private readonly ConcurrentDictionary<LegacyPlayerActionType, byte> _missingHandlers = new();
    private readonly LegacyAreaSnapshotBuilder _areaSnapshotBuilder;
    private readonly ConcurrentDictionary<long, DateTimeOffset> _sensorCooldowns = new();
    private static readonly TimeSpan RegenSensorCooldown = TimeSpan.FromMilliseconds(800);

    public LegacyWorldActionSink(IServerLogger logger, LegacySessionManager sessions, LegacyWorldState world, LegacyGameData data, LegacyMapSurface mapSurface)
    {
        _logger = logger;
        _sessions = sessions;
        _world = world;
        _gameData = data;
        _items = data.Items;
        _mapSurface = mapSurface;
        _areaSnapshotBuilder = new LegacyAreaSnapshotBuilder(world);
    }

    public async ValueTask HandleAsync(LegacyPlayerContext player, IReadOnlyList<LegacyPlayerAction> actions, CancellationToken cancellationToken)
    {
        if (actions.Count == 0)
        {
            return;
        }

        var snapshot = player.Snapshot;
        var originalMap = snapshot.CodigoMapa;
        var snapshotChanged = false;
        var positionChanged = false;

        foreach (var action in actions)
        {
            switch (action.Type)
            {
                case LegacyPlayerActionType.MoveStep:
                    if (HandleMoveStep(player, ref snapshot, action))
                    {
                        snapshotChanged = true;
                        positionChanged = true;
                    }
                    break;
                case LegacyPlayerActionType.MoveToCoordinate:
                    if (HandleMoveToCoordinate(player, ref snapshot, action))
                    {
                        snapshotChanged = true;
                        positionChanged = true;
                    }
                    break;
                case LegacyPlayerActionType.FollowEntity:
                    await HandleFollowAsync(player, action).ConfigureAwait(false);
                    break;
                case LegacyPlayerActionType.AttackOffensive:
                case LegacyPlayerActionType.AttackDefensive:
                    await HandleAttackAsync(player, action).ConfigureAwait(false);
                    break;
                case LegacyPlayerActionType.CastSpellSingle:
                case LegacyPlayerActionType.CastSpellContinuous:
                case LegacyPlayerActionType.CastSpellOnInventoryItem:
                    await HandleSpellAsync(player, action).ConfigureAwait(false);
                    break;
                case LegacyPlayerActionType.CommandFollowersAttack:
                case LegacyPlayerActionType.CommandFollowersFollow:
                case LegacyPlayerActionType.CommandFollowersStop:
                    await HandleFollowerCommandAsync(player, action).ConfigureAwait(false);
                    break;
                case LegacyPlayerActionType.ConsumeInventoryItem:
                    if (await HandleConsumeItemAsync(player, snapshot, action).ConfigureAwait(false))
                    {
                        snapshotChanged = true;
                    }
                    break;
                case LegacyPlayerActionType.UseInventoryItem:
                    if (await HandleUseItemAsync(player, snapshot, action).ConfigureAwait(false))
                    {
                        snapshotChanged = true;
                    }
                    break;
                case LegacyPlayerActionType.CraftInventoryItem:
                    await HandleCraftItemAsync(player, action).ConfigureAwait(false);
                    break;
                case LegacyPlayerActionType.DropInventoryItem:
                    {
                        var dropResult = await HandleDropItemAsync(player, snapshot, action).ConfigureAwait(false);
                        snapshotChanged |= dropResult.SnapshotChanged;
                        snapshot = dropResult.Snapshot;
                    }
                    break;
                case LegacyPlayerActionType.InspectGroundItems:
                    await HandleInspectGroundAsync(player, snapshot).ConfigureAwait(false);
                    break;
                case LegacyPlayerActionType.PickSpecificGroundItem:
                    {
                        var pickResult = await HandlePickSpecificAsync(player, snapshot, action).ConfigureAwait(false);
                        snapshotChanged |= pickResult.SnapshotChanged;
                        snapshot = pickResult.Snapshot;
                    }
                    break;
                case LegacyPlayerActionType.PickAllGroundItems:
                    {
                        var pickAllResult = await HandlePickAllAsync(player, snapshot).ConfigureAwait(false);
                        snapshotChanged |= pickAllResult.SnapshotChanged;
                        snapshot = pickAllResult.Snapshot;
                    }
                    break;
                case LegacyPlayerActionType.WithdrawMoney:
                    {
                        var withdrawResult = await HandleWithdrawMoneyAsync(player, snapshot, action).ConfigureAwait(false);
                        snapshotChanged |= withdrawResult.SnapshotChanged;
                        snapshot = withdrawResult.Snapshot;
                    }
                    break;
                case LegacyPlayerActionType.SensorClick:
                    if (await HandleSensorClickAsync(player, ref snapshot, action, cancellationToken).ConfigureAwait(false))
                    {
                        snapshotChanged = true;
                    }
                    break;
                default:
                    ReportMissingHandler(player, action);
                    break;
            }
        }

        var sensorResult = await HandleSensorTriggerAsync(player, ref snapshot, cancellationToken).ConfigureAwait(false);
        if (sensorResult.SnapshotChanged)
        {
            snapshotChanged = true;
        }

        if (sensorResult.PositionChanged)
        {
            positionChanged = true;
        }

        if (snapshotChanged)
        {
            player.UpdateSnapshot(snapshot);
        }

        var mapChanged = snapshot.CodigoMapa != originalMap;
        if (mapChanged)
        {
            await SendMapRefreshAsync(player, snapshot, cancellationToken).ConfigureAwait(false);
            positionChanged = false;
            await BroadcastMapChangeAsync(player, originalMap, snapshot.CodigoMapa).ConfigureAwait(false);
        }

        if (positionChanged)
        {
            if (player.IsInspectingBag && player.TryGetInspectedBag(out _, out var bagX, out var bagY))
            {
                await SendBagClosedAsync(player.Code, bagX, bagY).ConfigureAwait(false);
                player.EndInspectingBag();
            }

            await SendMovementUpdatesAsync(player, snapshot, cancellationToken).ConfigureAwait(false);
            await SendAreaRefreshAsync(player, snapshot).ConfigureAwait(false);
        }
    }

    private async ValueTask<SensorTriggerOutcome> HandleSensorTriggerAsync(LegacyPlayerContext player, ref LegacyPlayerSnapshot snapshot, CancellationToken cancellationToken)
    {
        var mapId = snapshot.CodigoMapa;
        if (!_mapSurface.TryGetSensor(mapId, snapshot.CoordenadaX, snapshot.CoordenadaY, out var sensor) ||
            sensor is null)
        {
            return default;
        }

        if (!IsSensorAccessible(snapshot, sensor, out var denialReason))
        {
            if (!string.IsNullOrEmpty(denialReason))
            {
                await SendInfoAsync(player.Code, denialReason).ConfigureAwait(false);
            }

            return default;
        }

        if (!TryValidateSensorKey(player, ref snapshot, sensor, mapId, out var keyContext, out var keyError))
        {
            if (!string.IsNullOrEmpty(keyError))
            {
                await SendInfoAsync(player.Code, keyError).ConfigureAwait(false);
            }

            return default;
        }

        var requiresConsumption = (sensor.Flags & LegacySensorFlags.ConsumeKey) != 0;

        SensorTriggerOutcome result;
        switch (sensor.Type)
        {
            case LegacySensorType.PhysicalRegeneration:
                result = await HandlePhysicalRegenSensorAsync(player, ref snapshot, mapId, keyContext, requiresConsumption).ConfigureAwait(false);
                break;
            case LegacySensorType.ManaRegeneration:
                result = await HandleManaRegenSensorAsync(player, ref snapshot, mapId, keyContext, requiresConsumption).ConfigureAwait(false);
                break;
            case LegacySensorType.Portal:
                result = await HandlePortalSensorAsync(player, ref snapshot, sensor, mapId, keyContext, requiresConsumption).ConfigureAwait(false);
                break;
            case LegacySensorType.SetFlag:
                result = await HandleFlagSensorAsync(player, ref snapshot, mapId, sensor, keyContext, setFlags: true, requiresConsumption).ConfigureAwait(false);
                break;
            case LegacySensorType.ClearFlag:
                result = await HandleFlagSensorAsync(player, ref snapshot, mapId, sensor, keyContext, setFlags: false, requiresConsumption).ConfigureAwait(false);
                break;
            case LegacySensorType.FoundClan:
                result = await HandleFoundClanSensorAsync(player, ref snapshot, sensor, mapId, keyContext, requiresConsumption, cancellationToken).ConfigureAwait(false);
                break;
            case LegacySensorType.ClanBanner:
                result = default;
                break;
            default:
                return default;
        }

        if (result.FlagsChanged)
        {
            await BroadcastMapFlagsAsync(mapId).ConfigureAwait(false);
        }

        return result;
    }

    private bool IsSensorAccessible(LegacyPlayerSnapshot snapshot, LegacyMapSensorDefinition sensor, out string? reason)
    {
        reason = null;
        if ((sensor.Flags & LegacySensorFlags.SoloGhost) != 0 && snapshot.Hp > 0)
        {
            reason = "Solo los espíritus pueden usar este portal.";
            return false;
        }

        if ((sensor.Flags & LegacySensorFlags.SoloApprentice) != 0 && snapshot.Nivel > LegacyConstants.MaxNewbieLevel)
        {
            reason = "Solo aprendices pueden usar este portal.";
            return false;
        }

        if ((sensor.Flags & LegacySensorFlags.SoloClan) != 0 && snapshot.Clan == 0)
        {
            reason = "Debes pertenecer a un clan para acceder a este sensor.";
            return false;
        }

        return true;
    }

    private async ValueTask<SensorTriggerOutcome> HandlePhysicalRegenSensorAsync(
        LegacyPlayerContext player,
        ref LegacyPlayerSnapshot snapshot,
        byte mapId,
        SensorKeyContext keyContext,
        bool requiresConsumption)
    {
        if (snapshot.Hp == 0)
        {
            await SendInfoAsync(player.Code, "No puedes regenerarte estando muerto.").ConfigureAwait(false);
            return default;
        }

        if (snapshot.Hp >= snapshot.MaxHp)
        {
            await SendInfoAsync(player.Code, "Ya tienes la salud completa.").ConfigureAwait(false);
            return default;
        }

        if (!TryEnterSensorCooldown(player.Code, LegacySensorType.PhysicalRegeneration, RegenSensorCooldown))
        {
            return default;
        }

        var heal = (snapshot.MaxHp >> 3) + 1;
        var updated = snapshot.Hp + heal;
        if (updated > snapshot.MaxHp)
        {
            updated = snapshot.MaxHp;
        }

        snapshot.Hp = (ushort)updated;
        await SendHpUpdateAsync(player.Code, snapshot.Hp).ConfigureAwait(false);
        var consumption = await ConsumeSensorKeyAsync(player, ref snapshot, mapId, keyContext, requiresConsumption).ConfigureAwait(false);
        return new SensorTriggerOutcome(true, false, consumption.FlagsChanged);
    }

    private async ValueTask<SensorTriggerOutcome> HandleManaRegenSensorAsync(
        LegacyPlayerContext player,
        ref LegacyPlayerSnapshot snapshot,
        byte mapId,
        SensorKeyContext keyContext,
        bool requiresConsumption)
    {
        if (snapshot.Hp == 0)
        {
            await SendInfoAsync(player.Code, "No puedes regenerar maná mientras estás muerto.").ConfigureAwait(false);
            return default;
        }

        if (snapshot.MaxMana == 0 || snapshot.Mana >= snapshot.MaxMana)
        {
            await SendInfoAsync(player.Code, "Tu maná ya está al máximo.").ConfigureAwait(false);
            return default;
        }

        if (!TryEnterSensorCooldown(player.Code, LegacySensorType.ManaRegeneration, RegenSensorCooldown))
        {
            return default;
        }

        var regen = (snapshot.MaxMana >> 3) + 1;
        var updated = snapshot.Mana + regen;
        if (updated > snapshot.MaxMana)
        {
            updated = snapshot.MaxMana;
        }

        snapshot.Mana = (byte)updated;
        await SendManaUpdateAsync(player.Code, snapshot.Mana).ConfigureAwait(false);
        var consumption = await ConsumeSensorKeyAsync(player, ref snapshot, mapId, keyContext, requiresConsumption).ConfigureAwait(false);
        return new SensorTriggerOutcome(true, false, consumption.FlagsChanged);
    }

    private async ValueTask<SensorTriggerOutcome> HandlePortalSensorAsync(
        LegacyPlayerContext player,
        ref LegacyPlayerSnapshot snapshot,
        LegacyMapSensorDefinition sensor,
        byte mapId,
        SensorKeyContext keyContext,
        bool requiresConsumption)
    {
        var destinationMap = sensor.Data1;
        var destinationX = sensor.Data2;
        var destinationY = sensor.Data3;
        var originalMap = snapshot.CodigoMapa;

        if (!_mapSurface.IsWalkable(destinationMap, destinationX, destinationY))
        {
            await SendInfoAsync(player.Code, "El portal está inestable y no puede abrirse.").ConfigureAwait(false);
            return default;
        }

        if (!_world.TryMovePlayer(player.Code, snapshot.CodigoMapa, snapshot.CoordenadaX, snapshot.CoordenadaY, destinationMap, destinationX, destinationY))
        {
            await SendInfoAsync(player.Code, "El destino del portal está ocupado.").ConfigureAwait(false);
            return default;
        }

        snapshot.CodigoMapa = destinationMap;
        snapshot.CoordenadaX = destinationX;
        snapshot.CoordenadaY = destinationY;
        snapshot.DestinoX = destinationX;
        snapshot.DestinoY = destinationY;
        snapshot.Direccion = 0;
        var movedWithinMap = destinationMap == originalMap;
        var consumption = await ConsumeSensorKeyAsync(player, ref snapshot, mapId, keyContext, requiresConsumption).ConfigureAwait(false);
        return new SensorTriggerOutcome(true, movedWithinMap, consumption.FlagsChanged);
    }

    private async ValueTask<SensorTriggerOutcome> HandleFlagSensorAsync(
        LegacyPlayerContext player,
        ref LegacyPlayerSnapshot snapshot,
        byte mapId,
        LegacyMapSensorDefinition sensor,
        SensorKeyContext keyContext,
        bool setFlags,
        bool requiresConsumption)
    {
        var mask = ComposeFlagMask(sensor);
        var changed = setFlags
            ? _world.TryUpdateMapFlags(mapId, mask, 0, out _)
            : _world.TryUpdateMapFlags(mapId, 0, mask, out _);

        if (changed)
        {
            _logger.Info($"[Mapa {mapId}] Flags actualizados ({(setFlags ? "set" : "clear")} {mask:X8}).");
        }

        var consumption = await ConsumeSensorKeyAsync(player, ref snapshot, mapId, keyContext, requiresConsumption).ConfigureAwait(false);
        return new SensorTriggerOutcome(consumption.SnapshotChanged, false, changed || consumption.FlagsChanged);
    }

    private async ValueTask<SensorTriggerOutcome> HandleFoundClanSensorAsync(
        LegacyPlayerContext player,
        ref LegacyPlayerSnapshot snapshot,
        LegacyMapSensorDefinition sensor,
        byte mapId,
        SensorKeyContext keyContext,
        bool requiresConsumption,
        CancellationToken token)
    {
        if ((snapshot.Pericias & LegacyConstants.SkillElocution) == 0)
        {
            await SendInfoAsync(player.Code, "Necesitas la pericia de Elocuencia para fundar un clan.").ConfigureAwait(false);
            return default;
        }

        if (snapshot.Nivel <= LegacyConstants.MaxLevelWithBonus)
        {
            await SendInfoAsync(player.Code, "Tu nivel aún es bajo para fundar un clan.").ConfigureAwait(false);
            return default;
        }

        if (snapshot.Clan <= LegacyConstants.MaxClans)
        {
            await SendInfoAsync(player.Code, "Debes abandonar tu clan actual antes de fundar otro.").ConfigureAwait(false);
            return default;
        }

        var leaderName = string.IsNullOrWhiteSpace(player.AvatarName)
            ? snapshot.GetAvatarName()
            : player.AvatarName;

        if (!_gameData.Clans.TryCreateClan(leaderName, null, out var clan, out var error) || clan is null)
        {
            await SendInfoAsync(player.Code, error ?? "No se pudo crear el clan.").ConfigureAwait(false);
            return default;
        }

        snapshot.Clan = clan.Id;
        await SendInfoAsync(player.Code, $"Fundaste el {clan.Name} (clan #{clan.Id}).").ConfigureAwait(false);
        await AnnounceClanCreationAsync(player, snapshot, clan, token).ConfigureAwait(false);
        _logger.Info($"[{player.Code}] Fundó el clan #{clan.Id} ({clan.Name}).");

        var portalOutcome = default(SensorTriggerOutcome);
        if (sensor.Data1 != 0 || sensor.Data2 != 0 || sensor.Data3 != 0)
        {
            portalOutcome = await HandlePortalSensorAsync(
                    player,
                    ref snapshot,
                    sensor,
                    mapId,
                    SensorKeyContext.None,
                    requiresConsumption: false)
                .ConfigureAwait(false);
        }

        var consumption = await ConsumeSensorKeyAsync(player, ref snapshot, mapId, keyContext, requiresConsumption).ConfigureAwait(false);
        return new SensorTriggerOutcome(
            SnapshotChanged: true,
            PositionChanged: portalOutcome.PositionChanged,
            FlagsChanged: portalOutcome.FlagsChanged || consumption.FlagsChanged);
    }

    private async Task SendHpUpdateAsync(ushort code, ushort hp)
    {
        var payload = new[]
        {
            (byte)0xFF,
            (byte)(hp & 0xFF),
            (byte)((hp >> 8) & 0xFF)
        };
        await _sessions.SendToSessionAsync(code, payload).ConfigureAwait(false);
    }

    private async Task SendManaUpdateAsync(ushort code, byte mana)
    {
        var payload = new[]
        {
            (byte)0xFE,
            mana
        };
        await _sessions.SendToSessionAsync(code, payload).ConfigureAwait(false);
    }

    private async Task AnnounceClanCreationAsync(LegacyPlayerContext player, LegacyPlayerSnapshot snapshot, LegacyClanInfo clan, CancellationToken token)
    {
        if (_sessions.TryGetContext(player.Code, out var context) && context is not null)
        {
            await context.ApplyClanAssignmentAsync(snapshot, clan, token).ConfigureAwait(false);
        }

        var assignment = LegacyWorldPacketFactory.BuildClanAssignmentPacket(clan.Id, player.Code);
        await _sessions.BroadcastToMapAsync(_world, snapshot.CodigoMapa, assignment).ConfigureAwait(false);

        var activation = LegacyWorldPacketFactory.BuildClanActivationPacket(clan);
        await _sessions.BroadcastAsync(activation).ConfigureAwait(false);
    }

    private static int ComposeFlagMask(LegacyMapSensorDefinition sensor) =>
        sensor.Data1 |
        (sensor.Data2 << 8) |
        (sensor.Data3 << 16) |
        (sensor.Data4 << 24);

    private readonly record struct SensorTriggerOutcome(bool SnapshotChanged, bool PositionChanged, bool FlagsChanged);

    private enum SensorKeyType
    {
        None = 0,
        MapFlag,
        Equipment,
        Honor
    }

    private readonly record struct SensorKeyContext(SensorKeyType Type, byte EquipmentSlot, int MapFlagMask, byte HonorCost)
    {
        public static SensorKeyContext None => default;
        public static SensorKeyContext ForMapFlag(int mask) => new(SensorKeyType.MapFlag, 0, mask, 0);
        public static SensorKeyContext ForEquipment(byte slot) => new(SensorKeyType.Equipment, slot, 0, 0);
        public static SensorKeyContext ForHonor(byte cost) => new(SensorKeyType.Honor, 0, 0, cost);
    }

    private readonly record struct SensorKeyOutcome(bool SnapshotChanged, bool FlagsChanged);

    private async ValueTask<SensorKeyOutcome> ConsumeSensorKeyAsync(
        LegacyPlayerContext player,
        ref LegacyPlayerSnapshot snapshot,
        byte mapId,
        SensorKeyContext context,
        bool requiresConsumption)
    {
        if (!requiresConsumption || context.Type == SensorKeyType.None)
        {
            return default;
        }

        var snapshotChanged = false;
        var flagsChanged = false;

        switch (context.Type)
        {
            case SensorKeyType.MapFlag:
                if (_world.TryUpdateMapFlags(mapId, 0, context.MapFlagMask, out _))
                {
                    flagsChanged = true;
                }
                break;
            case SensorKeyType.Honor:
                if (TryApplyHonorCost(ref snapshot, context.HonorCost))
                {
                    snapshotChanged = true;
                    await BroadcastHonorChangeAsync(mapId, player.Code, snapshot.Comportamiento).ConfigureAwait(false);
                }
                break;
            case SensorKeyType.Equipment:
                if (await ConsumeEquipmentAsync(player, ref snapshot, context.EquipmentSlot).ConfigureAwait(false))
                {
                    snapshotChanged = true;
                }
                break;
        }

        return new SensorKeyOutcome(snapshotChanged, flagsChanged);
    }

    private static bool TryApplyHonorCost(ref LegacyPlayerSnapshot snapshot, byte cost)
    {
        if (cost == 0)
        {
            return false;
        }

        var current = snapshot.Comportamiento;
        var updated = (sbyte)(current - cost);
        if (updated == current)
        {
            return false;
        }

        snapshot.Comportamiento = updated;
        return true;
    }

    private async ValueTask<bool> ConsumeEquipmentAsync(LegacyPlayerContext player, ref LegacyPlayerSnapshot snapshot, byte slot)
    {
        var replacement = slot switch
        {
            0 => EmptyRightHandSlot,
            1 => EmptyLeftHandSlot,
            _ => LegacyInventorySlot.Empty
        };

        if (replacement.IsEmpty)
        {
            return false;
        }

        if (!snapshot.TryWriteEquipmentSlot(slot, replacement))
        {
            return false;
        }

        await SendEquipmentSlotAsync(player, slot, replacement).ConfigureAwait(false);
        return true;
    }

    private async Task BroadcastHonorChangeAsync(byte mapId, ushort playerCode, sbyte behavior)
    {
        var payload = new[]
        {
            (byte)'I',
            (byte)'R',
            (byte)(playerCode & 0xFF),
            (byte)((playerCode >> 8) & 0xFF),
            unchecked((byte)behavior)
        };

        await _sessions.BroadcastToMapAsync(_world, mapId, payload).ConfigureAwait(false);
    }

    private bool HandleMoveStep(LegacyPlayerContext player, ref LegacyPlayerSnapshot snapshot, LegacyPlayerAction action)
    {
        var direction = action.PrimaryByte;
        if (direction >= DeltaX.Length)
        {
            return false;
        }

        var updated = false;
        var newX = unchecked((byte)(snapshot.CoordenadaX + DeltaX[direction]));
        var newY = unchecked((byte)(snapshot.CoordenadaY + DeltaY[direction]));

        if (!_world.TryMovePlayer(player.Code, snapshot.CodigoMapa, snapshot.CoordenadaX, snapshot.CoordenadaY, snapshot.CodigoMapa, newX, newY))
        {
            return false;
        }

        if (snapshot.CoordenadaX != newX || snapshot.CoordenadaY != newY)
        {
            snapshot.CoordenadaX = newX;
            snapshot.CoordenadaY = newY;
            snapshot.Direccion = direction;
            updated = true;
        }

        snapshot.DestinoX = newX;
        snapshot.DestinoY = newY;

        return updated;
    }

    private bool HandleMoveToCoordinate(LegacyPlayerContext player, ref LegacyPlayerSnapshot snapshot, LegacyPlayerAction action)
    {
        var packed = action.PrimaryWord;
        var destinationX = (byte)(packed & 0xFF);
        var destinationY = (byte)((packed >> 8) & 0xFF);

        if (!_world.TryMovePlayer(player.Code, snapshot.CodigoMapa, snapshot.CoordenadaX, snapshot.CoordenadaY, snapshot.CodigoMapa, destinationX, destinationY))
        {
            return false;
        }

        var updated = snapshot.CoordenadaX != destinationX || snapshot.CoordenadaY != destinationY;

        snapshot.CoordenadaX = destinationX;
        snapshot.CoordenadaY = destinationY;
        snapshot.DestinoX = destinationX;
        snapshot.DestinoY = destinationY;

        return updated;
    }

    private async ValueTask HandleFollowAsync(LegacyPlayerContext player, LegacyPlayerAction action)
    {
        if (!player.RecordAction(action))
        {
            return;
        }

        _logger.Info($"[{player.Code}] Comando de seguimiento hacia {action.PrimaryWord} (placeholder).");
        await SendInfoAsync(player.Code, $"Siguiendo al objetivo #{action.PrimaryWord} (motor aún en migración).").ConfigureAwait(false);
    }

    private async ValueTask HandleAttackAsync(LegacyPlayerContext player, LegacyPlayerAction action)
    {
        if (!player.RecordAction(action))
        {
            return;
        }

        var mode = action.Type == LegacyPlayerActionType.AttackDefensive ? "defensivo" : "ofensivo";
        _logger.Info($"[{player.Code}] Ataque {mode} al objetivo {action.PrimaryWord} (placeholder).");
        await SendInfoAsync(player.Code, $"Atacando ({mode}) al objetivo #{action.PrimaryWord}. La simulación llegará pronto.").ConfigureAwait(false);
    }

    private async ValueTask HandleSpellAsync(LegacyPlayerContext player, LegacyPlayerAction action)
    {
        if (!player.RecordAction(action))
        {
            return;
        }

        string description = action.Type switch
        {
            LegacyPlayerActionType.CastSpellOnInventoryItem => $"conjuro sobre objeto (slot {action.PrimaryByte})",
            LegacyPlayerActionType.CastSpellContinuous => $"conjuro continuo hacia #{action.PrimaryWord}",
            _ => $"conjuro hacia #{action.PrimaryWord}"
        };

        _logger.Info($"[{player.Code}] {description} (placeholder).");
        await SendInfoAsync(player.Code, $"{description}. El efecto aún no está disponible en .NET.").ConfigureAwait(false);
    }

    private async ValueTask HandleFollowerCommandAsync(LegacyPlayerContext player, LegacyPlayerAction action)
    {
        if (!player.RecordAction(action))
        {
            return;
        }

        var message = action.Type switch
        {
            LegacyPlayerActionType.CommandFollowersAttack => $"Ordenaste atacar al objetivo #{action.PrimaryWord}.",
            LegacyPlayerActionType.CommandFollowersFollow => $"Ordenaste seguir al objetivo #{action.PrimaryWord}.",
            LegacyPlayerActionType.CommandFollowersStop => "Ordenaste detener a los seguidores.",
            _ => "Comando de seguidores recibido."
        };

        _logger.Info($"[{player.Code}] {message} (placeholder).");
        await SendInfoAsync(player.Code, $"{message} IA en migración.").ConfigureAwait(false);
    }

    private ValueTask<bool> HandleConsumeItemAsync(LegacyPlayerContext player, LegacyPlayerSnapshot snapshot, LegacyPlayerAction action)
    {
        return ExecuteAsync();

        async ValueTask<bool> ExecuteAsync()
        {
            var slotIndex = action.PrimaryByte;
            if (!snapshot.TryReadSlot(slotIndex, out var slot) || slot.IsEmpty)
            {
                await SendInfoAsync(player.Code, "No hay nada para consumir en ese slot.").ConfigureAwait(false);
                return false;
            }

            var description = DescribeItem(slot);
            _logger.Info($"[{player.Code}] Consumir objeto {description} (slot {slotIndex}) (placeholder).");
            await SendInfoAsync(player.Code, $"Consumiendo {description}. Lógica en migración.").ConfigureAwait(false);
            return false;
        }
    }

    private ValueTask<bool> HandleUseItemAsync(LegacyPlayerContext player, LegacyPlayerSnapshot snapshot, LegacyPlayerAction action)
    {
        return ExecuteAsync();

        async ValueTask<bool> ExecuteAsync()
        {
            var slotIndex = action.PrimaryByte;
            if (!snapshot.TryReadSlot(slotIndex, out var slot) || slot.IsEmpty)
            {
                await SendInfoAsync(player.Code, "No hay objeto disponible en ese slot.").ConfigureAwait(false);
                return false;
            }

            var description = DescribeItem(slot);
            _logger.Info($"[{player.Code}] Usar objeto {description} (slot {slotIndex}) (placeholder).");
            await SendInfoAsync(player.Code, $"Usaste {description}. Efectos pendientes.").ConfigureAwait(false);
            return false;
        }
    }

    private async ValueTask HandleCraftItemAsync(LegacyPlayerContext player, LegacyPlayerAction action)
    {
        _logger.Info($"[{player.Code}] Fabricar receta {action.PrimaryByte} x{action.SecondaryByte} (placeholder).");
        await SendInfoAsync(player.Code, $"Fabricación pendiente (receta {action.PrimaryByte}, cant. {action.SecondaryByte}).").ConfigureAwait(false);
    }

    private ValueTask<(LegacyPlayerSnapshot Snapshot, bool SnapshotChanged)> HandleDropItemAsync(LegacyPlayerContext player, LegacyPlayerSnapshot snapshot, LegacyPlayerAction action)
    {
        return ExecuteAsync();

        async ValueTask<(LegacyPlayerSnapshot Snapshot, bool SnapshotChanged)> ExecuteAsync()
        {
            var working = snapshot;
            var slotIndex = action.PrimaryByte;
            if (!working.TryReadSlot(slotIndex, out var slot) || slot.IsEmpty)
            {
                await SendInfoAsync(player.Code, "Ese slot ya está vacío.").ConfigureAwait(false);
                return (snapshot, false);
            }

            if (_world.GroundItems.TryGetBagType(working.CodigoMapa, working.CoordenadaX, working.CoordenadaY, out var existingBagType) &&
                existingBagType == LegacyBagType.MagicTrap)
            {
                await SendInfoAsync(player.Code, "No puedes soltar objetos sobre una trampa activa.").ConfigureAwait(false);
                return (snapshot, false);
            }

            if (slot.ItemId < 4)
            {
                await SendInfoAsync(player.Code, "Ese objeto no puede soltarse.").ConfigureAwait(false);
                return (snapshot, false);
            }

            if (!LegacyItemStacking.TrySplit(slot, action.SecondaryByte, out var dropSlot, out var remainderSlot, out var actualAmount))
            {
                await SendInfoAsync(player.Code, "No se pudo determinar la cantidad a soltar.").ConfigureAwait(false);
                return (snapshot, false);
            }

            var originalSlot = slot;
            if (remainderSlot.HasValue)
            {
                working.TryWriteSlot(slotIndex, remainderSlot.Value);
                await SendInventorySlotAsync(player, slotIndex, remainderSlot.Value).ConfigureAwait(false);
            }
            else
            {
                working.TryClearSlot(slotIndex, out _);
                await SendInventorySlotAsync(player, slotIndex, LegacyInventorySlot.Empty).ConfigureAwait(false);
            }

            if (!_world.GroundItems.TryAddItem(
                    working.CodigoMapa,
                    working.CoordenadaX,
                    working.CoordenadaY,
                    player.Code,
                    dropSlot,
                    actualAmount,
                    out var entry,
                    out _))
            {
                working.TryWriteSlot(slotIndex, originalSlot);
                await SendInventorySlotAsync(player, slotIndex, originalSlot).ConfigureAwait(false);
                await SendInfoAsync(player.Code, "No hay espacio en el suelo para dejar más objetos.").ConfigureAwait(false);
                return (snapshot, false);
            }

            var description = DescribeItem(dropSlot, actualAmount);
            _logger.Info($"[{player.Code}] Dejó {description} en bolsa slot {entry.BagSlot} ({working.CodigoMapa}:{working.CoordenadaX},{working.CoordenadaY}).");
            await SendInfoAsync(player.Code, $"Depositaste {description} en la bolsa {entry.BagSlot}.").ConfigureAwait(false);
            var bagType = DetermineBagType(dropSlot);
            await UpdateBagMarkerAsync(working.CodigoMapa, working.CoordenadaX, working.CoordenadaY, bagType).ConfigureAwait(false);
            await NotifyInspectingPlayersAsync(working.CodigoMapa, working.CoordenadaX, working.CoordenadaY).ConfigureAwait(false);
            return (working, true);
        }
    }

    private ValueTask HandleInspectGroundAsync(LegacyPlayerContext player, LegacyPlayerSnapshot snapshot)
    {
        return ExecuteAsync();

        async ValueTask ExecuteAsync()
        {
            if (!_world.GroundItems.TryGetBagSlots(snapshot.CodigoMapa, snapshot.CoordenadaX, snapshot.CoordenadaY, out var slots))
            {
                if (player.IsInspectingBag && player.TryGetInspectedBag(out _, out var bagX, out var bagY))
                {
                    await SendBagClosedAsync(player.Code, bagX, bagY).ConfigureAwait(false);
                    player.EndInspectingBag();
                }

                await SendInfoAsync(player.Code, "No hay bolsas registradas en esta casilla.").ConfigureAwait(false);
                return;
            }

            await SendBagSnapshotAsync(player.Code, slots).ConfigureAwait(false);
            player.BeginInspectingBag(snapshot.CodigoMapa, snapshot.CoordenadaX, snapshot.CoordenadaY);

            var builder = new StringBuilder("Contenido de la bolsa:");
            for (var i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                if (slot.IsEmpty)
                {
                    continue;
                }

                builder.Append($" [{i}] {DescribeItem(slot)}");
            }

            await SendInfoAsync(player.Code, builder.ToString()).ConfigureAwait(false);
        }
    }

    private ValueTask<(LegacyPlayerSnapshot Snapshot, bool SnapshotChanged)> HandlePickSpecificAsync(LegacyPlayerContext player, LegacyPlayerSnapshot snapshot, LegacyPlayerAction action)
    {
        return ExecuteAsync();

        async ValueTask<(LegacyPlayerSnapshot Snapshot, bool SnapshotChanged)> ExecuteAsync()
        {
            var working = snapshot;
            var bagSlot = action.PrimaryByte;
            if (!_world.GroundItems.TryTake(working.CodigoMapa, working.CoordenadaX, working.CoordenadaY, bagSlot, out var removed, out var bagCleared) ||
                removed is null)
            {
                await SendInfoAsync(player.Code, "No se encontró un objeto en ese espacio de la bolsa.").ConfigureAwait(false);
                return (snapshot, false);
            }

            var entry = removed.Value;
            var slotData = new LegacyInventorySlot(entry.ItemId, entry.Modifier);
            if (!LegacyItemStacking.TrySplit(slotData, action.SecondaryByte, out var taken, out var leftover, out var actualAmount))
            {
                _world.GroundItems.PutBack(working.CodigoMapa, working.CoordenadaX, working.CoordenadaY, entry);
                await SendInfoAsync(player.Code, "No se pudo separar la cantidad solicitada.").ConfigureAwait(false);
                return (snapshot, false);
            }

            if (!TryAddToInventory(ref working, taken, out var targetSlot))
            {
                _world.GroundItems.PutBack(working.CodigoMapa, working.CoordenadaX, working.CoordenadaY, entry);
                await SendInfoAsync(player.Code, "No hay espacio en el inventario para recoger la bolsa.").ConfigureAwait(false);
                return (snapshot, false);
            }

            var bagStillExists = !bagCleared;
            if (leftover.HasValue)
            {
                var updatedEntry = entry with
                {
                    ItemId = leftover.Value.ItemId,
                    Modifier = leftover.Value.Modifier,
                    Quantity = LegacyItemStacking.GetCount(leftover.Value)
                };
                _world.GroundItems.PutBack(working.CodigoMapa, working.CoordenadaX, working.CoordenadaY, updatedEntry);
                bagStillExists = true;
                await UpdateBagMarkerAsync(working.CodigoMapa, working.CoordenadaX, working.CoordenadaY, DetermineBagType(leftover.Value)).ConfigureAwait(false);
            }

            await SendInventorySlotAsync(player, targetSlot, taken).ConfigureAwait(false);
            var description = DescribeItem(taken, actualAmount);
            _logger.Info($"[{player.Code}] Tomó {description} de la bolsa {bagSlot} y lo colocó en el slot {targetSlot}.");
            await SendInfoAsync(player.Code, $"Recogiste {description} en el slot {targetSlot}.").ConfigureAwait(false);

            if (bagStillExists)
            {
                await NotifyInspectingPlayersAsync(working.CodigoMapa, working.CoordenadaX, working.CoordenadaY).ConfigureAwait(false);
            }
            else
            {
                await SendBagRemovalAsync(working.CodigoMapa, working.CoordenadaX, working.CoordenadaY).ConfigureAwait(false);
            }

            return (working, true);
        }
    }

    private ValueTask<(LegacyPlayerSnapshot Snapshot, bool SnapshotChanged)> HandlePickAllAsync(LegacyPlayerContext player, LegacyPlayerSnapshot snapshot)
    {
        return ExecuteAsync();

        async ValueTask<(LegacyPlayerSnapshot Snapshot, bool SnapshotChanged)> ExecuteAsync()
        {
            var working = snapshot;
            var collected = _world.GroundItems.TakeAll(working.CodigoMapa, working.CoordenadaX, working.CoordenadaY);
            if (collected.Count == 0)
            {
                await SendInfoAsync(player.Code, "No hay bolsas por recoger en esta casilla.").ConfigureAwait(false);
                return (snapshot, false);
            }

            var picked = 0;
            var leftovers = new List<LegacyGroundItem>();
            foreach (var entry in collected)
            {
                var slotData = new LegacyInventorySlot(entry.ItemId, entry.Modifier);
                if (TryAddToInventory(ref working, slotData, out var assigned))
                {
                    picked++;
                    await SendInventorySlotAsync(player, assigned, slotData).ConfigureAwait(false);
                }
                else
                {
                    leftovers.Add(entry);
                }
            }

            foreach (var entry in leftovers)
            {
                var slotData = new LegacyInventorySlot(entry.ItemId, entry.Modifier);
                _world.GroundItems.TryAddItem(
                    working.CodigoMapa,
                    working.CoordenadaX,
                    working.CoordenadaY,
                    entry.OwnerCode,
                    slotData,
                    entry.Quantity,
                    out _,
                    out _);
                await UpdateBagMarkerAsync(working.CodigoMapa, working.CoordenadaX, working.CoordenadaY, DetermineBagType(slotData)).ConfigureAwait(false);
            }

            if (leftovers.Count > 0)
            {
                await SendInfoAsync(player.Code, $"Recogiste {picked} objetos pero quedaron {leftovers.Count} por falta de espacio.").ConfigureAwait(false);
                await NotifyInspectingPlayersAsync(working.CodigoMapa, working.CoordenadaX, working.CoordenadaY).ConfigureAwait(false);
            }
            else
            {
                _logger.Info($"[{player.Code}] Recogió {picked} objetos del suelo.");
                await SendInfoAsync(player.Code, $"Recogiste {picked} objetos registrados.").ConfigureAwait(false);
                await SendBagRemovalAsync(working.CodigoMapa, working.CoordenadaX, working.CoordenadaY).ConfigureAwait(false);
            }

            return (working, picked > 0);
        }
    }

    private static bool TryAddToInventory(ref LegacyPlayerSnapshot snapshot, LegacyInventorySlot slotData, out int actualSlot)
    {
        actualSlot = -1;
        if (snapshot.TryFindEmptySlot(out var emptySlot) &&
            snapshot.TryWriteSlot(emptySlot, slotData))
        {
            actualSlot = emptySlot;
            return true;
        }

        return false;
    }

    private ValueTask<(LegacyPlayerSnapshot Snapshot, bool SnapshotChanged)> HandleWithdrawMoneyAsync(LegacyPlayerContext player, LegacyPlayerSnapshot snapshot, LegacyPlayerAction action)
    {
        return ExecuteAsync();

        async ValueTask<(LegacyPlayerSnapshot Snapshot, bool SnapshotChanged)> ExecuteAsync()
        {
            var working = snapshot;
            var amount = action.GetCombinedAmount();
            if (amount <= 0)
            {
                await SendInfoAsync(player.Code, "Monto de dinero inválido.").ConfigureAwait(false);
                return (snapshot, false);
            }

            if (working.Dinero <= 0 || working.Dinero < amount)
            {
                await SendInfoAsync(player.Code, "No tienes suficiente oro para soltar esa cantidad.").ConfigureAwait(false);
                return (snapshot, false);
            }

            working.Dinero -= amount;
            await SendInfoAsync(player.Code, $"Reservaste ${amount}. El oro aún no puede colocarse en el suelo (pendiente de migración).").ConfigureAwait(false);
            return (working, true);
        }
    }

    private string DescribeItem(LegacyInventorySlot slot, int? overrideQuantity = null)
    {
        var name = _items.GetNameOrFallback(slot.ItemId);
        var quantity = overrideQuantity ?? LegacyItemStacking.GetCount(slot);
        return quantity > 1 ? $"{quantity}× {name}" : name;
    }

    private async ValueTask SendBagSnapshotAsync(ushort code, LegacyInventorySlot[] slots)
    {
        var payload = new byte[2 + slots.Length * 2];
        payload[0] = (byte)'I';
        payload[1] = (byte)'O';

        for (var i = 0; i < slots.Length; i++)
        {
            var offset = 2 + (i * 2);
            payload[offset] = slots[i].ItemId;
            payload[offset + 1] = slots[i].Modifier;
        }

        await _sessions.SendToSessionAsync(code, payload).ConfigureAwait(false);
    }

    private async ValueTask SendInventorySlotAsync(LegacyPlayerContext player, int slot, LegacyInventorySlot data)
    {
        var payload = LegacyInventoryPacketFactory.BuildInventorySlotPacket(slot, data.ItemId, data.Modifier);
        if (payload is null)
        {
            return;
        }

        await _sessions.SendToSessionAsync(player.Code, payload).ConfigureAwait(false);
    }

    private async ValueTask SendEquipmentSlotAsync(LegacyPlayerContext player, int slot, LegacyInventorySlot data)
    {
        var payload = LegacyInventoryPacketFactory.BuildEquipmentSlotPacket(slot, data.ItemId, data.Modifier);
        if (payload is null)
        {
            return;
        }

        await _sessions.SendToSessionAsync(player.Code, payload).ConfigureAwait(false);
    }

    private LegacyBagType DetermineBagType(LegacyInventorySlot slot) =>
        slot.ItemId switch
        {
            ItemIdLenna => LegacyBagType.Lenna,
            ItemIdMagicTrap => LegacyBagType.MagicTrap,
            _ => LegacyBagType.Common
        };

    private async ValueTask UpdateBagMarkerAsync(byte mapId, byte x, byte y, LegacyBagType desiredType)
    {
        if (desiredType == LegacyBagType.None)
        {
            desiredType = LegacyBagType.Common;
        }

        if (!_world.GroundItems.TrySetBagType(mapId, x, y, desiredType, out var _))
        {
            return;
        }

        await SendBagMarkerAsync(mapId, x, y, desiredType).ConfigureAwait(false);
    }

    private async ValueTask SendBagMarkerAsync(byte mapId, byte x, byte y, LegacyBagType bagType)
    {
        var opcode = bagType switch
        {
            LegacyBagType.Lenna => (byte)195,
            LegacyBagType.Campfire => (byte)196,
            LegacyBagType.MagicTrap => (byte)203,
            _ => (byte)194
        };

        var payload = new[] { opcode, x, y };
        await _sessions.BroadcastToMapAsync(_world, mapId, payload).ConfigureAwait(false);
    }

    private async ValueTask SendBagRemovalAsync(byte mapId, byte x, byte y)
    {
        var payload = new[] { (byte)192, x, y };
        await _sessions.BroadcastToMapAsync(_world, mapId, payload).ConfigureAwait(false);
        var watchers = CollectBagInspectors(mapId, x, y);
        if (watchers.Count > 0)
        {
            await CloseInspectingPlayersAsync(watchers, x, y).ConfigureAwait(false);
        }
    }

    private async ValueTask NotifyInspectingPlayersAsync(byte mapId, byte x, byte y)
    {
        var watchers = CollectBagInspectors(mapId, x, y);
        if (watchers.Count == 0)
        {
            return;
        }

        if (!_world.GroundItems.TryGetBagSlots(mapId, x, y, out var slots))
        {
            await CloseInspectingPlayersAsync(watchers, x, y).ConfigureAwait(false);
            return;
        }

        foreach (var watcher in watchers)
        {
            await SendBagSnapshotAsync(watcher.Code, slots).ConfigureAwait(false);
        }
    }

    private async ValueTask CloseInspectingPlayersAsync(IList<LegacyPlayerContext> watchers, byte x, byte y)
    {
        foreach (var watcher in watchers)
        {
            await SendBagClosedAsync(watcher.Code, x, y).ConfigureAwait(false);
            watcher.EndInspectingBag();
        }
    }

    private List<LegacyPlayerContext> CollectBagInspectors(byte mapId, byte x, byte y)
    {
        var watchers = new List<LegacyPlayerContext>();
        var players = _world.GetPlayersOnMap(mapId);
        foreach (var other in players)
        {
            if (other.IsInspectingTile(mapId, x, y))
            {
                watchers.Add(other);
            }
        }

        return watchers;
    }

    private async ValueTask SendBagClosedAsync(ushort code, byte x, byte y)
    {
        var payload = new[] { (byte)193, x, y };
        await _sessions.SendToSessionAsync(code, payload).ConfigureAwait(false);
    }

    private async ValueTask<bool> HandleSensorClickAsync(
        LegacyPlayerContext player,
        ref LegacyPlayerSnapshot snapshot,
        LegacyPlayerAction action,
        CancellationToken cancellationToken)
    {
        var mapId = snapshot.CodigoMapa;
        var sensorY = action.PrimaryByte;
        var sensorX = action.SecondaryByte;

        if (!_mapSurface.TryGetSensor(mapId, sensorX, sensorY, out var sensor) ||
            sensor is null ||
            sensor.Type != LegacySensorType.ClanBanner)
        {
            return false;
        }

        if (snapshot.Hp == 0 && snapshot.Comportamiento <= LegacyConstants.HeroBehaviorThreshold)
        {
            return false;
        }

        if (!TryValidateSensorKey(player, ref snapshot, sensor, mapId, out var keyContext, out var keyError))
        {
            if (!string.IsNullOrEmpty(keyError))
            {
                await SendInfoAsync(player.Code, keyError).ConfigureAwait(false);
            }

            return false;
        }

        var dx = snapshot.CoordenadaX - sensorX;
        var dy = snapshot.CoordenadaY - sensorY;
        var distanceSquared = (dx * dx) + (dy * dy);
        var hasExtendedRange = (sensor.Flags & LegacySensorFlags.RepelAvatar) != 0;
        var withinCloseRange = distanceSquared <= 5 && snapshot.CoordenadaY >= sensorY - 1;
        if (distanceSquared > 8 || (!hasExtendedRange && !withinCloseRange))
        {
            await SendInfoAsync(player.Code, "Estás demasiado lejos para accionar esta bandera.").ConfigureAwait(false);
            return false;
        }

        if ((sensor.Flags & LegacySensorFlags.SoloClan) != 0)
        {
            var castle = _gameData.Castles.Castles.FirstOrDefault(c => c.MapId == mapId);
            if (snapshot.Clan == LegacyConstants.NoClanId ||
                castle is null ||
                castle.ClanId != snapshot.Clan)
            {
                await SendInfoAsync(player.Code, "Solo el clan dueño del castillo puede usar esta bandera.").ConfigureAwait(false);
                return false;
            }
        }

        var mask = ComposeFlagMask(sensor);
        var requiresKey = (sensor.Flags & LegacySensorFlags.ConsumeKey) != 0;
        if (requiresKey)
        {
            var flags = _world.GetMapFlags(mapId);
            if ((flags & mask) == mask)
            {
                await SendInfoAsync(player.Code, "La bandera ya está activa.").ConfigureAwait(false);
                return false;
            }
        }

        var toggled = _world.TryToggleMapFlags(mapId, mask, out _);
        if (!toggled)
        {
            return false;
        }

        var consumption = await ConsumeSensorKeyAsync(player, ref snapshot, mapId, keyContext, requiresKey).ConfigureAwait(false);
        var flagsChanged = toggled || consumption.FlagsChanged;
        if (flagsChanged)
        {
            await BroadcastMapFlagsAsync(mapId).ConfigureAwait(false);
        }

        return consumption.SnapshotChanged;
    }

    private ValueTask SendMovementUpdatesAsync(LegacyPlayerContext player, LegacyPlayerSnapshot snapshot, CancellationToken cancellationToken)
    {
        return ExecuteAsync();

        async ValueTask ExecuteAsync()
        {
            try
            {
                var selfPayload = LegacyWorldPacketFactory.BuildSelfMovementPacket(snapshot);
                await _sessions.SendToSessionAsync(player.Code, selfPayload).ConfigureAwait(false);

                var broadcast = LegacyWorldPacketFactory.BuildMovementBroadcastPacket(player.Code, snapshot);
                await _sessions.BroadcastToMapAsync(_world, snapshot.CodigoMapa, broadcast, player.Code).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.Error($"[{player.Code}] Error enviando actualización de movimiento.", ex);
            }
        }
    }

    private async Task SendInfoAsync(ushort code, string message)
    {
        try
        {
            var encoded = LegacyConstants.LegacyEncoding.GetBytes(message);
            var length = (byte)Math.Min(255, encoded.Length);
            var payload = new byte[3 + length];
            payload[0] = (byte)'I';
            payload[1] = (byte)'G';
            payload[2] = length;
            Array.Copy(encoded, 0, payload, 3, length);
            await _sessions.SendToSessionAsync(code, payload).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.Error($"[{code}] Error enviando mensaje informativo.", ex);
        }
    }

    private void ReportMissingHandler(LegacyPlayerContext player, LegacyPlayerAction action)
    {
        if (_missingHandlers.TryAdd(action.Type, 0))
        {
            _logger.Warning($"[{player.Code}] Acción legacy '{(char)action.Opcode}' ({action.Type}) aún no implementada en el mundo .NET.");
        }
    }

    private ValueTask SendAreaRefreshAsync(LegacyPlayerContext player, LegacyPlayerSnapshot snapshot)
    {
        return ExecuteAsync();

        async ValueTask ExecuteAsync()
        {
            var payloads = _areaSnapshotBuilder.BuildRefreshPackets(player, snapshot);
            if (payloads.Count == 0)
            {
                return;
            }

            foreach (var payload in payloads)
            {
                await _sessions.SendToSessionAsync(player.Code, payload).ConfigureAwait(false);
            }
        }
    }

    private async Task SendMapRefreshAsync(LegacyPlayerContext player, LegacyPlayerSnapshot snapshot, CancellationToken cancellationToken)
    {
        if (!_sessions.TryGetContext(player.Code, out var context) || context is null)
        {
            return;
        }

        await context.SendMapRefreshAsync(snapshot, cancellationToken).ConfigureAwait(false);
    }

    private async Task BroadcastMapChangeAsync(LegacyPlayerContext player, byte previousMap, byte newMap)
    {
        try
        {
            var removal = LegacyWorldPacketFactory.BuildPlayerRemovalPacket(player.Code);
            await _sessions.BroadcastToMapAsync(_world, previousMap, removal, player.Code).ConfigureAwait(false);

            var spawn = LegacyWorldPacketFactory.BuildPlayerSpawnPacket(player);
            await _sessions.BroadcastToMapAsync(_world, newMap, spawn, player.Code).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.Error($"[{player.Code}] Error difundiendo cambio de mapa.", ex);
        }
    }

    private async Task BroadcastMapFlagsAsync(byte mapId)
    {
        var payload = BuildFlagPacket(_world.GetMapFlags(mapId));
        await _sessions.BroadcastToMapAsync(_world, mapId, payload).ConfigureAwait(false);
    }

    private static byte[] BuildFlagPacket(int flags)
    {
        var buffer = new List<byte> { (byte)'&', 0, (byte)'k' };
        AppendInt32(buffer, flags);
        return buffer.ToArray();
    }

    private static void AppendInt32(List<byte> buffer, int value)
    {
        buffer.Add((byte)(value & 0xFF));
        buffer.Add((byte)((value >> 8) & 0xFF));
        buffer.Add((byte)((value >> 16) & 0xFF));
        buffer.Add((byte)((value >> 24) & 0xFF));
    }

    private bool TryValidateSensorKey(
        LegacyPlayerContext player,
        ref LegacyPlayerSnapshot snapshot,
        LegacyMapSensorDefinition sensor,
        byte mapId,
        out SensorKeyContext context,
        out string? errorMessage)
    {
        context = SensorKeyContext.None;
        errorMessage = null;
        var restrictToHands = (sensor.Flags & LegacySensorFlags.ConsumeKey) != 0;
        var keyId = sensor.Key1;
        var keyModifier = sensor.Key2;

        switch (keyId)
        {
            case 0:
                return true;
            case 1:
                {
                    var mask = 1 << (keyModifier & 0x1F);
                    var flags = _world.GetMapFlags(mapId);
                    if ((flags & mask) == 0)
                    {
                        errorMessage = "Necesitas activar antes los mecanismos del castillo.";
                        return false;
                    }

                    context = SensorKeyContext.ForMapFlag(mask);
                    return true;
                }
            case 2:
                {
                    var behavior = unchecked((byte)snapshot.Comportamiento);
                    if (behavior < keyModifier)
                    {
                        errorMessage = "Tu honor no es suficiente para usar este sensor.";
                        return false;
                    }

                    context = SensorKeyContext.ForHonor(keyModifier);
                    return true;
                }
            case 3:
                {
                    if (!EvaluatePlayerRequirement(ref snapshot, keyModifier))
                    {
                        errorMessage = "No cumples los requisitos de este sensor.";
                        return false;
                    }

                    return true;
                }
            default:
                {
                    if (!TryFindEquipmentSlot(ref snapshot, keyId, keyModifier, restrictToHands, out var slot))
                    {
                        errorMessage = "Necesitas sostener la llave adecuada.";
                        return false;
                    }

                    context = SensorKeyContext.ForEquipment(slot);
                    return true;
                }
        }
    }

    private static bool EvaluatePlayerRequirement(ref LegacyPlayerSnapshot snapshot, byte descriptor)
    {
        var value = descriptor & 0x0F;
        var category = descriptor >> 4;
        return category switch
        {
            0 => snapshot.RitmoDeVida == 0,
            1 => snapshot.Categoria == value,
            2 => snapshot.TipoMonstruo == value,
            3 => snapshot.Nivel >= value * 5,
            4 => (snapshot.Pericias & (1u << value)) != 0,
            5 => (snapshot.Pericias & (1u << (value + 16))) != 0,
            6 => snapshot.Hp >= value * 10,
            7 => snapshot.Mana >= value * 10,
            8 => (snapshot.Conjuros & (1u << value)) != 0,
            9 => (snapshot.Conjuros & (1u << (value + 16))) != 0,
            10 => snapshot.DannoBase >= (value << 3),
            11 => snapshot.Constitucion >= (value << 1),
            12 => snapshot.Inteligencia >= (value << 1),
            13 => snapshot.Sabiduria >= (value << 1),
            14 => snapshot.Destreza >= (value << 1),
            _ => false
        };
    }

    private bool TryFindEquipmentSlot(
        ref LegacyPlayerSnapshot snapshot,
        byte itemId,
        byte modifier,
        bool handsOnly,
        out byte slot)
    {
        var limit = handsOnly ? 2 : LegacyConstants.EquipmentSlots;
        for (var index = 0; index < limit; index++)
        {
            if (!snapshot.TryReadEquipmentSlot(index, out var equipment))
            {
                continue;
            }

            if (equipment.ItemId == itemId && (modifier == 0 || equipment.Modifier == modifier))
            {
                slot = (byte)index;
                return true;
            }
        }

        slot = 0;
        return false;
    }

    private bool TryEnterSensorCooldown(ushort playerCode, LegacySensorType type, TimeSpan cooldown)
    {
        var key = ((long)type << 32) | playerCode;
        var now = DateTimeOffset.UtcNow;

        while (true)
        {
            if (_sensorCooldowns.TryGetValue(key, out var expires) && expires > now)
            {
                return false;
            }

            var target = now + cooldown;
            if (_sensorCooldowns.TryAdd(key, target))
            {
                return true;
            }

            if (_sensorCooldowns.TryUpdate(key, target, expires))
            {
                return true;
            }
        }
    }
}
