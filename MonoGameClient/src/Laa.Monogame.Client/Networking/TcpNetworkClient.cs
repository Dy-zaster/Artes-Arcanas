using System;
using System.Buffers.Binary;
using System.IO;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Laa.Monogame.Client.Networking;

/// <summary>
/// Streams raw packets to/from the legacy Delphi server. This implementation only
/// establishes the socket, relays bytes, and exposes hooks for future handshake/login
/// serialization — callers still need to build the legacy envelopes.
/// </summary>
public sealed class TcpNetworkClient : INetworkClient
{
    private static readonly object LogSync = new();
    private static readonly string LogPath = Path.Combine(AppContext.BaseDirectory, "log.txt");
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private TcpClient? _client;
    private NetworkStream? _stream;
    private CancellationTokenSource? _cts;
    private Task? _receiveTask;
    private LegacyLoginConfig _loginConfig = LegacyLoginConfig.Load(AppContext.BaseDirectory);
    private ushort _connectionCode;

    public event EventHandler<NetworkEventArgs>? MessageReceived;

    static TcpNetworkClient()
    {
        try
        {
            File.WriteAllText(LogPath, $"[{DateTime.Now:O}] TcpNetworkClient log inicializado.{Environment.NewLine}");
        }
        catch
        {
            // Logging failures shouldn't crash the game.
        }
    }

