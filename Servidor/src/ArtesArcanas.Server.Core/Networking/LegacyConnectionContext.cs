using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using System.Text;
using ArtesArcanas.Server.Core.Configuration;
using ArtesArcanas.Server.Core.Legacy;
using ArtesArcanas.Server.Core.Logging;
using ArtesArcanas.Server.Core.World;
using ArtesArcanas.Server.Core.World.Maps;

namespace ArtesArcanas.Server.Core.Networking;

internal sealed class LegacyConnectionContext : IAsyncDisposable
{
    private readonly TcpClient _client;
    private readonly NetworkStream _stream;
    private readonly LegacyWorldState _world;
    private readonly LegacyGameData _gameData;
    private readonly LegacyAccountStorage _accounts;
    private readonly ServerOptions _options;
    private readonly ILegacyCommandSink _commandSink;
    private readonly LegacySessionManager _sessionManager;
    private readonly IServerLogger _logger;
    private readonly CancellationToken _serverToken;
    private readonly List<byte> _buffer = new();
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    private int _bufferOffset;
    private bool _handshakeValidated;
    private bool _disconnectRequested;
    private bool _loggedIn;
    private string? _loginName;
    private LegacyUserAccount? _account;
    private LegacyPlayerSnapshot _playerSnapshot;
    private bool _hasSnapshot;
    private LegacyUserState _userState;
    private string _avatarName = string.Empty;
    private LegacyPlayerContext? _playerContext;
    private bool _departureNotified;
    private bool _spawnNotificationSent;
    private bool _spawnRepositioned;
    private byte _lastBroadcastedMapId = byte.MaxValue;

    public LegacyConnectionContext(
        TcpClient client,
        NetworkStream stream,
        LegacyWorldState world,
        LegacyGameData data,
        LegacyAccountStorage accounts,
        ServerOptions options,
        ILegacyCommandSink commandSink,
        LegacySessionManager sessionManager,
        IServerLogger logger,
        CancellationToken serverToken,
        ushort code)
    {
        _client = client;
        _stream = stream;
        _world = world;
        _gameData = data;
        _accounts = accounts;
        _options = options;
        _commandSink = commandSink;
        _sessionManager = sessionManager;
        _logger = logger;
        _serverToken = serverToken;
        Code = code;
        HandshakeSeed = CreateHandshakeSeed();
    }

    public ushort Code { get; }
    public uint HandshakeSeed { get; }

