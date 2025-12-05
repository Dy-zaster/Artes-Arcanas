# LAa Networking Protocol

This document captures how the original Delphi 6 client communicates with the server so the MonoGame rewrite can interoperate without requiring server changes.

## Transport + framing

- The client uses a blocking `TClientSocket` (WinSock) and processes responses inside `TJForm.ClientSocketRead` (`Original Pascal/Laa/Juego.pas:4120`).
- Incoming bytes are accumulated in `Socket.BufferRecepcion` and interpreted as a stream of *meta commands*. Every packet starts with one byte opcode followed by a variable-length payload that is consumed with helper readers (`GET1B`, `GET2B`, `GET4B`, `GET_Cadena127`, etc.) at `Original Pascal/Laa/Juego.pas:4326`.
- There is no length prefix: the parser loops until it runs out of buffered data. When the opcode handler detects that not enough bytes have arrived it simply returns and leaves the partial data in the buffer for the next read (`FaltaInformacion`).
- All numeric payloads use little-endian packing implemented through `GET*` helpers (`Original Pascal/Laa/Juego.pas:4345`). Strings are stored as length-prefixed byte arrays (1 byte length + UTF-8-compatible bytes) inside `TCadena127/255` records.

## Session bootstrap

1. When the login/character creation form submits, `TratarIniciarSesion` builds the first payload sent through the socket (`Original Pascal/Laa/Juego.pas:4160`).
2. The payload layout is:
   - `B4aStr(ObtenerHandshake(NroAleatorioServidor))`: 4-byte challenge token echoed back to defeat replay attacks.
   - `chr(VersionLA)`: single byte with the supported protocol/game version.
   - `Command` section:
     - **Login** (`'!'`): `! + PasswordAStr(hash(password, login)) + chr(len(login)) + login` (`Original Pascal/Laa/Juego.pas:4204`).
     - **Character creation** (`'*'`): `* + B2aStr(skills) + class/race byte + B4aStr(attribute bundle) + chr(len(name)) + name + PasswordAStr(hash(password,name))` (`Original Pascal/Laa/Juego.pas:4177`). The attribute bundle stores sex bit + INT/FRZ/CON/DES/SAB scores packed into a 32-bit integer.
