# Plan de migración del servidor de Artes Arcanas

## 1. Visión general

- **Objetivo**: portar el servidor legado en Pascal (Delphi/VCL) a una arquitectura moderna basada en .NET 8, manteniendo compatibilidad binaria con el cliente original/MonoGame y preservando toda la jugabilidad.
- **Estado**: existe un host .NET funcional para autenticación básica y chat, pero el “mundo” todavía depende al 100 % del código Pascal (`Mundo.pas`, `TableroControlado.pas`, `smain.pas`).

## 2. Funcionalidades ya migradas

1. **Infraestructura básica**  
   - Detección de rutas (`ServerPaths.Discover`) y carga del archivo `opciones.txt` con los parámetros fundamentales (IP, puerto, mapas máximos, mensaje de bienvenida, flags de chat/múltiples sesiones).  
   - Implementación de un `IServerLogger` con soporte para timestamps y registro a archivo replicando `server.log`.
2. **Compatibilidad binaria de cuentas**  
   - Lectura y escritura de `avatares/*.avt` mediante `LegacyAccountSerializer`, incluyendo snapshot del personaje de 292 bytes y metadatos (`LegacyUserData`).  
   - Registro de administradores (`admin.dat`) con estados y permisos enumerados.
3. **Networking y sesiones**  
   - Servidor TCP (`LegacyNetworkServer`) que acepta múltiples clientes, reserva códigos 0..255 igual que el Pascal, y rechaza si no hay slots.  
   - Handshake `| + B2a + B4a` y validación de versión/seed (`LegacySecurity.ComputeHandshake`).  
   - Proceso completo del comando `!` de login: saneado del usuario, bloqueo de logins duplicados, validación de hash SHA-256 y construcción del payload `@`+flags+snapshot+`!`.  
   - Envío del mensaje de bienvenida original y flag de múltiples sesiones (`I` + 0x18).  
   - El servidor ya genera los paquetes legacy `N` y `~` para anunciar apariciones y retiros y, al igual que los comandos `p/*P/r`, sólo se envían a los jugadores conectados al mismo mapa.  
   - Se añadió una grilla de ocupación por mapa: durante el login se buscan casillas libres cercanas si la original está ocupada y los movimientos `m/M` sólo prosperan cuando la casilla de destino está libre, con lo que se elimina la superposición de avatares incluso antes de portar el motor completo.  
   - Los comandos de inventario `c`, `u`, `F`, `S`, `R`, `r`, `a` y `$` ya manipulan los slots reales del snapshot y envían los mismos paquetes de refresco (`216+slot`) que el servidor Pascal. Además, el nuevo `LegacyGroundItemStore` guarda bolsas temporales por mapa/coordenada para simular drops/recogidas mientras se porta la lógica completa de inventario/dinero, y el catálogo `items.json` alimenta al mundo .NET con nombres, flags y tamaños máximos para partir o apilar objetos como en Pascal (incluyendo la respuesta `IO` del comando `R`). El servidor también volvió a emitir los eventos visuales `#194/#195/#196/#203` al crear o transformar bolsas (Lenna, fogatas, trampas), evita que los jugadores suelten objetos sobre trampas activas y mantiene a los jugadores que están revisando una bolsa para reenviarles la lista actualizada cada vez que cambia su contenido.  
   - Difusión de chat local (`H`/`h`) y global (`T`/`IH`) a través de `LegacySessionManager`, restringiendo el chat local al mapa correspondiente.
4. **Canalización de comandos “legacy”**  
   - `LegacyCommandRouter`/`LegacyCommandDispatcher` recibe cualquier opcode desconocido y lo encola para análisis posterior.  
   - `LegacyCommandInterpreter` ya decodifica `m`, `M`, `W`, `A/B`, `y/Y`, `J` y `O(a/s/d)` mapeándolos a `LegacyPlayerAction`.  
   - Se añadió un `LegacyWorldLoop` que procesa periódicamente las acciones, actualiza el snapshot del jugador y envía los paquetes `p`/`*P` junto con refrescos `r…` generados a partir de los demás jugadores conectados; los comandos de combate/seguimiento muestran mensajes informativos hasta que llegue la simulación completa.
