using System.Runtime.InteropServices;
using System.Text;

namespace ArtesArcanas.Server.Core.Legacy;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct LegacyPlayerSnapshot
{
    public const int RawSize = LegacyConstants.PlayerSnapshotSize;

    public fixed byte Timers[LegacyConstants.MaxTimers];
    public byte TipoMonstruo;
    public byte Direccion;
    public byte CoordenadaX;
    public byte CoordenadaY;
    public ushort Hp;
    public byte Mana;
    public byte ReservadoZ1;
    public byte Activo;
    public byte CodigoMapa;
    public byte Accion;
    public byte Animacion;
    public ushort Codigo;
    public byte CodigoNido;
    public sbyte Comportamiento;
    public int Banderas;
    public ushort Duenno;
    public byte AtaqueUtilizado;
    public byte PericiasDinamicas;
    public ushort ObjetivoAtacado;
    public ushort ObjetivoASeguir;
    public byte RitmoDeVida;
    public byte ControlMovimiento;
    public byte CoordXAnterior;
    public byte CoordYAnterior;

    public sbyte TurnosGastados;
    public byte Rostro;
    public byte DestinoX;
    public byte DestinoY;
    public byte Categoria;
    public byte Nivel;
    public ushort Experiencia;
    public fixed byte NombreAvatar[17];
    public byte CapacidadIdentificacion;
    public byte Comida;
    public byte AurasExternas;
    public uint Pericias;
    public int Apuntado;
    public int Dinero;
    public uint Conjuros;
    public int Reservado4Bytes;

    public fixed byte Usando[LegacyConstants.EquipmentSlots * 2];
    public fixed byte Inventario[LegacyConstants.InventoryArtifactSlots * 2];

    public byte NivelAtaque;
    public byte Defensa;
    public sbyte ModificadorDefensa;
    public byte DannoBase;
    public byte EspecialidadArma;
    public byte NivelEspecializacion;
    public byte HabilidadResaltada;
    public byte Fuerza;
    public byte Constitucion;
    public byte Inteligencia;
    public byte Sabiduria;
    public byte Destreza;

    public fixed sbyte Armadura[8];

    public byte ConjuroElegido;
    public byte NivelDeCategoria;
    public byte FlagsComunicacion;
    public byte Clan;

    public fixed byte Baul[LegacyConstants.ChestSlots * 2];

    public ushort MaxHp;
    public byte MaxMana;
    public byte Meditacion255;

    public ushort ObjetivoAtaqueAutomatico;
    public byte AccionAutomatica;
    public byte MensajesEnviados;
    public int QuestEnCurso;
    public int QuestLogrado;

    public fixed ushort Camaradas[LegacyConstants.PartySlots];

    public int DineroOferta;
    public ushort CodigoMonstruoOferta;
    public byte ObjetoOfertaId;
    public byte ObjetoOfertaMod;
    public byte IndiceObjetoOferta;
    public byte IndiceInflacionModificada;
    public byte TipoTransaccion;
    public byte CantidadObjetosOferta;

    public static LegacyPlayerSnapshot FromBytes(ReadOnlySpan<byte> data)
    {
        if (data.Length < RawSize)
        {
            throw new ArgumentException($"Se esperaban {RawSize} bytes para el snapshot del jugador.", nameof(data));
        }

        return MemoryMarshal.Read<LegacyPlayerSnapshot>(data);
    }

    public byte[] ToByteArray()
    {
        var buffer = new byte[RawSize];
        MemoryMarshal.Write(buffer.AsSpan(), in this);
        return buffer;
    }

    public unsafe void SetArmorValue(int index, sbyte value)
    {
        if ((uint)index >= 8)
        {
            return;
        }

        fixed (sbyte* armor = Armadura)
        {
            armor[index] = value;
        }
    }

    public unsafe void SetPartySlot(int index, ushort value)
    {
        if ((uint)index >= LegacyConstants.PartySlots)
        {
            return;
        }

        fixed (ushort* party = Camaradas)
        {
            party[index] = value;
        }
    }