3. Additional administrative commands (password changes, logout, account flags) reuse the same socket connection and each send ASCII prefixes such as `XC`, `XS`, `KT`, `K?`, etc. See [Client → Server commands](#client--server-commands).

## Server → Client commands

The interpreter (`ClientSocketRead`) switches on the opcode byte and updates client state. The most relevant groups are:

### Movement & actor state

| Opcode | Payload | Description | References |
| --- | --- | --- | --- |
| `p` | `X (1B), Y (1B), Dir (1B)` | Updates the local player's coordinates when the server resyncs movement. | `Original Pascal/Laa/Juego.pas:4346` |
| `P` | `SpriteId (2B), X (1B), Y (1B), Dir (1B)` | Teleports/positions non-player sprites. | `Original Pascal/Laa/Juego.pas:4352`
| `#128..#135` | `SpriteId (2B)` | Changes direction for a sprite (8 possible values). The opcode minus `128` becomes the direction enum. | `Original Pascal/Laa/Juego.pas:4359`
| `#160..#175` | `SpriteId (2B)` | Changes animation/action for any sprite (16 values). | `Original Pascal/Laa/Juego.pas:4370`
| `r` | `Count (1B) + (SpriteId (2B) + X + Y + Dir) * count` | Batch refresh for many sprites. | `Original Pascal/Laa/Juego.pas:4487`
| `s` | `SubOp (1B)` | Applies status effects to the local avatar (resurrection, protections, berserk, etc.). | `Original Pascal/Laa/Juego.pas:4515`

### Vital stats and resources

| Opcode | Payload | Description | Reference |
| --- | --- | --- | --- |
| `#255` | `HP (2B)` | Replaces HP bar silently. | `Original Pascal/Laa/Juego.pas:4386`
| `#252/#251/#249` | `HP (2B) + Source (1B) + extra` | Reports HP loss and the attacker (monster index, object id, or spell id). | `Original Pascal/Laa/Juego.pas:4389`
| `#254` | `Mana (1B)` | Refreshes mana bar. | `Original Pascal/Laa/Juego.pas:4411`
| `#253` | `Food (1B)` | Updates hunger value. | `Original Pascal/Laa/Juego.pas:4416`
| `#250` | `Money (4B)` | Refreshes gold counter. | `Original Pascal/Laa/Juego.pas:4421`
| `e` | `XP (2B)` | Player experience update. | `Original Pascal/Laa/Juego.pas:4496`

### Map objects, drops, and inventory sync

| Opcode | Payload | Description | Reference |
| --- | --- | --- | --- |
| `#192..#207` | `TileX (1B), TileY (1B)` | Adds/removes map bags, logs, corpses, traps, fogatas. Each opcode maps to a `TTipoBolsa`. | `Original Pascal/Laa/Juego.pas:4426`
| `#208..#245` | `SlotIndex (1B), ItemId (1B)` | Refreshes equipped objects or inventory modifiers. | `Original Pascal/Laa/Juego.pas:4459`
| `#0..#7` | `Modifier (1B)` | Updates durability/modifier for the 8 wearable slots. Handles breakage notifications. | `Original Pascal/Laa/Juego.pas:4465`
| `b` | `SubOp + payload` | Syncs bag/chest contents: SubOps `0..29` update vault slots, `128..157` update map bag slots. | `Original Pascal/Laa/Juego.pas:4582`

### Visual/audio and combat feedback

| Opcode | Payload | Description | Reference |
| --- | --- | --- | --- |
| `S` | `X, Y, EffectCode` | Spawns positional FX + sound (combat hits, crafting, spells). Many mappings exist for codes `0..255`. | `Original Pascal/Laa/Juego.pas:4529`
| `=` | `SpriteId (2B), SubOp (1B)` | Applies special effect to a sprite (currently resurrection). | `Original Pascal/Laa/Juego.pas:4510`
| `d`/`D` | `Damage (2B) + Descriptor` | Chat log entry describing inflicted damage to monsters (`d`) or avatars (`D`). | `Original Pascal/Laa/Juego.pas:4559`
| `C` | `SpellId (1B) + Target (2B)` | Reports spells cast on avatars. | `Original Pascal/Laa/Juego.pas:4567`

### Messaging, UI, and admin notifications

- `h`/`H`: free-form chat balloon strings or canned combat results tied to a sprite (`Original Pascal/Laa/Juego.pas:4500`).
- `i`: numeric server info codes mapped to localized strings/sounds (`Original Pascal/Laa/Juego.pas:4572`).
- `I` with subcommands `M/K/Q/N/( /P/k/…): clan changes, timers, shop offers, world saves, etc. Each branch references clan arrays in `Original Pascal/Laa/Juego.pas:4595` onwards.
- `*` + `SubOp`: reserved namespace currently used for direct sprite teleport with `tmDirectoConEfecto` blending (`Original Pascal/Laa/Juego.pas:4521`).

The MonoGame client will mirror this table using strongly typed enums to avoid hard-coded ASCII magic numbers.

## Client → Server commands

`Cliente.SendTextNow` reveals the complete outbound vocabulary. Key actions include:

| Command | Payload | Purpose | Reference |
| --- | --- | --- | --- |
| `XX` | — | Stop any queued action (`JDetenerAcciones`). | `Original Pascal/Laa/MundoEspejo.pas:506`
| `m` | `Dir (1B)` | Step movement in one of eight directions. | `Original Pascal/Laa/MundoEspejo.pas:523`
| `M` | `Packed target (2B)` or `X (1B) + Y (1B)` | Continuous run/teleport or map click movement. (`JMover`, `JMoverXY`). | `Original Pascal/Laa/MundoEspejo.pas:520`
| `W` | `Target (2B)` | Follow highlighted entity. | `Original Pascal/Laa/MundoEspejo.pas:551`
| `A`/`B` | `Target (2B)` | Attack (aggressive vs defensive). | `Original Pascal/Laa/MundoEspejo.pas:611`
| `Y`/`y` | `Target (2B)` | Cast spell (channeled vs single cast). Requires optional `j` (set spell id) and `J` (select artifact). | `Original Pascal/Laa/MundoEspejo.pas:643`
| `KG`/`KS`/`Ki` | `Count (1B) + Slot (1B)` | Vault deposit/withdraw/swap. | `Original Pascal/Laa/MundoEspejo.pas:700`
| `V`/`F`/`C`/`r`/`S` | `Target + counts` | Merchant interactions (sell, buy, fabricate) as seen in `UCliente.pas:701` and `MundoEspejo.pas:913`.
| `XE`, `Xs`, `XR`, `X!`, etc. | — | Misc. account/character actions (toggle weapon specialty, highlight skills, request resurrection, logout). | `Original Pascal/Laa/Juego.pas:1105`, `Original Pascal/Laa/MundoEspejo.pas:670`.

The new client should model these as request DTOs (e.g., `MoveCommand`, `CastSpellCommand`) and serialize them using the same byte layout to guarantee compatibility with the existing server.

## Porting considerations

1. **Byte order** – The Delphi client assumes little-endian packing when it builds integers. In C#/.NET make sure to explicitly encode/decode with `BinaryPrimitives` to avoid runtime endianness surprises.
2. **Buffering** – The original implementation relies on persistent buffers and manual position tracking. We should mirror that approach but wrap it inside a reusable `PacketReader`/`PacketWriter` abstraction with bounds checking.
3. **String encoding** – Length-prefixed strings are limited to 127/255 bytes. The new client should UTF-8 encode and truncate accordingly so it never overruns server expectations.
4. **Timing** – Server throttles excessive commands (see opcode `I#8`). Implement client-side spam guards and cooldowns before firing movement/attack packets.
5. **Documentation** – Keep this document updated as more opcodes are decoded. Each time we port a subsystem, capture its packet(s) and cross-link the C# implementation.