5. **Catálogo de mapas legacy**  
   - Se añadió un cargador (`LegacyMapLoader`) que recorre los archivos `*.mpv` del cliente Pascal, lee los encabezados binarios (`TDatosMapa`/`TDatosMapaExtendido`), expone flags, adyacencias y respawns y los concentra en un `LegacyMapManager`.  
   - Durante la carga se reconstruye una grilla 256×256 (`LegacyMapGrid`) expandiendo el terreno 64×64 original, lo que permite consultar el tipo de casilla por coordenada antes de portar toda la lógica de `TableroControlado`.  
   - El `LegacyWorldActionSink` consume esta grilla mediante un `LegacyMapSurface`, por lo que los movimientos en .NET ya respetan los límites del mapa y se prepara el terreno para validar colisiones reales.
   - `ServerHost` detecta automáticamente el directorio `Original Pascal/Laa/bin`, registra la cantidad de mapas disponibles al iniciar y deja listo el repositorio para el futuro subsistema de terreno.
6. **Estado inicial del mundo y sincronización de jugadores**  
   - Durante el login se valida que el avatar tenga un mapa/casilla cargable; si no existe el mapa o la coordenada es inválida se reaplica el respawn configurado en el `.mpv` o alguna de las posiciones base de `opciones.txt`, dejando registro en el log.  
   - Tras autenticar, el servidor arma los paquetes `r…`/`P` del área actual y los envía inmediatamente al jugador para que vea a los demás sin necesidad de moverse, y también difunde un `*P` al resto de sesiones para crear al avatar recién conectado.  
   - Se activó el comando `'G'` para chat de clan usando la misma codificación legacy (`IG`) y se añadió manejo de errores alrededor de la sincronización inicial para aislar fallos por sesión.
7. **Datos legacy adicionales y clima**  
   - Durante el arranque también se carga el catálogo de monstruos `std.mon`, lo que expone en .NET las mismas estadísticas, botines y comportamientos definidos en Pascal. `LegacyWorldState` valida cada NPC embedido en los mapas contra este catálogo y los paquetes `M/m` volvieron a transportar el `codAnime` original junto con las banderas vivas para imitar exactamente el layout enviado por `TableroControlado`.  
   - `LegacyMapLoader` ahora respeta los sensores (`TSensor`) embebidos en cada `.mpv`. `LegacyWorldActionSink` consulta esas definiciones y ejecuta los portales legacy —incluyendo las restricciones básicas de clan, nivel o estado fantasma—, reemitiendo `'^'/J/j/#0/&0` sobre el mapa destino y difundiendo los paquetes `~`/`N` en ambos mapas para mantener la sincronización.  
   - También se generan instancias reales para cada nido y comerciante (`LegacyMonsterInstance`): el mapa ya se llena con criaturas vivas que se envían en `M/m`, responden a `~`/`N` y se mueven con un ticker genérico que patrulla celdas libres respetando la grilla legacy, de modo que el cliente ve NPCs caminando apenas ingresa a un mapa. Los comerciantes usan el mismo pipeline y siguen apareciendo en el listado `#`.  
   - El ticker de clima dejó de alternar lluvia/sol cada dos minutos: se implementó el ciclo completo de `ControlClima` (`LegacyWeatherTicker`), con día/noche cada 57 600 ticks, eventos `X`/`XR`/`XT` según las banderas del mapa (`bmMapaDeInterior`, `bmSinLluvia`, `bmSinBruma`) y pendientes idénticas a `PendienteDeClima`. Los mapas interiores ya no reciben lluvia y los de tipo “siempre de noche” siguen reportando `CL_NOCHE`.