    public async Task RunAsync(CancellationToken token)
    {
        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token, _serverToken);
        try
        {
            await SendHandshakeAsync(linkedCts.Token).ConfigureAwait(false);
            await ReceiveLoopAsync(linkedCts.Token).ConfigureAwait(false);
        }
        finally
        {
            linkedCts.Dispose();
            await NotifyDepartureAsync().ConfigureAwait(false);
            _sessionManager.Unregister(Code);
            _world.ReleaseLogin(_loginName);
            _world.RemovePlayer(Code);
            _world.ReleaseSlot(Code);
        }
    }

    private async Task SendHandshakeAsync(CancellationToken token)
    {
        var payload = new byte[7];
        payload[0] = (byte)'|';
        BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(1, 2), Code);
        BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(3, 4), HandshakeSeed);
        await SendPacketAsync(payload, token).ConfigureAwait(false);
    }

    private async Task ReceiveLoopAsync(CancellationToken token)
    {
        var buffer = new byte[2048];
        while (!token.IsCancellationRequested && !_disconnectRequested)
        {
            int read;
            try
            {
                read = await _stream.ReadAsync(buffer.AsMemory(0, buffer.Length), token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            if (read <= 0)
            {
                break;
            }

            AppendBuffer(buffer.AsSpan(0, read));

            if (!_handshakeValidated)
            {
                if (!TryProcessHandshake())
                {
                    continue;
                }
            }

            var continueLoop = await ProcessCommandsAsync(token).ConfigureAwait(false);
            if (!continueLoop)
            {
                break;
            }
        }
    }

    private bool TryProcessHandshake()
    {
        if (!EnsureAvailable(5))
        {
            return false;
        }

        var span = PeekSpan(5);
        var clientHandshake = BinaryPrimitives.ReadUInt32LittleEndian(span[..4]);
        var clientVersion = span[4];
        Consume(5);

        var expected = LegacySecurity.ComputeHandshake(HandshakeSeed);
        if (!LegacySecurity.ValidateHandshake(expected, clientHandshake))
        {
            _logger.Warning($"[{Code}] Handshake inválido, conexión cerrada.");
            _client.Client.Disconnect(reuseSocket: false);
            _disconnectRequested = true;
            return false;
        }

        if (clientVersion != LegacyConstants.LegacyClientVersion)
        {
            _logger.Warning($"[{Code}] Cliente con versión {clientVersion} incompatible.");
            _ = SendVersionMismatchAsync(clientVersion);
            _disconnectRequested = true;
            return false;
        }

        _handshakeValidated = true;
        _logger.Info($"[{Code}] Handshake completado (versión {clientVersion}).");
        return true;
    }

    private async Task<bool> ProcessCommandsAsync(CancellationToken token)
    {
        while (!_disconnectRequested && AvailableBytes > 0)
        {
            var opcode = (char)PeekByte(0);

            if (!_loggedIn)
            {
                if (opcode != '!')
                {
                    _logger.Warning($"[{Code}] Comando inesperado antes de iniciar sesión ('{opcode}').");
                    Consume(1);
                    continue;
                }

                if (!TryExtractLoginCommand(out var loginCommand))
                {
                    return true;
                }

                await HandleLoginCommandAsync(loginCommand, token).ConfigureAwait(false);
                continue;
            }

            switch (opcode)
            {
                case 'H':
                    if (!TryPeekCommandString(1, out var localChat, out var total))
                    {
                        return true;
                    }
                    Consume(total);
                    await HandleLocalChatAsync(localChat, token).ConfigureAwait(false);
                    break;

            case 'T':
                if (!TryPeekCommandString(1, out var globalChat, out var totalLength))
                {
                    return true;
                }
                Consume(totalLength);
                await HandleGlobalChatAsync(globalChat, token).ConfigureAwait(false);
                break;
            case 'G':
                    if (!TryPeekCommandString(1, out var clanChat, out var clanCommandLength))
                    {
                        return true;
                    }
                    Consume(clanCommandLength);
                    await HandleClanChatAsync(clanChat, token).ConfigureAwait(false);
                    break;
            case 'K':
                    if (!TryExtractClanCommand(out var clanCommand))
                    {
                        return true;
                    }
                    await HandleClanCommandAsync(clanCommand, token).ConfigureAwait(false);
                    break;

            case 'X':
                    if (!EnsureAvailable(2))
                    {
                        return true;
                    }
                    var subOp = (char)PeekByte(1);
                    if (subOp == '!')
                    {
                        Consume(2);
                        await HandleLogoutAsync(token).ConfigureAwait(false);
                    }
                    else
                    {
                        _logger.Warning($"[{Code}] Subcomando 'X{subOp}' no soportado.");
                        Consume(2);
                    }
                    break;

                default:
                    await FlushToCommandSinkAsync(token).ConfigureAwait(false);
                    return !_disconnectRequested;
            }
        }

        await FlushToCommandSinkAsync(token).ConfigureAwait(false);
        return !_disconnectRequested;
    }

    private bool TryExtractLoginCommand(out LoginCommand command)
    {
        command = default;
        const int passwordLength = 32;
        var headerLength = 1 + passwordLength + 1;
        if (!EnsureAvailable(headerLength))
        {
            return false;
        }

        var header = PeekSpan(headerLength);
        var loginLength = header[headerLength - 1];
        var totalLength = headerLength + loginLength;
        if (!EnsureAvailable(totalLength))
        {
            return false;
        }

        var span = PeekSpan(totalLength);
        var password = span.Slice(1, passwordLength).ToArray();
        var loginBytes = span.Slice(headerLength, loginLength).ToArray();
        Consume(totalLength);
        var login = LegacyConstants.LegacyEncoding.GetString(loginBytes);
        command = new LoginCommand(password, login);
        return true;
    }

    private async Task HandleLoginCommandAsync(LoginCommand command, CancellationToken token)
    {
        if (_loginName is not null)
        {
            await SendLegacyAsync("EY", token).ConfigureAwait(false);
            return;
        }

        var sanitizedLogin = LegacySecurity.SanitizeLogin(command.Login);
        if (_world.IsLoginActive(sanitizedLogin))
        {
            await SendLegacyAsync("EY", token).ConfigureAwait(false);
            return;
        }

        var account = await _accounts.TryReadAsync(sanitizedLogin, token).ConfigureAwait(false);
        if (account is null)
        {
            await SendLegacyAsync("EN", token).ConfigureAwait(false);
            return;
        }

        if (account.Data.State <= LegacyUserState.Baneado)
        {
            await SendLegacyAsync("EB", token).ConfigureAwait(false);
            return;
        }

        if (!PasswordsMatch(account.Data.PasswordHash, command.PasswordHash))
        {
            await SendLegacyAsync("EC", token).ConfigureAwait(false);
            return;
        }

        _account = account;
        _playerSnapshot = LegacyPlayerSnapshot.FromBytes(account.CharacterSnapshot);
        _hasSnapshot = true;
        EnsureValidSpawnLocation();
        _world.RegisterLogin(sanitizedLogin);
        _loginName = sanitizedLogin;
        _userState = account.Data.State;
        _avatarName = _playerSnapshot.GetAvatarName();
        _loggedIn = true;
        try
        {
            _playerContext = _world.RegisterPlayer(Code, sanitizedLogin, _avatarName, _userState, ref _playerSnapshot, out var spawnAdjusted);
            _spawnRepositioned = spawnAdjusted;
            if (spawnAdjusted)
            {
                _logger.Info($"[{Code}] Posición ajustada tras el login para evitar superposición ({_playerSnapshot.CodigoMapa}:{_playerSnapshot.CoordenadaX},{_playerSnapshot.CoordenadaY}).");
            }
        }
        catch (InvalidOperationException ex)
        {
            _logger.Error($"[{Code}] No se pudo reservar una posición libre para {_avatarName}.", ex);
            await SendInfoMessageAsync("Servidor lleno: no hay posiciones disponibles en el mapa actual.", token).ConfigureAwait(false);
            _disconnectRequested = true;
            return;
        }
        _sessionManager.Register(this);
        await BroadcastClanActivationAsync((byte)_playerSnapshot.Clan, token).ConfigureAwait(false);

        await SendLoginSequenceAsync(token).ConfigureAwait(false);
        await SendMapRefreshAsync(_playerSnapshot, token).ConfigureAwait(false);
        await SendInitialWorldStateAsync(token).ConfigureAwait(false);
        await NotifyArrivalAsync().ConfigureAwait(false);
    }

    private async Task SendLoginSequenceAsync(CancellationToken token)
    {
        if (!_hasSnapshot || _account is null)
        {
            return;
        }

        var payload = _playerSnapshot.BuildLoginPayload();
        var flags = (byte)(2 | (_options.VerificationMode ? 1 : 0));

        var builder = new StringBuilder(payload.Length + 2);
        builder.Append('@');
        builder.Append((char)flags);
        builder.Append(payload);

        await SendLegacyAsync(builder.ToString(), token).ConfigureAwait(false);
        await SendLegacyAsync("!", token).ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(_options.WelcomeMessage))
        {
            await SendInfoMessageAsync(_options.WelcomeMessage, token).ConfigureAwait(false);
        }

        await SendInfoMessageAsync("Servidor en migración a .NET: aún no hay mundo activo.", token).ConfigureAwait(false);
        await SendClanStatusAsync(token).ConfigureAwait(false);
        await SendMapEconomyStatusAsync(token).ConfigureAwait(false);
        await SendActiveClanListAsync(token).ConfigureAwait(false);
        await SendMapCastleStatusAsync(token).ConfigureAwait(false);

        if (_options.AllowMultipleSessions)
        {
            await SendLegacyAsync("I" + ((char)24).ToString(), token).ConfigureAwait(false);
        }

        if (_spawnRepositioned)
        {
            await SendInfoMessageAsync("Tu posición fue ajustada para evitar superposición con otros jugadores.", token).ConfigureAwait(false);
        }
    }

    private async Task SendInitialWorldStateAsync(CancellationToken token)
    {
        if (!_hasSnapshot || _playerContext is null)
        {
            return;
        }

        try
        {
            var builder = new LegacyAreaSnapshotBuilder(_world);
            var packets = builder.BuildRefreshPackets(_playerContext, _playerSnapshot);
            foreach (var payload in packets)
            {
                await _sessionManager.SendToSessionAsync(Code, payload).ConfigureAwait(false);
            }

            var broadcast = LegacyWorldPacketFactory.BuildMovementBroadcastPacket(Code, _playerSnapshot);
            await _sessionManager.BroadcastToMapAsync(_world, _playerSnapshot.CodigoMapa, broadcast, Code).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.Error($"[{Code}] Error enviando el estado inicial del mundo.", ex);
        }
    }

    private async Task SendMapBootstrapAsync(LegacyPlayerSnapshot snapshot, CancellationToken token)
    {
        try
        {
            var mapId = snapshot.CodigoMapa;
            var castle = _gameData.Castles.Castles.FirstOrDefault(c => c.MapId == mapId);
            var castleClan = castle?.ClanId ?? (byte)0;

            string? offlineClanName = null;
            LegacyClanBanner? banner = null;
            if (castle is not null && castle.ClanId <= LegacyConstants.MaxClans)
            {
                var clan = _gameData.Clans.Clans.FirstOrDefault(c => c.Id == castle.ClanId);
                if (clan is not null && clan.ActiveMembers == 0 && !string.IsNullOrWhiteSpace(clan.Name))
                {
                    offlineClanName = clan.Name;
                    banner = clan.Banner;
                }
            }

            var (weatherType, weatherIntensity, weatherDrift) = ResolveWeather(mapId);

            var payload = LegacyWorldPacketFactory.BuildMapBootstrapPacket(
                mapId,
                snapshot.CoordenadaX,
                snapshot.CoordenadaY,
                weatherType,
                weatherIntensity,
                weatherDrift,
                castleClan,
                offlineClanName,
                banner);

            await SendPacketAsync(payload, token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.Error($"[{Code}] Error enviando datos iniciales del mapa.", ex);
        }
    }

    private async Task SendInitialMapStateAsync(LegacyPlayerSnapshot snapshot, CancellationToken token)
    {
        var mapId = snapshot.CodigoMapa;
        await SendPlayerListsAsync(mapId, snapshot, token).ConfigureAwait(false);
        await SendMonsterListsAsync(mapId, snapshot, token).ConfigureAwait(false);
        await SendMerchantListAsync(mapId, token).ConfigureAwait(false);
        await SendBagListAsync(mapId, token).ConfigureAwait(false);
    }

    internal async Task SendMapRefreshAsync(LegacyPlayerSnapshot snapshot, CancellationToken token)
    {
        if (_lastBroadcastedMapId == snapshot.CodigoMapa)
        {
            return;
        }

        await SendMapBootstrapAsync(snapshot, token).ConfigureAwait(false);
        await SendInitialMapStateAsync(snapshot, token).ConfigureAwait(false);
        _lastBroadcastedMapId = snapshot.CodigoMapa;
    }

    private async Task NotifyArrivalAsync()
    {
        if (_spawnNotificationSent || !_hasSnapshot || _playerContext is null)
        {
            return;
        }

        try
        {
            var payload = LegacyWorldPacketFactory.BuildPlayerSpawnPacket(_playerContext);
            await _sessionManager.BroadcastToMapAsync(_world, _playerSnapshot.CodigoMapa, payload, Code).ConfigureAwait(false);
            _spawnNotificationSent = true;
        }
        catch (Exception ex)
        {
            _logger.Error($"[{Code}] Error notificando ingreso del jugador.", ex);
        }
    }

    private async Task SendPlayerListsAsync(byte mapId, LegacyPlayerSnapshot originSnapshot, CancellationToken token)
    {
        const int MaxNearX = 29;
        const int MaxNearY = 24;
        var near = new List<LegacyPlayerContext>();
        var far = new List<LegacyPlayerContext>();

        foreach (var other in _world.GetPlayers())
        {
            if (other.Code == Code)
            {
                continue;
            }

            var snapshot = other.Snapshot;
            if (snapshot.CodigoMapa != mapId)
            {
                continue;
            }

            var dx = Math.Abs(snapshot.CoordenadaX - originSnapshot.CoordenadaX);
            var dy = Math.Abs(snapshot.CoordenadaY - originSnapshot.CoordenadaY);
            if (dx <= MaxNearX && dy <= MaxNearY)
            {
                near.Add(other);
            }
            else
            {
                far.Add(other);
            }
        }

        if (near.Count > 0)
        {
            await SendPlayerPacketsAsync('J', near, includePosition: true, token).ConfigureAwait(false);
        }

        if (far.Count > 0)
        {
            await SendPlayerPacketsAsync('j', far, includePosition: false, token).ConfigureAwait(false);
        }
    }

    private async Task SendPlayerPacketsAsync(char opcode, List<LegacyPlayerContext> players, bool includePosition, CancellationToken token)
    {
        const int PacketLimit = 32;
        var index = 0;
        while (index < players.Count)
        {
            var count = Math.Min(PacketLimit, players.Count - index);
            var payload = BuildPlayerPacket(opcode, players, index, count, includePosition);
            await SendPacketAsync(payload, token).ConfigureAwait(false);
            index += count;
        }
    }

    private byte[] BuildPlayerPacket(char opcode, List<LegacyPlayerContext> players, int start, int count, bool includePosition)
    {
        var buffer = new List<byte>(2 + count * 24)
        {
            (byte)opcode,
            0
        };

        for (var i = 0; i < count; i++)
        {
            var player = players[start + i];
            var snapshot = player.Snapshot;
            AppendUInt16(buffer, player.Code);

            if (includePosition)
            {
                buffer.Add(snapshot.CoordenadaX);
                buffer.Add(snapshot.CoordenadaY);
                buffer.Add((byte)(snapshot.Direccion | ((snapshot.Accion & 0x0F) << 4)));
            }

            buffer.Add(snapshot.Animacion);
            AppendUInt16(buffer, unchecked((ushort)snapshot.Banderas));
            buffer.Add(snapshot.Nivel);

            var classInfo = (byte)(snapshot.Categoria & 0x0F);
            classInfo |= (byte)((snapshot.TipoMonstruo & 0x0F) << 4);
            if (snapshot.Hp > 0)
            {
                classInfo |= 0x80;
            }

            buffer.Add(classInfo);
            buffer.Add(unchecked((byte)snapshot.Comportamiento));
            buffer.Add(snapshot.Clan);
            buffer.Add(snapshot.Rostro);

            var name = snapshot.GetAvatarName();
            AppendString(buffer, name, 16);
        }

        buffer[1] = (byte)count;
        return buffer.ToArray();
    }

    private async Task SendMonsterListsAsync(byte mapId, LegacyPlayerSnapshot originSnapshot, CancellationToken token)
    {
        const int MaxNearX = 29;
        const int MaxNearY = 24;
        var entries = _world.GetMonsters(mapId);
        if (entries.Count == 0)
        {
            return;
        }

        var near = new List<LegacyMonsterInstance>();
        var far = new List<LegacyMonsterInstance>();
        foreach (var monster in entries)
        {
            var dx = Math.Abs(monster.X - originSnapshot.CoordenadaX);
            var dy = Math.Abs(monster.Y - originSnapshot.CoordenadaY);
            if (dx <= MaxNearX && dy <= MaxNearY)
            {
                near.Add(monster);
            }
            else
            {
                far.Add(monster);
            }
        }

        if (near.Count > 0)
        {
            await SendMonsterPacketsAsync('M', near, includePosition: true, token).ConfigureAwait(false);
        }

        if (far.Count > 0)
        {
            await SendMonsterPacketsAsync('m', far, includePosition: false, token).ConfigureAwait(false);
        }
    }

    private async Task SendMonsterPacketsAsync(char opcode, List<LegacyMonsterInstance> monsters, bool includePosition, CancellationToken token)
    {
        const int PacketLimit = 128;
        var index = 0;
        while (index < monsters.Count)
        {
            var count = Math.Min(PacketLimit, monsters.Count - index);
            var payload = BuildMonsterPacket(opcode, monsters, index, count, includePosition);
            await SendPacketAsync(payload, token).ConfigureAwait(false);
            index += count;
        }
    }

    private byte[] BuildMonsterPacket(char opcode, List<LegacyMonsterInstance> monsters, int start, int count, bool includePosition)
    {
        var perEntry = includePosition ? 8 : 5;
        var buffer = new List<byte>(2 + count * perEntry)
        {
            (byte)opcode,
            0
        };

        for (var i = 0; i < count; i++)
        {
            var monster = monsters[start + i];
            AppendUInt16(buffer, monster.Code);
            if (includePosition)
            {
                buffer.Add(monster.X);
                buffer.Add(monster.Y);
                buffer.Add(LegacyWorldPacketFactory.ComposeMonsterDirection(monster.Direction, monster.Action));
            }

            buffer.Add(monster.Animation);
            AppendUInt16(buffer, monster.Flags);
        }

        buffer[1] = (byte)count;
        return buffer.ToArray();
    }

    private async Task SendMerchantListAsync(byte mapId, CancellationToken token)
    {
        var merchants = _world.GetMerchantMonsters(mapId);
        if (merchants.Count == 0)
        {
            var empty = new[] { (byte)'#', (byte)0 };
            await SendPacketAsync(empty, token).ConfigureAwait(false);
            return;
        }

        var buffer = new List<byte>(2 + merchants.Count * 2)
        {
            (byte)'#',
            (byte)merchants.Count
        };

        foreach (var merchant in merchants)
        {
            AppendUInt16(buffer, merchant.Code);
        }

        await SendPacketAsync(buffer.ToArray(), token).ConfigureAwait(false);
    }

    private async Task SendBagListAsync(byte mapId, CancellationToken token)
    {
        var flags = _world.GetMapFlags(mapId);

        const int MaxBagsPerPacket = 240;
        var bags = _world.GroundItems.GetBags(mapId);
        if (bags.Count == 0)
        {
            var emptyPayload = new List<byte> { (byte)'&', 0, (byte)'k' };
            AppendInt32(emptyPayload, flags);
            await SendPacketAsync(emptyPayload.ToArray(), token).ConfigureAwait(false);
            return;
        }

        var index = 0;
        while (index < bags.Count)
        {
            var count = Math.Min(MaxBagsPerPacket, bags.Count - index);
            var payload = new List<byte>(2 + count * 3 + 5) { (byte)'&', 0 };

            for (var i = 0; i < count; i++)
            {
                var bag = bags[index + i];
                payload.Add(bag.X);
                payload.Add(bag.Y);
                payload.Add((byte)bag.BagType);
            }

            var isLast = index + count >= bags.Count;
            if (isLast)
            {
                payload.Add((byte)'k');
                AppendInt32(payload, flags);
            }

            payload[1] = (byte)count;
            await SendPacketAsync(payload.ToArray(), token).ConfigureAwait(false);
            index += count;
        }
    }

    private void EnsureValidSpawnLocation()
    {
        if (!_hasSnapshot)
        {
            return;
        }

        if (IsCurrentPositionValid())
        {
            return;
        }

        if (_world.TryGetRespawn(_playerSnapshot.CodigoMapa, out var respawnMap, out var respawnX, out var respawnY) &&
            _world.IsWalkable(respawnMap, respawnX, respawnY))
        {
            ApplySpawnPosition(respawnMap, respawnX, respawnY, "respawn del mapa");
            return;
        }

        if (TrySelectBasePosition(out var baseMap, out var baseX, out var baseY))
        {
            ApplySpawnPosition(baseMap, baseX, baseY, "posición base configurada");
            return;
        }

        ApplySpawnPosition(0, 0, 0, "posición por defecto");
    }

    private bool TrySelectBasePosition(out byte mapId, out byte x, out byte y)
    {
        mapId = 0;
        x = 0;
        y = 0;

        if (_options.BasePositions.Count == 0)
        {
            return false;
        }

        var race = _playerSnapshot.TipoMonstruo % 9;
        foreach (var position in _options.BasePositions)
        {
            if (position.RaceId != race && position.RaceId != 0)
            {
                continue;
            }

            if (!_world.TryGetMapDefinition(position.MapId, out _))
            {
                continue;
            }

            if (_world.IsWalkable(position.MapId, position.X, position.Y))
            {
                mapId = position.MapId;
                x = position.X;
                y = position.Y;
                return true;
            }
        }

        foreach (var position in _options.BasePositions)
        {
            if (!_world.TryGetMapDefinition(position.MapId, out _))
            {
                continue;
            }

            if (_world.IsWalkable(position.MapId, position.X, position.Y))
            {
                mapId = position.MapId;
                x = position.X;
                y = position.Y;
                return true;
            }
        }

        return false;
    }

    private bool IsCurrentPositionValid()
    {
        return _world.TryGetMapDefinition(_playerSnapshot.CodigoMapa, out _) &&
               _world.IsWalkable(_playerSnapshot.CodigoMapa, _playerSnapshot.CoordenadaX, _playerSnapshot.CoordenadaY);
    }

    private void ApplySpawnPosition(byte mapId, byte x, byte y, string reason)
    {
        var previousMap = _playerSnapshot.CodigoMapa;
        _playerSnapshot.CodigoMapa = mapId;
        _playerSnapshot.CoordenadaX = x;
        _playerSnapshot.CoordenadaY = y;
        _playerSnapshot.DestinoX = x;
        _playerSnapshot.DestinoY = y;
        _logger.Warning($"[{Code}] Posición inválida detectada (mapa {previousMap}); reubicado en mapa {mapId} ({x},{y}) [{reason}].");
    }

    private async Task HandleLocalChatAsync(string rawMessage, CancellationToken token)
    {
        var sanitized = SanitizeChatMessage(rawMessage);
        if (string.IsNullOrEmpty(sanitized))
        {
            return;
        }

        var truncated = TruncateChatMessage(sanitized);
        var builder = new StringBuilder(truncated.Length + 4);
        builder.Append('h');
        LegacyBinaryEncoding.AppendB2(builder, Code);
        builder.Append((char)truncated.Length);
        builder.Append(truncated);
        var payload = LegacyConstants.LegacyEncoding.GetBytes(builder.ToString());
        await _sessionManager.BroadcastToMapAsync(_world, _playerSnapshot.CodigoMapa, payload).ConfigureAwait(false);
    }

    private async Task HandleClanChatAsync(string rawMessage, CancellationToken token)
    {
        if (_playerSnapshot.Clan > LegacyConstants.MaxClans)
        {
            await SendInfoMessageAsync("No perteneces a un clan.", token).ConfigureAwait(false);
            return;
        }

        var sanitized = SanitizeChatMessage(rawMessage);
        if (string.IsNullOrEmpty(sanitized))
        {
            return;
        }

        var truncated = TruncateChatMessage(sanitized);
        var composed = $"{_avatarName}: {truncated}";
        var encoded = LegacyConstants.LegacyEncoding.GetBytes(composed);
        var length = (byte)Math.Min(79, encoded.Length);
        var payload = new byte[3 + length];
        payload[0] = (byte)'I';
        payload[1] = (byte)'G';
        payload[2] = length;
        Array.Copy(encoded, 0, payload, 3, length);
        await BroadcastClanAsync(_playerSnapshot.Clan, payload, token).ConfigureAwait(false);
    }

    private bool TryExtractClanCommand(out ClanCommand command)
    {
        command = default;
        if (!EnsureAvailable(2))
        {
            return false;
        }

        var subCommand = (char)PeekByte(1);
        int totalLength;
        switch (subCommand)
        {
            case 'P':
                totalLength = 10;
                break;
            case '(':
                totalLength = 3;
                break;
            case 'R':
            case 'D':
                totalLength = 4;
                break;
            case 'L':
            case 'B':
            case 'T':
                totalLength = 2;
                break;
            case 'M':
                totalLength = 3;
                break;
            case 'G':
            case 'i':
            case 'S':
                totalLength = 4;
                break;
            case 'E':
            case 'g':
                totalLength = 5;
                break;
            case 'N':
                if (!EnsureAvailable(3))
                {
                    return false;
                }
                var nameLength = PeekByte(2);
                totalLength = 3 + nameLength;
                if (!EnsureAvailable(totalLength))
                {
                    return false;
                }
                break;
            default:
                totalLength = 2;
                break;
        }

        if (!EnsureAvailable(totalLength))
        {
            return false;
        }

        var buffer = PeekSpan(totalLength).ToArray();
        Consume(totalLength);
        command = new ClanCommand(subCommand, buffer.AsMemory(2));
        return true;
    }

    private async Task HandleClanCommandAsync(ClanCommand command, CancellationToken token)
    {
        switch (command.SubCommand)
        {
            case 'P':
                await HandleClanBannerChangeAsync(command.Payload.Span, token).ConfigureAwait(false);
                break;
            case '(':
                await HandleClanColorChangeAsync(command.Payload.Span, token).ConfigureAwait(false);
                break;
            case 'N':
                await HandleClanRenameAsync(command.Payload, token).ConfigureAwait(false);
                break;
            case 'R':
                await HandleClanRecruitAsync(command.Payload.Span, token).ConfigureAwait(false);
                break;
            case 'D':
                await HandleClanRemovalAsync(command.Payload.Span, token).ConfigureAwait(false);
                break;
            case 'L':
                await HandleClanMemberListAsync(token).ConfigureAwait(false);
                break;
            default:
                await SendInfoMessageAsync($"El comando de clan 'K{command.SubCommand}' aún no está disponible en la migración .NET.", token).ConfigureAwait(false);
                break;
        }
    }

    private async Task HandleClanBannerChangeAsync(ReadOnlySpan<byte> payload, CancellationToken token)
    {
        if (payload.Length < 8)
        {
            await SendInfoMessageAsync("Comando de clan incompleto.", token).ConfigureAwait(false);
            return;
        }

        if (!TryEnsureClanLeader(out var clan, out var error))
        {
            await SendInfoMessageAsync(error ?? "No perteneces a un clan.", token).ConfigureAwait(false);
            return;
        }

        var primary = BinaryPrimitives.ReadUInt32LittleEndian(payload[..4]);
        var secondary = BinaryPrimitives.ReadUInt32LittleEndian(payload.Slice(4, 4));

        if (!_gameData.Clans.TrySetBanner(clan.Id, primary, secondary, out _, out var updateError))
        {
            await SendInfoMessageAsync(updateError ?? "No se pudo actualizar el pendón del clan.", token).ConfigureAwait(false);
            return;
        }

        await BroadcastClanBannerAsync(clan.Id, primary, secondary).ConfigureAwait(false);
        await SendInfoMessageAsync("Pendón actualizado.", token).ConfigureAwait(false);
    }

    private async Task HandleClanColorChangeAsync(ReadOnlySpan<byte> payload, CancellationToken token)
    {
        if (payload.Length < 1)
        {
            await SendInfoMessageAsync("Comando de clan incompleto.", token).ConfigureAwait(false);
            return;
        }

        if (!TryEnsureClanLeader(out var clan, out var error))
        {
            await SendInfoMessageAsync(error ?? "No perteneces a un clan.", token).ConfigureAwait(false);
            return;
        }

        var color = payload[0];
        if (!_gameData.Clans.TrySetColor(clan.Id, color, out _, out var updateError))
        {
            await SendInfoMessageAsync(updateError ?? "No se pudo actualizar el color del clan.", token).ConfigureAwait(false);
            return;
        }

        await BroadcastClanColorAsync(clan.Id, color).ConfigureAwait(false);
        await SendInfoMessageAsync("Color del clan actualizado.", token).ConfigureAwait(false);
    }

    private async Task HandleClanRenameAsync(ReadOnlyMemory<byte> payload, CancellationToken token)
    {
        if (payload.Length <= 0)
        {
            await SendInfoMessageAsync("Nombre del clan inválido.", token).ConfigureAwait(false);
            return;
        }

        var span = payload.Span;
        var length = span[0];
        if (length == 0 || payload.Length - 1 < length)
        {
            await SendInfoMessageAsync("Nombre del clan inválido.", token).ConfigureAwait(false);
            return;
        }

        if (!TryEnsureClanLeader(out var clan, out var error))
        {
            await SendInfoMessageAsync(error ?? "No perteneces a un clan.", token).ConfigureAwait(false);
            return;
        }

        var nameBytes = span.Slice(1, length);
        var desiredName = LegacyConstants.LegacyEncoding.GetString(nameBytes);
        if (!_gameData.Clans.TryRename(clan.Id, desiredName, out var updated, out var updateError))
        {
            await SendInfoMessageAsync(updateError ?? "No se pudo renombrar el clan.", token).ConfigureAwait(false);
            return;
        }

        var effective = updated ?? clan;
        await BroadcastClanRenameAsync(effective.Id, effective.Name).ConfigureAwait(false);
        await SendInfoMessageAsync($"El clan ahora se llama {effective.Name}.", token).ConfigureAwait(false);
    }

    private async Task HandleClanRecruitAsync(ReadOnlySpan<byte> payload, CancellationToken token)
    {
        if (payload.Length < 2)
        {
            await SendInfoMessageAsync("Comando de clan incompleto.", token).ConfigureAwait(false);
            return;
        }

        if (!TryEnsureClanLeader(out var clan, out var error))
        {
            await SendInfoMessageAsync(error ?? "No perteneces a un clan.", token).ConfigureAwait(false);
            return;
        }

        var recruitCode = BinaryPrimitives.ReadUInt16LittleEndian(payload);
        if (recruitCode == Code)
        {
            await SendInfoMessageAsync("Ya perteneces a tu clan.", token).ConfigureAwait(false);
            return;
        }

        if (!_world.TryGetPlayer(recruitCode, out var recruit) || recruit is null)
        {
            await SendInfoMessageAsync("No se encontró al jugador a reclutar.", token).ConfigureAwait(false);
            return;
        }

        if (recruit.Snapshot.Clan <= LegacyConstants.MaxClans)
        {
            await SendInfoMessageAsync("Ese jugador ya pertenece a un clan.", token).ConfigureAwait(false);
            return;
        }

        var recruitSnapshot = recruit.Snapshot;
        recruitSnapshot.Clan = clan.Id;
        recruit.UpdateSnapshot(recruitSnapshot);

        if (_sessionManager.TryGetContext(recruitCode, out var recruitConnection) && recruitConnection is not null)
        {
            await recruitConnection.ApplyClanAssignmentAsync(recruitSnapshot, clan, token).ConfigureAwait(false);
            await recruitConnection.SendInfoMessageAsync($"Fuiste reclutado en el clan {clan.Name}.", token).ConfigureAwait(false);
        }

        var assignment = LegacyWorldPacketFactory.BuildClanAssignmentPacket(clan.Id, recruitCode);
        await _sessionManager.BroadcastToMapAsync(_world, recruitSnapshot.CodigoMapa, assignment).ConfigureAwait(false);

        _ = _gameData.Clans.TryAdjustActiveMembers(clan.Id, 1, out _, out _);
        await SendInfoMessageAsync($"Reclutaste a {recruit.AvatarName}.", token).ConfigureAwait(false);
    }

    private async Task HandleClanRemovalAsync(ReadOnlySpan<byte> payload, CancellationToken token)
    {
        if (payload.Length < 2)
        {
            await SendInfoMessageAsync("Comando de clan incompleto.", token).ConfigureAwait(false);
            return;
        }

        if (!TryResolveClan(out var clan, out var error) || clan is null)
        {
            await SendInfoMessageAsync(error ?? "No perteneces a un clan.", token).ConfigureAwait(false);
            return;
        }

        var targetCode = BinaryPrimitives.ReadUInt16LittleEndian(payload);
        if (targetCode == Code)
        {
            if (string.Equals(clan.Leader, _avatarName, StringComparison.Ordinal))
            {
                await HandleClanDisbandAsync(clan, token).ConfigureAwait(false);
                return;
            }

            if (_playerContext is null)
            {
                await SendInfoMessageAsync("No se pudo actualizar tu personaje.", token).ConfigureAwait(false);
                return;
            }

            await RemovePlayerFromClanAsync(clan.Id, _playerContext, "Abandonaste tu clan.", token).ConfigureAwait(false);
            await SendInfoMessageAsync("Abandonaste tu clan.", token).ConfigureAwait(false);
            return;
        }

        if (!string.Equals(clan.Leader, _avatarName, StringComparison.Ordinal))
        {
            await SendInfoMessageAsync("Solo el líder puede despedir integrantes del clan.", token).ConfigureAwait(false);
            return;
        }

        if (!_world.TryGetPlayer(targetCode, out var target) || target is null)
        {
            await SendInfoMessageAsync("No se encontró al jugador a despedir.", token).ConfigureAwait(false);
            return;
        }

        if (target.Snapshot.Clan != clan.Id)
        {
            await SendInfoMessageAsync("Ese jugador no pertenece a tu clan.", token).ConfigureAwait(false);
            return;
        }

        await RemovePlayerFromClanAsync(clan.Id, target, $"El clan {clan.Name} te expulsó.", token).ConfigureAwait(false);
        await SendInfoMessageAsync($"Expulsaste a {target.AvatarName}.", token).ConfigureAwait(false);
    }

    private async Task HandleClanMemberListAsync(CancellationToken token)
    {
        if (!TryResolveClan(out var clan, out var error) || clan is null)
        {
            await SendInfoMessageAsync(error ?? "No perteneces a un clan.", token).ConfigureAwait(false);
            return;
        }

        var members = new List<string>();
        var leaderName = clan.Leader;
        foreach (var player in _world.GetPlayers())
        {
            if (player.Snapshot.Clan != clan.Id)
            {
                continue;
            }

            if (!string.IsNullOrEmpty(leaderName) && string.Equals(player.AvatarName, leaderName, StringComparison.Ordinal))
            {
                members.Add("*" + player.AvatarName);
            }
            else
            {
                members.Add(player.AvatarName);
            }
        }

        var joined = string.Join(',', members);
        var encoded = LegacyConstants.LegacyEncoding.GetBytes(joined);
        var length = (byte)Math.Min(255, encoded.Length);
        var payload = new byte[3 + length];
        payload[0] = (byte)'I';
        payload[1] = (byte)'*';
        payload[2] = length;
        if (length > 0)
        {
            Array.Copy(encoded, 0, payload, 3, length);
        }

        await SendPacketAsync(payload, token).ConfigureAwait(false);
    }

    private async Task HandleClanDisbandAsync(LegacyClanInfo clan, CancellationToken token)
    {
        var members = _world.GetPlayers()
            .Where(player => player.Snapshot.Clan == clan.Id)
            .ToArray();

        foreach (var member in members)
        {
            await RemovePlayerFromClanAsync(clan.Id, member, "Tu clan fue disuelto.", token, adjustCount: false).ConfigureAwait(false);
        }

        if (!_gameData.Clans.TryRemoveClan(clan.Id, out var error))
        {
            await SendInfoMessageAsync(error ?? "No se pudo eliminar el registro del clan.", token).ConfigureAwait(false);
            return;
        }

        await BroadcastClanBannerAsync(clan.Id, 0, 0).ConfigureAwait(false);
        await BroadcastClanColorAsync(clan.Id, 255).ConfigureAwait(false);
        await SendInfoMessageAsync("Disolviste tu clan.", token).ConfigureAwait(false);
    }

    private async Task RemovePlayerFromClanAsync(byte clanId, LegacyPlayerContext memberContext, string? notification, CancellationToken token, bool adjustCount = true)
    {
        var snapshot = memberContext.Snapshot;
        snapshot.Clan = LegacyConstants.NoClanId;
        memberContext.UpdateSnapshot(snapshot);

        if (_sessionManager.TryGetContext(memberContext.Code, out var connection) && connection is not null)
        {
            await connection.ApplyClanAssignmentAsync(snapshot, null, token).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(notification))
            {
                await connection.SendInfoMessageAsync(notification!, token).ConfigureAwait(false);
            }
        }

        var assignment = LegacyWorldPacketFactory.BuildClanAssignmentPacket(LegacyConstants.NoClanId, memberContext.Code);
        await _sessionManager.BroadcastToMapAsync(_world, snapshot.CodigoMapa, assignment).ConfigureAwait(false);

        if (adjustCount)
        {
            _ = _gameData.Clans.TryAdjustActiveMembers(clanId, -1, out _, out _);
        }
    }

    private bool TryEnsureClanLeader(out LegacyClanInfo clan, out string? error)
    {
        if (!TryResolveClan(out clan, out error) || clan is null)
        {
            clan = default!;
            return false;
        }

        if (!string.Equals(clan.Leader, _avatarName, StringComparison.Ordinal))
        {
            error = "No eres el líder de tu clan.";
            clan = default!;
            return false;
        }

        error = null;
        return true;
    }

    private bool TryResolveClan(out LegacyClanInfo? clan, out string? error)
    {
        clan = null;
        error = null;

        if (_playerSnapshot.Clan > LegacyConstants.MaxClans)
        {
            error = "No perteneces a ningún clan.";
            return false;
        }

        clan = _gameData.Clans.Clans.FirstOrDefault(c => c.Id == _playerSnapshot.Clan);
        if (clan is null)
        {
            error = $"Clan #{_playerSnapshot.Clan} no encontrado en clanes.dat.";
            return false;
        }

        return true;
    }

    private async Task BroadcastClanBannerAsync(byte clanId, uint primary, uint secondary)
    {
        var payload = new List<byte> { (byte)'I', (byte)'P', clanId };
        AppendUInt32(payload, primary);
        AppendUInt32(payload, secondary);
        await _sessionManager.BroadcastAsync(payload.ToArray(), excludedCode: null).ConfigureAwait(false);
    }

    private async Task BroadcastClanColorAsync(byte clanId, byte color)
    {
        var payload = new byte[] { (byte)'I', (byte)'(', clanId, color };
        await _sessionManager.BroadcastAsync(payload, excludedCode: null).ConfigureAwait(false);
    }

    private async Task BroadcastClanRenameAsync(byte clanId, string name)
    {
        var encoded = LegacyConstants.LegacyEncoding.GetBytes(name ?? string.Empty);
        var length = (byte)Math.Min(255, encoded.Length);
        var payload = new byte[4 + length];
        payload[0] = (byte)'I';
        payload[1] = (byte)'N';
        payload[2] = clanId;
        payload[3] = length;
        if (length > 0)
        {
            Array.Copy(encoded, 0, payload, 4, length);
        }

        await _sessionManager.BroadcastAsync(payload, excludedCode: null).ConfigureAwait(false);
    }

    private async Task HandleGlobalChatAsync(string rawMessage, CancellationToken token)
    {
        if (!_options.AllowGlobalCommunication && _userState < LegacyUserState.Moderador)
        {
            await SendLegacyAsync("I" + ((char)16).ToString(), token).ConfigureAwait(false);
            return;
        }

        var sanitized = SanitizeChatMessage(rawMessage);
        if (string.IsNullOrEmpty(sanitized))
        {
            return;
        }

        var truncated = TruncateChatMessage(sanitized);
        var composed = $"{_avatarName}: {truncated}";
        var encoded = LegacyConstants.LegacyEncoding.GetBytes(composed);
        var length = (byte)Math.Min(79, encoded.Length);
        var payload = new byte[3 + length];
        payload[0] = (byte)'I';
        payload[1] = (byte)'H';
        payload[2] = length;
        Array.Copy(encoded, 0, payload, 3, length);
        await _sessionManager.BroadcastAsync(payload, excludedCode: null).ConfigureAwait(false);
    }

    private async Task HandleLogoutAsync(CancellationToken token)
    {
        await SendLegacyAsync("I" + ((char)9).ToString(), token).ConfigureAwait(false);
        _disconnectRequested = true;
    }

    private async Task SendVersionMismatchAsync(byte reportedVersion)
    {
        try
        {
            var payload = new[] { (byte)'E', (byte)'V', LegacyConstants.LegacyClientVersion };
            await SendPacketAsync(payload).ConfigureAwait(false);
        }
        catch
        {
            // ignore
        }
        finally
        {
            _logger.Info($"[{Code}] Versión rechazada ({reportedVersion}).");
            _client.Close();
        }
    }

    private Task SendLegacyAsync(string data, CancellationToken token)
    {
        var buffer = new byte[data.Length];
        for (var i = 0; i < data.Length; i++)
        {
            buffer[i] = (byte)(data[i] & 0xFF);
        }

        return SendPacketAsync(buffer, token);
    }

    private Task SendInfoMessageAsync(string message, CancellationToken token)
    {
        var encoded = LegacyConstants.LegacyEncoding.GetBytes(message);
        var length = (byte)Math.Min(255, encoded.Length);
        var payload = new byte[3 + length];
        payload[0] = (byte)'I';
        payload[1] = (byte)'G';
        payload[2] = length;
        Array.Copy(encoded, 0, payload, 3, length);
        return SendPacketAsync(payload, token);
    }

    private async Task SendClanStatusAsync(CancellationToken token)
    {
        if (!_hasSnapshot)
        {
            return;
        }

        if (_playerSnapshot.Clan > LegacyConstants.MaxClans)
        {
            await SendInfoMessageAsync("No perteneces a ningún clan.", token).ConfigureAwait(false);
            return;
        }

        var clan = _gameData.Clans.Clans.FirstOrDefault(c => c.Id == _playerSnapshot.Clan);
        if (clan is null)
        {
            await SendInfoMessageAsync($"Clan #{_playerSnapshot.Clan} no encontrado en clanes.dat.", token).ConfigureAwait(false);
            return;
        }

        await SendInfoMessageAsync($"Clan: {clan.Name} (líder {clan.Leader}).", token).ConfigureAwait(false);

        var castles = _gameData.Castles.Castles
            .Where(c => c.ClanId == clan.Id)
            .Select(c => c.MapId)
            .ToArray();
        if (castles.Length > 0)
        {
            var mapList = string.Join(", ", castles.Select(m => $"#{m}"));
            await SendInfoMessageAsync($"Castillos controlados: {mapList}.", token).ConfigureAwait(false);
        }
    }

    private async Task SendMapEconomyStatusAsync(CancellationToken token)
    {
        if (!_hasSnapshot)
        {
            return;
        }

        var mapId = _playerSnapshot.CodigoMapa;
        if (_gameData.Prices.Inflations.TryGetValue(mapId, out var merchants) && merchants.Count > 0)
        {
            await SendInfoMessageAsync($"Mapa #{mapId}: {merchants.Count} comerciantes con precios personalizados.", token).ConfigureAwait(false);
        }

        var castle = _gameData.Castles.Castles.FirstOrDefault(c => c.MapId == mapId);
        if (castle is not null)
        {
            var owner = castle.ClanId <= LegacyConstants.MaxClans
                ? _gameData.Clans.Clans.FirstOrDefault(c => c.Id == castle.ClanId)?.Name ?? $"Clan #{castle.ClanId}"
                : "Sin clan";

            await SendInfoMessageAsync($"Castillo local: dueño {owner}, impuestos {castle.Taxes}%.", token).ConfigureAwait(false);
        }
    }

    private async Task SendActiveClanListAsync(CancellationToken token)
    {
        var active = _gameData.Clans.Clans
            .Where(c => c.ActiveMembers > 0 || !string.IsNullOrWhiteSpace(c.Leader))
            .Take(255)
            .ToArray();

        if (active.Length == 0)
        {
            return;
        }

        var buffer = new List<byte>(2 + active.Length * 12)
        {
            (byte)'I',
            (byte)'k',
            0 // placeholder for count
        };

        foreach (var clan in active)
        {
            buffer.Add(clan.Id);
            AppendUInt32(buffer, clan.Banner.ColorPrimary);
            AppendUInt32(buffer, clan.Banner.ColorSecondary);
            buffer.Add(clan.Color);

            var encodedName = LegacyConstants.LegacyEncoding.GetBytes(clan.Name ?? string.Empty);
            var length = (byte)Math.Min(255, encodedName.Length);
            buffer.Add(length);
            buffer.AddRange(encodedName.AsSpan(0, length).ToArray());
        }

        buffer[2] = (byte)active.Length;
        await SendPacketAsync(buffer.ToArray(), token).ConfigureAwait(false);
    }

    private async Task SendMapCastleStatusAsync(CancellationToken token)
    {
        if (!_hasSnapshot)
        {
            return;
        }

        var mapId = _playerSnapshot.CodigoMapa;
        var castle = _gameData.Castles.Castles.FirstOrDefault(c => c.MapId == mapId);
        if (castle is null)
        {
            return;
        }

        var payload = new byte[3];
        payload[0] = (byte)'I';
        payload[1] = (byte)'Q';
        payload[2] = castle.ClanId <= LegacyConstants.MaxClans ? castle.ClanId : (byte)0xFF;
        await SendPacketAsync(payload, token).ConfigureAwait(false);
    }

    private static void AppendUInt32(List<byte> buffer, uint value)
    {
        buffer.Add((byte)(value & 0xFF));
        buffer.Add((byte)((value >> 8) & 0xFF));
        buffer.Add((byte)((value >> 16) & 0xFF));
        buffer.Add((byte)((value >> 24) & 0xFF));
    }

    private static void AppendUInt16(List<byte> buffer, ushort value)
    {
        buffer.Add((byte)(value & 0xFF));
        buffer.Add((byte)((value >> 8) & 0xFF));
    }

    private static void AppendInt32(List<byte> buffer, int value)
    {
        var span = stackalloc byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(span, value);
        buffer.Add(span[0]);
        buffer.Add(span[1]);
        buffer.Add(span[2]);
        buffer.Add(span[3]);
    }

    private static void AppendString(List<byte> buffer, string? text, int maxLength)
    {
        var safe = text ?? string.Empty;
        var encoded = LegacyConstants.LegacyEncoding.GetBytes(safe);
        var length = (byte)Math.Min(maxLength, encoded.Length);
        buffer.Add(length);
        if (length > 0)
        {
            buffer.AddRange(encoded.AsSpan(0, length).ToArray());
        }
    }

    private (byte WeatherType, byte Intensity, sbyte Drift) ResolveWeather(byte mapId)
    {
        var (globalType, globalIntensity, globalDrift) = _world.GetGlobalWeather();
        if (globalType != LegacyWeatherType.Normal)
        {
            return ((byte)globalType, globalIntensity, globalDrift);
        }

        if (_world.TryGetMapDefinition(mapId, out var definition) && definition is not null)
        {
            if ((definition.Flags & LegacyMapFlags.AlwaysNight) != 0)
            {
                return ((byte)LegacyWeatherType.Night, (byte)255, 0);
            }
        }

        return ((byte)LegacyWeatherType.Normal, 0, 0);
    }

    private async Task BroadcastClanAsync(byte clanId, ReadOnlyMemory<byte> payload, CancellationToken token)
    {
        var players = _world.GetPlayers();
        foreach (var player in players)
        {
            if (player.Snapshot.Clan != clanId)
            {
                continue;
            }

            await _sessionManager.SendToSessionAsync(player.Code, payload).ConfigureAwait(false);
        }
    }

    internal async Task ApplyClanAssignmentAsync(LegacyPlayerSnapshot snapshot, LegacyClanInfo? clan, CancellationToken token)
    {
        _playerSnapshot = snapshot;
        if (_account is null)
        {
            return;
        }

        try
        {
            _account.Data.ClanIdentifier = clan?.ClanIdentifier ?? 0;
            var bytes = snapshot.ToByteArray();
            Buffer.BlockCopy(bytes, 0, _account.CharacterSnapshot, 0, bytes.Length);
            await _accounts.WriteAsync(_account, token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.Error($"[{Code}] Error guardando la cuenta tras actualizar el clan.", ex);
        }
    }

    private Task BroadcastClanActivationAsync(byte clanId, CancellationToken token)
    {
        if (clanId > LegacyConstants.MaxClans)
        {
            return Task.CompletedTask;
        }

        var clan = _gameData.Clans.Clans.FirstOrDefault(c => c.Id == clanId);
        if (clan is null)
        {
            return Task.CompletedTask;
        }

        var payload = LegacyWorldPacketFactory.BuildClanActivationPacket(clan);
        return SendPacketAsync(payload, token);
    }

    private static bool PasswordsMatch(byte[] stored, byte[] provided)
    {
        if (stored.Length != provided.Length)
        {
            return false;
        }

        var match = true;
        for (var i = 0; i < stored.Length; i++)
        {
            match &= stored[i] == provided[i];
        }

        return match;
    }

    private static uint CreateHandshakeSeed()
    {
        Span<byte> buffer = stackalloc byte[4];
        RandomNumberGenerator.Fill(buffer);
        return BinaryPrimitives.ReadUInt32LittleEndian(buffer);
    }

    private void AppendBuffer(ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty)
        {
            return;
        }

        _buffer.AddRange(data.ToArray());
    }

    private int AvailableBytes => _buffer.Count - _bufferOffset;

    private bool EnsureAvailable(int count) => AvailableBytes >= count;

    private ReadOnlySpan<byte> PeekSpan(int count) =>
        CollectionsMarshal.AsSpan(_buffer).Slice(_bufferOffset, count);

    private byte PeekByte(int offset) =>
        CollectionsMarshal.AsSpan(_buffer)[_bufferOffset + offset];

    private bool TryPeekCommandString(int headerOffset, out string message, out int totalLength)
    {
        message = string.Empty;
        totalLength = 0;
        if (!EnsureAvailable(headerOffset + 1))
        {
            return false;
        }

        var length = PeekByte(headerOffset);
        var required = headerOffset + 1 + length;
        if (!EnsureAvailable(required))
        {
            return false;
        }

        var span = PeekSpan(required);
        message = LegacyConstants.LegacyEncoding.GetString(span.Slice(headerOffset + 1, length));
        totalLength = required;
        return true;
    }

    private void Consume(int count)
    {
        _bufferOffset += count;
        CompactBufferIfNeeded();
    }

    private void CompactBufferIfNeeded()
    {
        if (_bufferOffset == 0)
        {
            return;
        }

        if (_bufferOffset < 1024 && _bufferOffset < _buffer.Count / 2)
        {
            return;
        }

        _buffer.RemoveRange(0, _bufferOffset);
        _bufferOffset = 0;
    }

    private static string SanitizeChatMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(message.Length);
        foreach (var ch in message)
        {
            if (ch >= 32 && ch != 127)
            {
                builder.Append(ch);
            }
        }

        return builder.ToString().Trim();
    }

    private static string TruncateChatMessage(string message)
    {
        var limit = Math.Min(79, message.Length);
        return message[..limit];
    }

    private async Task FlushToCommandSinkAsync(CancellationToken token)
    {
        var available = AvailableBytes;
        if (available <= 0)
        {
            return;
        }

        var payload = PeekSpan(available).ToArray();
        Consume(available);
        if (payload.Length > 0)
        {
            await _commandSink.EnqueueAsync(Code, payload, token).ConfigureAwait(false);
        }
    }

    private async Task NotifyDepartureAsync()
    {
        if (_departureNotified || !_loggedIn || !_hasSnapshot)
        {
            return;
        }

        _departureNotified = true;

        try
        {
            var payload = LegacyWorldPacketFactory.BuildPlayerRemovalPacket(Code);
            await _sessionManager.BroadcastToMapAsync(_world, _playerSnapshot.CodigoMapa, payload, Code).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.Error($"[{Code}] Error notificando salida del jugador.", ex);
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            _client.Dispose();
            await _stream.DisposeAsync().ConfigureAwait(false);
        }
        finally
        {
            _sessionManager.Unregister(Code);
            _world.ReleaseLogin(_loginName);
            _world.RemovePlayer(Code);
            _world.ReleaseSlot(Code);
        }
    }

    internal async Task SendPacketAsync(ReadOnlyMemory<byte> payload, CancellationToken token = default)
    {
        await _writeLock.WaitAsync(token).ConfigureAwait(false);
        try
        {
            await _stream.WriteAsync(payload, token).ConfigureAwait(false);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    private readonly record struct LoginCommand(byte[] PasswordHash, string Login);

    private readonly record struct ClanCommand(char SubCommand, ReadOnlyMemory<byte> Payload);
}