    public async Task ConnectAsync(string host, int port)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            throw new ArgumentException("Host must be provided.", nameof(host));
        }

        await DisconnectAsync().ConfigureAwait(false);

        Log($"Conectando a {host}:{port}...");

        try
        {
            _client = new TcpClient();
            await _client.ConnectAsync(host, port).ConfigureAwait(false);
            _stream = _client.GetStream();
            Log("Socket TCP abierto. Esperando desafío '|'.");

            var (connectionCode, serverSeed) = await ReadBootstrapChallengeAsync().ConfigureAwait(false);
            _connectionCode = connectionCode;
            Log($"Recibido desafío: codigo={connectionCode}, seed={serverSeed}.");
            await SendBootstrapAsync(serverSeed).ConfigureAwait(false);
            Log("Handshake/login enviado.");

            _cts = new CancellationTokenSource();
            _receiveTask = Task.Run(() => ReceiveLoopAsync(_cts.Token));
            Log("ReceiveLoop iniciado.");
        }
        catch (Exception ex)
        {
            Log($"ERROR durante ConnectAsync: {ex}");
            await DisconnectAsync().ConfigureAwait(false);
            throw;
        }
    }

    public async Task DisconnectAsync()
    {
        _cts?.Cancel();
        if (_receiveTask is not null)
        {
            try
            {
                await _receiveTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // ignored
            }
        }

        _cts?.Dispose();
        _cts = null;
        _receiveTask = null;

        _stream?.Dispose();
        _stream = null;

        _client?.Close();
        _client = null;
    }

    public async Task SendAsync(ReadOnlyMemory<byte> payload)
    {
        if (_stream is null)
        {
            throw new InvalidOperationException("Network stream not initialized.");
        }

        if (payload.IsEmpty)
        {
            return;
        }

        await _sendLock.WaitAsync().ConfigureAwait(false);
        try
        {
            await _stream.WriteAsync(payload).ConfigureAwait(false);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    public void Dispose()
    {
        _sendLock.Dispose();
        _stream?.Dispose();
        _client?.Dispose();
        _cts?.Dispose();
    }

    private async Task ReceiveLoopAsync(CancellationToken token)
    {
        if (_stream is null)
        {
            return;
        }

        var buffer = new byte[4096];
        Log("ReceiveLoop activo.");
        while (!token.IsCancellationRequested)
        {
            int bytesRead;
            try
            {
                bytesRead = await _stream.ReadAsync(buffer.AsMemory(0, buffer.Length), token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            if (bytesRead == 0)
            {
                Log("El servidor cerró la conexión.");
                break;
            }

            var payload = new byte[bytesRead];
            Buffer.BlockCopy(buffer, 0, payload, 0, bytesRead);
            var message = new NetworkMessage(NetworkMessageType.RawServerStream, payload);
            MessageReceived?.Invoke(this, new NetworkEventArgs(message));
        }
        Log("ReceiveLoop finalizado.");
    }

    /// <summary>
    /// Emite el mismo handshake/login que <c>Cliente.SendTextNow</c> en el cliente original.
    /// </summary>
    private async Task SendBootstrapAsync(uint serverSeed)
    {
        if (_stream is null)
        {
            return;
        }

        var handshake = ComputeHandshake(serverSeed);
        var builder = new MemoryStream();
        using var writer = new BinaryWriter(builder);

        writer.Write(handshake);
        writer.Write((byte)5); // VersionLA from original client

        if (string.IsNullOrWhiteSpace(_loginConfig.AccountName))
        {
            var sanitizedAvatar = _loginConfig.GetSanitizedAvatarIdentifier();
            writer.Write((byte)'*');
            writer.Write(_loginConfig.Skills);
            writer.Write((byte)((_loginConfig.RaceIndex & 0xF) | ((_loginConfig.ClassIndex & 0xF) << 4)));
            writer.Write(EncodeAttributes());
            writer.Write((byte)_loginConfig.AvatarName.Length);
            writer.Write(Encoding.ASCII.GetBytes(_loginConfig.AvatarName));
            var password = HashPassword(_loginConfig.Password, sanitizedAvatar);
            writer.Write(password);
        }
        else
        {
            var sanitizedLogin = _loginConfig.GetSanitizedAccountName();
            writer.Write((byte)'!');
            var password = HashPassword(_loginConfig.Password, sanitizedLogin);
            writer.Write(password);
            var login = Encoding.ASCII.GetBytes(sanitizedLogin);
            writer.Write((byte)login.Length);
            writer.Write(login);
        }

        writer.Flush();
        var payload = builder.ToArray();
        await SendAsync(payload).ConfigureAwait(false);
        Log($"Handshake serializado ({payload.Length} bytes).");
    }

    private static byte[] HashPassword(string password, string identifier)
    {
        var concat = $"Artes Arcanas:{identifier}{password}";
        return SHA256.HashData(Encoding.UTF8.GetBytes(concat));
    }

    private byte[] EncodeAttributes()
    {
        var value = 0u;
        if (_loginConfig.Gender != 0) value |= 0x8000_0000;
        value |= (uint)_loginConfig.Intelligence & 0x1F;
        value |= (uint)(_loginConfig.Strength & 0x1F) << 5;
        value |= (uint)(_loginConfig.Constitution & 0x1F) << 10;
        value |= (uint)(_loginConfig.Dexterity & 0x1F) << 15;
        value |= (uint)(_loginConfig.Wisdom & 0x1F) << 20;
        return BitConverter.GetBytes(value);
    }

    private static uint ComputeHandshake(uint seed)
    {
        const uint Token = 0x542C3A9E;
        uint XRandom(uint value)
        {
            value ^= value << 13;
            value ^= value >> 17;
            value ^= value << 5;
            return value;
        }

        return XRandom(seed + XRandom(Token ^ seed));
    }

    private async Task<(ushort connectionCode, uint serverSeed)> ReadBootstrapChallengeAsync()
    {
        var command = await ReadExactAsync(1).ConfigureAwait(false);
        if (command[0] != (byte)'|')
        {
            var opcode = (char)command[0];
            Log($"ERROR: opcode inesperado '{opcode}' al esperar '|'.");
            throw new IOException($"Unexpected bootstrap opcode {opcode}.");
        }

        var payload = await ReadExactAsync(6).ConfigureAwait(false);
        var connectionCode = BinaryPrimitives.ReadUInt16LittleEndian(payload.AsSpan(0, 2));
        var serverSeed = BinaryPrimitives.ReadUInt32LittleEndian(payload.AsSpan(2, 4));
        return (connectionCode, serverSeed);
    }

    private async Task<byte[]> ReadExactAsync(int length)
    {
        if (_stream is null)
        {
            throw new InvalidOperationException("Network stream not initialized.");
        }

        var buffer = new byte[length];
        var offset = 0;
        while (offset < length)
        {
            var bytesRead = await _stream.ReadAsync(buffer.AsMemory(offset, length - offset)).ConfigureAwait(false);
            if (bytesRead == 0)
            {
                var message = "Server closed the connection before finishing the bootstrap handshake.";
                Log($"ERROR: {message}");
                throw new IOException(message);
            }

            offset += bytesRead;
        }

        return buffer;
    }

    private static void Log(string message)
    {
        var line = $"[{DateTime.Now:O}] {message}{Environment.NewLine}";
        lock (LogSync)
        {
            try
            {
                File.AppendAllText(LogPath, line);
            }
            catch
            {
                // Ignore IO errors.
            }
        }
    }
}