* Se migró la lectura de `clanes.dat`, `castillos.dat` y `precios.dat`: el host carga todos los clanes activos (nombre/líder/bandera), los castillos con dueño/impuestos/banderas y las tablas de precios por mapa/comerciante, registrando estadísticas durante el arranque en un `LegacyGameData` centralizado. Al iniciar sesión el jugador recibe mensajes que resumen su clan, castillos controlados, condiciones económicas del mapa actual, el paquete `Ik` con los clanes disponibles, el estado del castillo local (`IQ`) y se habilita el chat de clan (`G`) usando la misma codificación legacy (`IG`). Además, los sensores `tsFundarClan` vuelven a funcionar: validan los requisitos legacy, crean el registro correspondiente dentro de `clanes.dat`, asignan el nuevo clan al jugador y difunden los paquetes `IK`/`I#200` para sincronizar a todo el mundo sin depender del servidor Pascal.
* Los comandos `K` del cliente (cambiar pendón `KP`, color `K(`, nombre `KN`, reclutar `KR`, despedir/abandonar `KD`, listado `KL`) ya se ejecutan dentro de `LegacyConnectionContext`: se valida que el usuario sea líder cuando corresponde, se actualiza `clanes.dat`, se sincronizan los snapshots y archivos `.avt` de los jugadores afectados y se difunden los paquetes legacy `IP/I(/IN/I#200` para mantener a todos los clientes al día. También es posible disolver el clan (reseteando su entrada en `clanes.dat`), aunque todavía no se actualizan los castillos asociados.
* Se activó un bucle de mundo mínimo que procesa acciones y ejecuta “tickers” periódicos. Por ahora incluye `LegacyKeepAliveTicker` y `LegacyIdleTimeoutTicker`, que envían `I\0` y desconectan sesiones tras varios minutos de inactividad, replicando el comportamiento del `tickMundo` original mientras se porta el resto del motor.

## 3. Funcionalidades pendientes de migrar

1. **Simulación del mundo**  
   - Falta portar `tickMundo`, `ControlJugadores`, `ControlMonstruos`, clima, limpieza de bolsas/fogatas y guardados automáticos.  
   - Aunque ya se cargan los metadatos `.mpv`, el motor de mapas (`TTableroControlado`) aún no existe en .NET: no hay grid lógico, sensores, comerciantes, IA, combate ni manipulación de objetos en el piso.
2. **Sistemas de juego**  
   - Inventario completo (consumir, usar, fabricar, soltar, recoger, comercio con NPC, baúl, clan/castillo).  
   - Estados especiales: berserker, zoomorfismo, meditaciones, seguros, agro/agresividad, penalizaciones de chat.  
   - Gestor de clanes/castillos/precios (`clanes.dat`, `castillos.dat`, `precios.dat`) y toda la lógica de honor, guardianes, impuestos y mejoras (las operaciones básicas del clan ya están migradas, pero faltan el baúl, las mejoras de castillo, impuestos y la persistencia de `castillos.dat`/`precios.dat`).  
   - Creación de personaje (`*`), cambio de contraseña (`XC`), party/grupo (`XG`, `Xg`), sensores (`s`), oferta/comercio (`V`, `C`, `#`, `)`), etc.
3. **Operaciones administrativas**  
   - Comandos `&` (GM) y menús de la UI VCL (limpieza, clima, activar/desactivar servidor, mensajes globales).  
   - Gestión de permisos basada en `ArchAdministradores`: actualmente se lee el archivo pero no se aplica `LegacyUserPermissions` para autorizar acciones.
4. **Persistencia adicional**  
   - `castillos.dat` y `precios.dat` siguen sin procesarse dentro del host .NET (los nuevos clanes ya se escriben en `clanes.dat`), por lo que cualquier cambio en castillos/impuestos aún depende del servidor Pascal.
5. **Gobernanza de sesiones**  
   - No se replican timers de ocio, desconexiones diferidas por agresividad, anti-flood o cierre forzado desde interfaz, presentes en `Mundo.pas`/`smain.pas`.

## 4. Riesgos y dependencias