    public string BuildLoginPayload()
    {
        var builder = new StringBuilder(256);
        LegacyBinaryEncoding.AppendB2(builder, Hp);
        builder.Append((char)Mana);
        builder.Append((char)Comida);
        builder.Append((char)Direccion);
        builder.Append((char)Animacion);
        LegacyBinaryEncoding.AppendB3(builder, Banderas);
        builder.Append((char)(Categoria | (TipoMonstruo << 4)));
        LegacyBinaryEncoding.AppendB2(builder, (ushort)(Pericias & 0xFFFF));
        builder.Append((char)Nivel);
        LegacyBinaryEncoding.AppendB2(builder, Experiencia);
        LegacyBinaryEncoding.AppendB4(builder, Conjuros);
        builder.Append((char)EspecialidadArma);
        builder.Append((char)NivelEspecializacion);
        builder.Append((char)Rostro);
        builder.Append((char)(byte)Comportamiento);
        builder.Append((char)Clan);
        LegacyBinaryEncoding.AppendB4(builder, unchecked((uint)Dinero));
        LegacyBinaryEncoding.AppendB4(builder, unchecked((uint)GetPackedSkills()));
        AppendEquipment(builder);
        builder.Append((char)ConjuroElegido);
        AppendAvatarName(builder);
        return builder.ToString();
    }

    public string GetAvatarName()
    {
        fixed (byte* name = NombreAvatar)
        {
            var length = Math.Min(name[0], (byte)16);
            return LegacyConstants.LegacyEncoding.GetString(new ReadOnlySpan<byte>(name + 1, length));
        }
    }

    public unsafe void GetEquipmentArtifact(int slot, out byte id, out byte modifier)
    {
        id = 0;
        modifier = 0;
        if ((uint)slot >= LegacyConstants.EquipmentSlots)
        {
            return;
        }

        fixed (byte* equipment = Usando)
        {
            var index = slot * 2;
            id = equipment[index];
            modifier = equipment[index + 1];
        }
    }

    public unsafe void SetEquipmentArtifact(int slot, byte id, byte modifier)
    {
        if ((uint)slot >= LegacyConstants.EquipmentSlots)
        {
            return;
        }

        fixed (byte* equipment = Usando)
        {
            var index = slot * 2;
            equipment[index] = id;
            equipment[index + 1] = modifier;
        }
    }

    public unsafe void SetInventoryArtifact(int slot, byte id, byte modifier)
    {
        if ((uint)slot >= LegacyConstants.InventoryArtifactSlots)
        {
            return;
        }

        fixed (byte* inventory = Inventario)
        {
            var index = slot * 2;
            inventory[index] = id;
            inventory[index + 1] = modifier;
        }
    }

    public unsafe void SetAvatarName(string name)
    {
        var bytes = string.IsNullOrEmpty(name)
            ? Array.Empty<byte>()
            : LegacyConstants.LegacyEncoding.GetBytes(name);
        var length = (byte)Math.Min(16, bytes.Length);
        fixed (byte* destination = NombreAvatar)
        {
            destination[0] = length;
            for (var i = 0; i < length; i++)
            {
                destination[i + 1] = bytes[i];
            }

            for (var i = length; i < 16; i++)
            {
                destination[i + 1] = 0;
            }
        }
    }

    private void AppendEquipment(StringBuilder builder)
    {
        for (var slot = 0; slot < LegacyConstants.EquipmentSlots; slot++)
        {
            var idx = slot * 2;
            builder.Append((char)Usando[idx]);
            builder.Append((char)Usando[idx + 1]);
        }

        for (var slot = 0; slot < LegacyConstants.InventoryArtifactSlots; slot++)
        {
            var idx = slot * 2;
            builder.Append((char)Inventario[idx]);
            builder.Append((char)Inventario[idx + 1]);
        }
    }

    private void AppendAvatarName(StringBuilder builder)
    {
        var length = 0;
        fixed (byte* name = NombreAvatar)
        {
            length = Math.Min(name[0], (byte)16);
            builder.Append((char)length);
            for (var i = 0; i < length; i++)
            {
                builder.Append((char)name[i + 1]);
            }
        }
    }

    private int GetPackedSkills()
    {
        const int Mask = 0x1F;
        var highlighted = HabilidadResaltada & 0x3F;
        return
            (Fuerza & Mask) |
            ((Inteligencia & Mask) << 5) |
            ((Destreza & Mask) << 10) |
            ((Sabiduria & Mask) << 15) |
            ((Constitucion & Mask) << 20) |
            (highlighted << 25);
    }
}
