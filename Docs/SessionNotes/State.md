# Estado actual – Cliente y Servidor (MonoGame + Protocolo binario)

## Cliente (MonoGame)

- **Pantallas**: flujo de arranque con Login → Selección de personajes → Creación de personaje → InGame.
  - Login: usuario/clave, envía `LoginRequest` al servidor (127.0.0.1:7667).
  - Selección: lista de hasta 5 personajes, botón Ingresar, Crear, Volver, Salir.
  - Creación: nombre, raza, clase, pericias (máx 3), stats tirados (5..50, suma < 100), Crear/Cancelar.
  - InGame: carga mapa y posición desde el personaje; HUD muestra HP/MP/Comida, oro/plata, armadura, res. mágica, evasión, honor numérico, experiencia.
- **Networking**: `TcpGameClient` usa el framing AA55 de `Laa.Protocol`. Mensajes implementados: Login, CharacterList, CharacterCreate, EnterWorld, Ping/Pong, Error.
- **UI/HUD**:
  - HUD inferior intacta (minimapa, portrait, paperdoll). Removidos paneles flotantes de debug (inventario, hechizos, comercio, roadmap) y quick-actions (ataque/ hechizo rápido).
  - Pericias se muestran según el bitmask real del personaje. Armadura/res. mágica/evasión se muestran en la columna de combate/defensa.
- **Recursos**: solo se cargan atlases desde `content/graphics`; no se usa `grf` en runtime.
- **Mapas**: se cargan por id (`map_<id>.json`), cámara centrada en el spawn del personaje recibido.
- **Animaciones de jugador**:
  - El set de avatares tiene 5 direcciones (N, S, W, diag-izq, diag-der); no hay columnas específicas para derecha.
  - Se usa el mapeo original `MC_DirAnimacion` (Pascal) y se espeja (`SpriteEffects.FlipHorizontally`) para las direcciones hacia la derecha (E, NE, SE). El cálculo de posición ya contempla el espejo.
  - Nota para monstruos: varias animaciones de monstruos usan el mismo patrón (falta columna derecha); reutilizar la dirección izquierda con espejo al implementarlos.
- **Monstruos en cliente**: ya no se generan spawns locales ni simulación; el cliente espera que el servidor envíe las instancias/posiciones y solo renderizará lo que reciba.
- **Protocolo de entidades**: se usa `EntitySnapshot` (MessageId 30) para que el servidor envíe los monstruos del mapa actual (id, tipo, posición, dirección, espejo, acción, HP). El cliente los crea y los dibuja; no hay IA ni updates incrementales aún.

## Servidor (net8, TCP 127.0.0.1:7667)

- **Protocolo**: binario con header fijo (Magic 0xAA55, Version, MessageId, Flags, PayloadLength).
- **Mensajes activos**:
  - `LoginRequest/Response`
  - `CharacterListRequest/Response`
  - `CharacterCreateRequest/Response`
  - `EnterWorldRequest/Response`
  - `Ping/Pong`
  - `ErrorResponse`
- **Persistencia**: JSON por cuenta en `accounts/` (`AccountRecord` + `CharacterRecord`).
  - `CharacterRecord` incluye: raza, clase, pericias (mask), stats base, evasión, nivel, experiencia, mapa/pos, HP/MP max/actual, oro, plata, comida, honor (1–5), armadura, resistencia mágica, inventario (itemId, amount, equippedSlot), hechizos (spellId).
  - Spawn inicial según raza:
    - Humano→mapa1 (220,62), Elfo→4 (138,232), Enano→6 (46,53), Gnomo→1 (206,51), Semielfo→4 (138,232), Orco→8 (33,107), Drow→12 (138,104).
- **Datos de monstruos en servidor**: se copió `monsters.json` a `src/Laa.Server/data/` para que el servidor resuelva tipos/animaciones/atributos al instanciarlos. El servidor aún no envía spawns al cliente; se añadirá al definir el protocolo de mundo.
- **Handlers**:
  - Login valida usuario/clave (creación de cuentas externa).
  - CharacterList entrega hasta 5 personajes con todos los campos.
  - CharacterCreate valida slots/nombre, aplica defaults/spawn, guarda JSON.
  - EnterWorld responde OK (sin lógica de mundo aún).
  - Ping → Pong, ErrorResponse para fallos de payload/validación.

## Qué queda pendiente

- Mostrar inventario/hechizos del personaje en la HUD (el protocolo ya los envía).
- Integrar lógica de mundo/combat real en EnterWorld/updates.
- Tabla de experiencia real y sincronizar honor/estados con servidor.
- Crear cuentas vía web (no soportado por protocolo).