- **Compatibilidad con cliente**: cada opcode mal interpretado rompe la sincronización. Es vital portar y validar uno por uno con capturas de tráfico del servidor original.  
- **Datos compartidos**: clanes, castillos y precios son archivos binarios compartidos entre cliente, editores y servidor. Cualquier discrepancia puede corromperlos.  
- **Herramientas auxiliares**: Los editores y utilitarios (Directorio `Original Pascal/Editor*`) asumen el formato actual; no deben romperse durante la migración.

## 5. Plan recomendado de migración

1. **Bootstrapping del “mundo”**  
   - Implementar un `WorldLoop` en .NET que ejecute `Tick` cada 50 ms (o la cadencia original) y consuma las acciones acumuladas por `LegacyCommandInterpreter`.  
   - Portar estructuras mínimas de mapa/entidades (mapa base, posiciones, flags, spawn de monstruos) para que los comandos `m/M/W/A/B/y/Y/J/O` tengan efecto visible.
2. **Portar progresivamente los subsistemas del juego**  
   - Inventario y consumo (`c`, `u`, `F`, `S`, `R`, `r`, `a`, `$`, `V`, `C`, `#`, `)`).  
   - Estados especiales (berserker `i`, zoomorfismo `z`, meditaciones `e/d/o`, selección de conjuro `j`, equipamiento `I`).  
   - Clanes/castillos (`K*`), baúl (`KBGSi`), control de monstruos (`O*`), party (`XG`, `Xg`), sensores, comercio NPC.
3. **Administración y persistencia**  
   - Completar la escritura de `castillos.dat`/`precios.dat` y el resto de los registros (`ClanJugadores`, `Castillo`, `PrecioArticulo`): los clanes ya se crean desde .NET pero falta portar el resto del ecosistema.  
   - Aplicar permisos (`LegacyUserPermissions`) para habilitar comandos GM (`&`) y acciones administrativas con registros en el log.
4. **Gestión de sesiones avanzada**  
   - Replicar timers de ocio, desconexiones diferidas, protección anti-flood y modo “saliendo del servidor” (`X!`).  
   - Integrar métricas básicas para sustituir la UI VCL (por ejemplo, comandos de consola o endpoints HTTP).
5. **Validación y transición**  
   - Ejecutar baterías de pruebas contra el cliente legado: login, movimiento, combate básico, comercio, clanes.  
   - Mantener el servidor Pascal como referencia hasta que cada módulo pase pruebas de regresión.  
   - Documentar cada subsistema migrado y su cobertura de comandos para facilitar QA y soporte futuro.

## 6. Siguientes pasos inmediatos

1. Añadir un “game loop” en `ServerHost` que procese `LegacyWorldState` y exponga hooks para el motor de mapas.  
2. Portar la lógica de colocación/movimiento de jugadores y enviar los paquetes `p/N/*P/r` que el cliente espera al moverse.  
3. Consumir el catálogo de mapas legacy (`LegacyMapManager`) desde el loop del mundo para validar colisiones, sensores y respawns antes de portar la lógica completa de `TableroControlado`.  
4. Empezar el port de la lógica de inventario (comandos `c`, `u`, `F`, `S`, `R`, `r`, `a`, `$`) ya que son los más utilizados después del movimiento/chat.  
5. Sincronizar la carga de mapas igual que el Pascal: al ingresar o cruzar mapas enviar el paquete `'^'` con coordenadas, clima y dueño del castillo, seguido del resto de eventos iniciales (`J/j`, `M/m`, `#0`, `&0`). Esto evita que el cliente vea un “mundo negro” y prepara el terreno para transmitir comerciantes, bolsas y banderas del mapa.  
6. Definir una estrategia de testing (captura de tráfico, scripts de cliente) para verificar cada comando migrado antes de avanzar al siguiente subsistema.

---

> **Nota**: mantener la migración iterativa y con visibilidad (logs detallados, métricas) facilitará la comparación con el servidor Pascal y reducirá riesgos de regresión.
## 7. Progreso reciente y próximos hitos

