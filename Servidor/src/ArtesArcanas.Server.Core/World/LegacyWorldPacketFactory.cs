using System;
using System.Collections.Generic;
using ArtesArcanas.Server.Core.Legacy;

namespace ArtesArcanas.Server.Core.World;

internal static class LegacyWorldPacketFactory
{
    public static byte[] BuildSelfMovementPacket(LegacyPlayerSnapshot snapshot)
    {
        var payload = new byte[4];
        payload[0] = (byte)'p';
        payload[1] = snapshot.CoordenadaX;
        payload[2] = snapshot.CoordenadaY;
        payload[3] = ComposeDirection(snapshot);
        return payload;
    }

    public static byte[] BuildMovementBroadcastPacket(ushort code, LegacyPlayerSnapshot snapshot)
    {
        var payload = new byte[7];
        payload[0] = (byte)'*';
        payload[1] = (byte)'P';
        payload[2] = (byte)(code & 0xFF);
        payload[3] = (byte)((code >> 8) & 0xFF);
        payload[4] = snapshot.CoordenadaX;
        payload[5] = snapshot.CoordenadaY;
        payload[6] = ComposeDirection(snapshot);
        return payload;
    }

    public static byte[] BuildPlayerSpawnPacket(LegacyPlayerContext player)
    {
        var snapshot = player.Snapshot;
        var buffer = new List<byte>(32)
        {
            (byte)'N'
        };

        AppendUInt16(buffer, player.Code);
        buffer.Add(snapshot.CoordenadaX);
        buffer.Add(snapshot.CoordenadaY);
        buffer.Add(ComposeDirection(snapshot));
        buffer.Add(snapshot.Animacion);
        AppendUInt16(buffer, unchecked((ushort)snapshot.Banderas));
        buffer.Add(snapshot.Nivel);

        var classInfo = (byte)(snapshot.Categoria | (snapshot.TipoMonstruo << 4));
        if (snapshot.Hp > 0)
        {
            classInfo |= 0x80;
        }
        buffer.Add(classInfo);

        buffer.Add(unchecked((byte)snapshot.Comportamiento));
        buffer.Add(snapshot.Clan);
        buffer.Add(snapshot.Rostro);

        var name = snapshot.GetAvatarName();
        var encoded = LegacyConstants.LegacyEncoding.GetBytes(name);
        var length = (byte)Math.Min(16, encoded.Length);
        buffer.Add(length);
        buffer.AddRange(encoded.AsSpan(0, length).ToArray());

        return buffer.ToArray();
    }

    public static byte[] BuildPlayerRemovalPacket(ushort code)
    {
        return new[]
        {
            (byte)'~',
            (byte)(code & 0xFF),
            (byte)((code >> 8) & 0xFF)
        };
    }

    public static byte[] BuildMapBootstrapPacket(
        byte mapId,
        byte x,
        byte y,
        byte weatherType,
        byte weatherIntensity,
        sbyte weatherDrift,
        byte castleClan,
        string? offlineClanName,
        LegacyClanBanner? clanBanner)
    {
        var buffer = new List<byte>(24)
        {
            (byte)'^',
            mapId,
            x,
            y,
            weatherType,
            weatherIntensity,
            unchecked((byte)weatherDrift),
            castleClan
        };

        if (offlineClanName is { Length: > 0 } clanName && clanBanner is not null)
        {
            var encoded = LegacyConstants.LegacyEncoding.GetBytes(clanName);
            var length = (byte)Math.Min(127, encoded.Length);
            buffer.Add(length);
            buffer.AddRange(encoded.AsSpan(0, length).ToArray());
            AppendUInt32(buffer, clanBanner.ColorPrimary);
            AppendUInt32(buffer, clanBanner.ColorSecondary);
        }
        else
        {
            buffer.Add(0);
        }

        return buffer.ToArray();
    }

    public static byte[] BuildClanActivationPacket(LegacyClanInfo clan)
    {
        var buffer = new List<byte>(24)
        {
            (byte)'I',
            (byte)'k',
            1,
            clan.Id
        };

        AppendUInt32(buffer, clan.Banner.ColorPrimary);
        AppendUInt32(buffer, clan.Banner.ColorSecondary);
        buffer.Add(clan.Color);

        var encodedName = LegacyConstants.LegacyEncoding.GetBytes(clan.Name ?? string.Empty);
        var length = (byte)Math.Min(255, encodedName.Length);
        buffer.Add(length);
        if (length > 0)
        {
            buffer.AddRange(encodedName.AsSpan(0, length).ToArray());
        }

        return buffer.ToArray();
    }

    public static byte[] BuildClanAssignmentPacket(byte clanId, ushort playerCode)
    {
        return new[]
        {
            (byte)'I',
            (byte)200,
            clanId,
            (byte)(playerCode & 0xFF),
            (byte)((playerCode >> 8) & 0xFF)
        };
    }

    private static byte ComposeDirection(LegacyPlayerSnapshot snapshot) =>
        ComposeMonsterDirection(snapshot.Direccion, snapshot.Accion);

    public static byte ComposeMonsterDirection(byte direction, byte action) =>
        (byte)(direction | ((action & 0x0F) << 4));

    private static void AppendUInt16(List<byte> buffer, ushort value)
    {
        buffer.Add((byte)(value & 0xFF));
        buffer.Add((byte)((value >> 8) & 0xFF));
    }

    private static void AppendUInt32(List<byte> buffer, uint value)
    {
        buffer.Add((byte)(value & 0xFF));
        buffer.Add((byte)((value >> 8) & 0xFF));
        buffer.Add((byte)((value >> 16) & 0xFF));
        buffer.Add((byte)((value >> 24) & 0xFF));
    }

    public static byte[] BuildMonsterMovementPacket(LegacyMonsterInstance monster)
    {
        return new[]
        {
            (byte)'P',
            (byte)(monster.Code & 0xFF),
            (byte)((monster.Code >> 8) & 0xFF),
            monster.X,
            monster.Y,
            ComposeMonsterDirection(monster.Direction, monster.Action)
        };
    }

    public static byte[] BuildMonsterSpawnPacket(LegacyMonsterInstance monster)
    {
        var buffer = new List<byte>(8)
        {
            (byte)'n'
        };

        AppendUInt16(buffer, monster.Code);
        buffer.Add(monster.X);
        buffer.Add(monster.Y);
        buffer.Add(ComposeMonsterDirection(monster.Direction, monster.Action));
        buffer.Add(monster.Animation);
        return buffer.ToArray();
    }
}
