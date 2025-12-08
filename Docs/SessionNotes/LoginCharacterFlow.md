# Login y selección/creación de personajes (sesión de trabajo)

## Resumen

- Se eliminó el cliente legacy y dependencias de `grf`; el runtime usa solo atlases en `content/graphics`.
- Se definió un protocolo binario compartido (`Laa.Protocol`) con framing AA55, `MessageId` y helpers `PacketReader/Writer`.
- Se creó un servidor TCP (`Laa.Server`) en 127.0.0.1:7667 con persistencia JSON por cuenta (máx. 5 personajes).
- El cliente MonoGame ahora arranca en una pantalla de login; luego muestra selección de personajes y creación de personajes antes de entrar al juego.

## Protocolo

### Framing
- Magic `0xAA55`, `Version` (byte), `MessageId` (byte), `Flags` (byte), `PayloadLength` (int32 LE), payload binario.

### Mensajes añadidos
- `LoginRequest/LoginResponse` (1/2)
- `CharacterListRequest/Response` (10/11)
- `CharacterCreateRequest/Response` (12/13)
- `EnterWorldRequest/Response` (14/15)
- `Ping/Pong` (254/255), `ErrorResponse` (200)

### Payloads clave
- `LoginRequest`: byte len + usuario, byte len + password/hash.
- `CharacterListRequest`: usuario.
- `CharacterListResponse`: `count (byte)` + por personaje: nombre, raza, clase, perkMask (uint), stats (fuerza/const/inte/sab/desk como bytes).
- `CharacterCreateRequest`: usuario + nombre + raza + clase + perkMask + stats (bytes).
- `EnterWorldRequest`: nombre del personaje.

## Servidor (`src/Laa.Server`)
- `AccountStore` guarda cada cuenta en JSON (`accounts/<usuario>.json`) con `Username`, `PasswordHash`, `Characters[]`.
- Handlers:
  - `LoginRequest`: valida cuenta/clave.
  - `CharacterListRequest`: devuelve la lista de personajes.
  - `CharacterCreateRequest`: valida slots (máx 5), nombre único en la cuenta, agrega personaje y persiste.
  - `EnterWorldRequest`: responde ok.
  - `Ping` → `Pong`, `ErrorResponse` para errores.
- Utiliza `SpanReader` y helpers de `Laa.Protocol`.

Ejemplo de cuenta JSON:
```json
{
  "Username": "demo",
  "PasswordHash": "clave123",
  "Characters": [
    {
      "Name": "Aria",
      "Race": 0,
      "Class": 1,
      "PerkMask": 5,
      "StatStrength": 25,
      "StatConstitution": 20,
      "StatIntelligence": 15,
      "StatWisdom": 10,
      "StatDexterity": 20
    }
  ]
}
```

## Cliente (`src/Laa.Monogame.Client`)
- Nuevo cliente TCP `TcpGameClient` usando `Laa.Protocol` (127.0.0.1:7667).
- Estados (`ClientStage`): `Login` → `CharacterSelect` → `CharacterCreate` → `InGame`.
- Pantalla de login: usuario/clave (inputs), envía `LoginRequest` y solicita lista al recibir `LoginResponse`.
- Pantalla de personajes:
  - Lista (máx 5), botón “Ingresar” habilitado al seleccionar, “Crear personaje”, “Volver”, “Salir”.
  - Al ingresar envía `EnterWorldRequest`.
- Pantalla de creación:
  - Nombre, raza/clase cíclicos, pericias multi-select (máx 3), stats tirados (5..50 paso 5, suma < 100), botones Crear/Cancelar.
  - Envía `CharacterCreateRequest`; al confirmar, refresca lista y vuelve a selección.
- Widgets nuevos: `UiTextInputWidget`, `UiButtonWidget` (con `IsEnabled`), `UiSelectableListWidget`.
- El render/UI del mundo se oculta hasta `InGame`.

## Comandos útiles
- Build: `cd MonoGameClient && dotnet build`
- Servidor: `cd MonoGameClient/src/Laa.Server && dotnet run`
- Cliente: `cd MonoGameClient/src/Laa.Monogame.Client && dotnet run`