### Estado actual

- `std.mon` ya se consume en el arranque y alimenta a `LegacyWorldState`/`LegacyWorldPacketFactory`. Con ello, los paquetes `M/m` volvieron a incluir el `codAnime` y las banderas reales de cada NPC, y cualquier subsistema puede consultar estadísticas y botines directamente desde `LegacyGameData.Monsters`.
- Los sensores incluidos en los `.mpv` se cargan y se exponen en `LegacyMapDefinition`. `LegacyWorldActionSink` detecta cuando el jugador pisa un portal y ejecuta el teletransporte legacy completo: valida las restricciones básicas (`fsSoloFantasma`, `fsSoloAprendiz`, `fsSoloClan`), reubica al avatar dentro o fuera del mapa y reemite `'^'/J/j/#0/&0` hacia el nuevo mapa mientras difunde `~`/`N` en ambos lados.
- `LegacyWeatherTicker` ya no alterna clima cada dos minutos; implementa el ciclo completo de `ControlClima` con día/noche, eventos `X`/`XR`/`XT` y pendientes idénticas a las de Pascal, respetando las banderas de cada mapa (`bmMapaDeInterior`, `bmSinLluvia`, `bmSinBruma`). El estado global se replica en `SetGlobalWeather` para que los nuevos logins reciban el clima correcto en el paquete `'^'`.
- Los nidos y comerciantes generan instancias reales (`LegacyMonsterInstance`) que se envían en `M/m` y se actualizan en tiempo real: un ticker (`LegacyMonsterTicker`) les asigna un movimiento aleatorio y difunde los paquetes `P` correspondientes, lo que elimina el “mundo vacío” y deja listo el pipeline para futuras IAs y combates.
- El sensor `tsFundarClan` ya no es un placeholder: valida nivel/pericia, elige un slot libre en `clanes.dat`, crea el registro legacy con bandera e identificador únicos y difunde los paquetes `IK`/`I#200` para que todos los clientes vean el nuevo clan sin depender del servidor Pascal.

### Trabajo en curso

- Darles comportamiento significativo a los NPCs: hoy sólo patrullan celdas libres; falta portar el control de agresión, ataques y respawns para que `ControlMonstruos` tenga paridad con Pascal.
- Portar el resto de sensores (curaciones avanzadas, banderas/castillos, llaves especiales) y enlazarlos con los `FlagsCalabozo` para que puertas, trampas y scripts del mapa reaccionen como en `TableroControlado`.
- Enlazar el ticker de clima con los efectos laterales del mundo (apagado de fogatas, autolimpieza de bolsas/flags) reutilizando las banderas autolimpiables que ya expone `LegacyMapExtendedData`.
- Extender el ecosistema de clanes hacia castillos/baúles: hoy sólo se administra el roster; faltan los comandos de tesoro, mejoras y la escritura de `castillos.dat`.

### Siguientes pasos recomendados

1. Reutilizar `LegacyMonsterInstance` para comenzar a portar la IA: definir objetivos/agresividad básica, enviar `n`/`~` cuando aparezcan/desaparezcan y alimentar el pipeline de combate con datos reales (HP, animación de ataque) antes de migrar `ControlMonstruos` completo.
2. Completar la traducción de sensores (`tsRegFisica`, `tsCBandera`, `tsLBandera`, `tsPortal` con llaves especiales) y sincronizarlos con los flags de mapa (`Fijar/BorrarFlagsCalabozo`) para que puertas, banderas y llaves reaccionen igual que en el servidor Pascal.
3. Integrar el nuevo `LegacyWeatherTicker` con los temporizadores de limpieza y con los efectos sobre fogatas/bolsas, aprovechando el catálogo de mapas para emitir eventos específicos por mapa (apagado de fogatas cuando hay lluvia, regreso automático a `CL_NOCHE` en mapas oscuros, etc.).
4. Extender el subsistema de clanes más allá del roster: baúl y tesoro compartidos, mejoras de castillo, impuestos y escritura de `castillos.dat`/`precios.dat`.

