# Protocolo binario v1 (MMORPG)

Este protocolo es nuevo y pensado para reemplazar por completo el wire del servidor Pascal. Objetivos: paquetes chicos, parsing barato, versionado explícito y nombres de mensajes legibles.

## Framing

- Endianness: little-endian en todos los campos numéricos.
- Header fijo (9 bytes):
  - `Magic` (2B): `0xAA55` para validar desalineos.
  - `Version` (1B): versión del protocolo (`1` actual).
  - `MessageId` (1B): tipo de mensaje (tabla abajo).
  - `Flags` (1B): bit 0 = comprimido (LZ4), bit 1 = requiere ACK, resto reservado.
  - `PayloadLength` (4B): bytes del payload sin header.
- Payload: binario según el `MessageId`.
- Transporte recomendado: TCP para simplicidad inicial. Si luego se agrega UDP para movimiento, se reutiliza el framing con un campo adicional de `Seq/Ack`.

### Límites

- `PayloadLength` máximo recomendado: 1 MiB. Rechazar o dividir mensajes más grandes (ej. streaming de mapa).
- Strings: `byte length` + UTF-8 bytes; truncar a 255 y validar.
- Arrays: `ushort count` + elementos.

## MessageId (básicos)

```
1  LoginRequest           username, passwordHash
2  LoginResponse          resultCode, sessionToken
3  CreateAccountRequest   username, passwordHash, email (opcional)
4  CreateAccountResponse  resultCode
5  LogoutRequest          sessionToken
6  KeepAlive              timestamp

20 MapRequest             mapId, chunkStart (tile), viewport size
21 MapChunk               mapId, chunk coords, terrain+props payload
22 MapReady               mapId, version hash

30 EntitySnapshot         full list of entities on map (id, type, pos, stats)
31 EntityUpdate           partial updates (id, pos/dir/action/states)
32 EntityRemove           ids removed

40 ChatMessage            channel, fromId, text
41 SystemMessage          code, params

50 InventorySnapshot      slots[]
51 InventoryUpdate        slot changes
52 MerchantOffer          merchantId, items[]

60 CombatEvent            attackerId, targetId, skillId, damage, state

200 ErrorResponse         code, message
254 Ping                  client timestamp
255 Pong                  server timestamp
```

(`resultCode` y otros enums se documentarán por separado.)

## Formatos de payload (ejemplos)

- `LoginRequest`
  - `byte userLen` + `userLen` bytes UTF-8
  - `byte passLen` + `passLen` bytes UTF-8 (hash/salted en la app, no en wire)
- `LoginResponse`
  - `byte result` (0=OK, 1=InvalidCredentials, 2=Locked, 3=VersionMismatch)
  - `byte tokenLen` + `tokenLen` bytes (si `result==0`)
- `EntityUpdate`
  - `ushort count`
  - Repetir `count` veces:
    - `int entityId`
    - `byte fieldsMask` (bit 0 pos, 1 dir, 2 action, 3 hp/mp, 4 state flags)
    - Pos (si bit 0): `float x`, `float y`
    - Dir (bit 1): `byte dir`
    - Action (bit 2): `byte action`
    - Stats (bit 3): `ushort hp`, `ushort mp`
    - State flags (bit 4): `ushort flags`

## Compresión

Si `Flags` tiene bit 0 = 1, el payload está comprimido con LZ4 (frame block). Descomprimir antes de parsear campos.

## Seguridad

- Siempre enviar un `Version` coherente; rechazar conexiones fuera de rango.
- No enviar contraseñas en claro: aplicar hash+salt lado cliente antes de `LoginRequest`.
- Opcional: HMAC del payload usando `sessionToken` y un nonce simple para evitar replay (agregar flag dedicado más adelante).

## Cliente

Clases de soporte a implementar (C#):

- `PacketHeader` (struct) y `Packet` (MessageId + ReadOnlyMemory<byte>).
- `PacketWriter`/`PacketReader` con buffer reutilizable y validaciones de `Magic`/longitud.
- `IMessageSerializer<T>` para cada DTO.
- Opcional: compresión LZ4 cuando `Flags` lo pidan.

El cliente no debe depender de assets legacy; todo el runtime lee desde `content/graphics` y JSON de `content/data`. Este protocolo es el contrato futuro para el nuevo servidor. 