## 8. Foco específico: brechas en la migración de mapas

### 8.1 Estado actual

- El cargador de `.mpv` (`LegacyMapLoader`) reconstruye encabezados, flags y una grilla 256×256 por mapa, expuesta mediante `LegacyMapGrid`/`LegacyMapSurface`.
- `LegacyWorldActionSink` ya valida límites del mapa, sensores básicos de portales y respawns, y el `LegacyMonsterTicker` consume estos datos para patrullar NPCs sin superponerse.
- Existen repositorios parciales para bolsas (`LegacyGroundItemStore`) y comerciantes/nidos (`LegacyMonsterInstance`), pero sus ciclos de vida aún no dependen del mapa.

### 8.2 Brechas críticas por componente

| Componente | Trabajo pendiente | Riesgo/impacto |
| --- | --- | --- |
| Motor de tablero (`TTableroControlado`) | Falta una contraparte `.NET` que mantenga el estado vivo del mapa (ocupación por celda, colas de refresco, watchers por área, envío de `r/#/&` en lote, listas de fogatas/bolsas activas). | Sin este motor no es posible aplicar reglas de colisión reales ni transmitir correctamente lo que cada jugador debe ver. |
| Sensores y `FlagsCalabozo` | Sólo los portales simples están implementados; siguen pendientes sensores de llaves, banderas, castillos, scripts de clan/nivel y `SensorClick`. | Los mapas “especiales” (dungeons, castillos) no reaccionarán y el progreso de clanes quedará bloqueado. |
| Entidades del mapa | Los NPCs caminan pero no tienen IA ni objetivos y los comerciantes no controlan colisiones ni rotaciones (`EnviarSpritesMapa`). | Eventos como ataques, seguimiento a jugadores, reaparecer tras muerte y rotación de comerciantes no existen. |
| Objetos dinámicos (bolsas, fogatas, trampas, recursos) | `LegacyGroundItemStore` no controla TTL, flags autolimpiables, comunicación `#194/#195` por mapa ni la conversión completa de `Artefacto`↔paquete. Las fogatas y trampas siguen siendo placeholders. | Riesgo de desincronización visual, acumulación de bolsas y exploits de duplicación. |
| Cambios de mapa y streaming | El login reproduce `'^'/J/j` pero todavía no existe una transición completa al cruzar portales o edges (no se vacía la visibilidad anterior ni se reenvían todos los `M/m/#0/&0`). | Cada cruce de mapa puede dejar sprites “fantasma” o perder NPCs esenciales. |

### 8.3 Hitos propuestos enfocados en mapas

1. **`LegacyMapRuntime`**: crear un objeto por mapa que encapsule la grilla, ocupación, colecciones de bolsas/fogatas, sensores y watchers. Debe exponer métodos equivalentes a `ColocarJugador`, `SacarJugador`, `GetRefrescamientoAreaJugador` y `EnviarDatosInicialesMapa`, generando los paquetes `p/*P/r/#/&` exactos.  
   - Entregable: API estable (`TryPlacePlayer`, `BroadcastAreaUpdate`, `EnumerateEntities`) consumida por `LegacyWorldLoop`.
2. **Colisiones y reservas de celda**: migrar la lógica de `CeldaLibre`/`ColocarJugador`/`Mover` para impedir superposiciones entre jugadores, NPCs y objetos físicos. Integrar la grilla 256×256 con las banderas de casilla (agua, lava, pared, sensor) antes de portar combate.  
   - Entregable: tests dirigidos que prueben teletransportes y movimientos simultáneos.
3. **Sensores avanzados**: portar `RealizarControlSensores`, `SensorClick` y `RealizarComportamientoFlag`. Priorizar `tsRegFisica`, `tsCBandera`, `tsLBandera`, `tsFundarClan` (ya migrado), llaves (`ConsumirLaLlave`) y scripts de castillo/puerta.  
   - Entregable: catálogo JSON/Markdown que mapee cada `TipoSensor` a su handler C# y checklist de mapas que dependan de ellos.
4. **Ciclo de vida de objetos en piso**: completar `LegacyGroundItemStore` para que respete TTL (`ControlBolsasMapa`), limpieza por clima (`apagar fogatas`), conversión completa de `TArtefacto` y watchers (`BorlasEnMapa`). Incluir fogatas (`PrenderFogata`), trampas (`CrearTrampa`, `TTrigger`) y drops al morir (`SoltarObjetosMuerto`).  
   - Entregable: simulación reproducible que muestre creación, actualización y eliminación de bolsas/fogatas desde consola.
5. **Streaming completo de mapas**: migrar `EnviarDatosInicialesMapa`/`enviarDatosFinalesMapa` para toda transición (portales, respawns, logout/login). Debe enviar en orden `'^'` + clima, luego `J/j`, `M/m`, `#0`, `&0` y limpiar la visibilidad anterior (`~`).  
   - Entregable: script de regresión que mueva un jugador entre dos mapas y compare la secuencia de paquetes contra capturas del servidor Pascal.
6. **Hooks para IA/combate**: una vez consolidado el runtime del mapa, exponer APIs para que `LegacyMonsterTicker` y el futuro `CombatSystem` consulten ocupación, cuartos y sensores. Esto permitirá portar `ControlMonstruos`, `Atacar`, `LanzarConjuro` y `MoverAutomaticamente` sin reescribir el manejo de mapa otra vez.  
   - Entregable: capa `IMapRuntime` mockeable con pruebas unitarias para IA y combate.

> La recomendación es validar cada hito en un mapa controlado (ej. `Bosque Inicial`) antes de generalizarlo. Así se puede comparar cada paquete generado con capturas del servidor Pascal y garantizar compatibilidad binaria.

### 8.4 Avances recientes

- `LegacyMapRuntime` ya vive dentro de `LegacyWorldState`: registra cada jugador por mapa, expone snapshots atómicos y permite consultas por área para que `BroadcastToMapAsync` y `LegacyAreaSnapshotBuilder` dejen de recorrer a todos los jugadores.
- La grilla 256×256 (`LegacyMapGrid`) ahora replica el `terBol` del Pascal: cada celda codifica los flags `ft_*` (sólido, cubierto, agua, fuego, etc.) y `LegacyMapSurface` usa la misma máscara que el cliente original para decidir si una casilla es caminable. Los monstruos reutilizan los flags de `std.mon`, así que nidos y comerciantes ya respetan las restricciones de terreno (agua/lava solo acepta criaturas con los bits correspondientes y nunca se invaden casillas prohibidas).
- Las reservas de celdas y el movimiento de jugadores también viven dentro del runtime: cada mapa lleva su propia grilla de ocupación, `TryMovePlayer` delega en `LegacyMapRuntime.TryOccupy/Release` y los monstruos consultan la misma tabla antes de patrullar, lo que elimina superposiciones entre avatares/NPC y alinea la lógica con los métodos `LugarVacioXY`/`ColocarJugador` del servidor original.
- Los sensores volvieron a comportarse como en Pascal: `LegacyWorldActionSink` valida y consume las llaves legacy (`SensorKeyContext`), aplica cooldowns a las plataformas de regeneración, hace que portales y banderas modifiquen `FlagsCalabozo` sólo cuando el mapa lo permite y reacciona al click `s` de las banderas de clan, verificando al dueño del castillo, consumiendo la llave y difundiendo los cambios al mapa correspondiente.
- Próximo foco recomendado: migrar los sensores pendientes (`tsResurreccion`, `tsSwapItem`, llaves/calabozos especiales) reutilizando este pipeline, propagar los efectos secundarios de honor (recalculo de stats) y validar las nuevas rutas con capturas del servidor original para asegurar compatibilidad binaria.
